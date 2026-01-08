using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Tasks;
using UdpPackets;
using RedStar.Amounts.WeatherExtensions;
using MetWorksModels;
using Utility;

namespace MetWorksServices;

/// <summary>
/// Transforms raw UDP packets into typed weather readings with user-preferred units.
/// Implements event-driven retransformation: when unit settings change,
/// automatically retransforms cached packets and publishes updated readings.
/// </summary>
public class WeatherDataTransformer : IAsyncDisposable
{
    // ========================================
    // Dependencies
    // ========================================
    private IFileLogger? _fileLogger;
    private ISettingsRepository? _settingsRepository;
    private ProvenanceTracker? _provenanceTracker;

    private IFileLogger FileLoggerSafe => 
        _fileLogger ?? throw new InvalidOperationException("WeatherDataTransformer not initialized.");
    
    private ISettingsRepository SettingsRepositorySafe =>
        _settingsRepository ?? throw new InvalidOperationException("WeatherDataTransformer not initialized.");

    // ========================================
    // Last-Packet Cache (LRU pattern)
    // ========================================
    /// <summary>
    /// Maintains the most recent packet per PacketEnum type for retransformation.
    /// When settings change, we retransform ALL cached packets with new unit preferences.
    /// </summary>
    private readonly ConcurrentDictionary<PacketEnum, IRawPacketRecordTyped> _lastPacketCache = new();

    // ========================================
    // Unit Preference Fields
    // ========================================
    private Unit _preferredTemperatureUnit = TemperatureUnits.DegreeFahrenheit;
    private Unit _preferredPressureUnit = PressureUnits.InchOfMercury;
    private Unit _preferredSpeedUnit = SpeedUnits.MilePerHour;
    private Unit _preferredDistanceUnit = LengthUnits.Mile;
    private Unit _preferredPrecipitationUnit = LengthUnits.Inch;

    // ========================================
    // Initialization
    // ========================================

    public async Task<bool> InitializeAsync(
        IFileLogger iFileLogger,
        ISettingsRepository iSettingsRepository,
        ProvenanceTracker? provenanceTracker = null
    )
    {
        if (iFileLogger is null)
            throw new ArgumentNullException(nameof(iFileLogger));
        
        if (iSettingsRepository is null)
            throw new ArgumentNullException(nameof(iSettingsRepository));

        _fileLogger = iFileLogger;
        _settingsRepository = iSettingsRepository;
        _provenanceTracker = provenanceTracker; // Optional

        try
        {
            // Register RedStar.Amounts temperature conversions
            RegisterUnitConversions();

            // Load current unit preferences from settings
            LoadUnitPreferences();

            // Subscribe to unit setting changes (wildcard pattern)
            _settingsRepository.OnSettingsChanged("/services/udp/unitOverrides", OnUnitSettingChanged);

            // Subscribe to raw packet events
            ISingletonEventRelay.Register<IRawPacketRecordTyped>(this, OnRawPacketReceived);

            _fileLogger.Information("🌡️ WeatherDataTransformer initialized successfully");
            _fileLogger.Information($"   Temperature: {_preferredTemperatureUnit.Name}");
            _fileLogger.Information($"   Pressure: {_preferredPressureUnit.Name}");
            _fileLogger.Information($"   Speed: {_preferredSpeedUnit.Name}");
            _fileLogger.Information($"   Distance: {_preferredDistanceUnit.Name}");
            _fileLogger.Information($"   Precipitation: {_preferredPrecipitationUnit.Name}");

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _fileLogger.Error($"❌ WeatherDataTransformer initialization failed: {ex.Message}");
            throw;
        }
    }

    // ========================================
    // Unit Conversion Registration
    // ========================================

    private void RegisterUnitConversions()
    {
        // Temperature conversions (already registered in TemperatureUnits)
        TemperatureUnits.RegisterConversions();
        
        FileLoggerSafe.Debug("✅ RedStar.Amounts unit conversions registered");
    }

    // ========================================
    // Unit Preference Management
    // ========================================

    /// <summary>
    /// Loads unit preferences from settings repository.
    /// Public for testing purposes.
    /// </summary>
    public void RefreshUnitPreferences() => LoadUnitPreferences();

    private void LoadUnitPreferences()
    {
        try
        {
            // Temperature
            var tempSetting = SettingsRepositorySafe.GetValueOrDefault("/services/udp/unitOverrides/temperature");
            _preferredTemperatureUnit = UnitManager.GetUnitByName(tempSetting ?? "degree fahrenheit");

            // Pressure
            var pressureSetting = SettingsRepositorySafe.GetValueOrDefault("/services/udp/unitOverrides/pressure");
            _preferredPressureUnit = UnitManager.GetUnitByName(pressureSetting ?? "inch of mercury");

            // Speed
            var speedSetting = SettingsRepositorySafe.GetValueOrDefault("/services/udp/unitOverrides/windSpeed");
            _preferredSpeedUnit = UnitManager.GetUnitByName(speedSetting ?? "mile/hour");

            // Distance
            var distanceSetting = SettingsRepositorySafe.GetValueOrDefault("/services/udp/unitOverrides/distance");
            _preferredDistanceUnit = UnitManager.GetUnitByName(distanceSetting ?? "mile");

            // Precipitation
            var precipSetting = SettingsRepositorySafe.GetValueOrDefault("/services/udp/unitOverrides/precipitation");
            _preferredPrecipitationUnit = UnitManager.GetUnitByName(precipSetting ?? "inch");

            FileLoggerSafe.Debug("✅ Unit preferences loaded successfully");
        }
        catch (Exception ex)
        {
            FileLoggerSafe.Error($"❌ Failed to load unit preferences: {ex.Message}");
            // Keep default units if settings fail
        }
    }

    // ========================================
    // Settings Change Handler
    // ========================================

    private void OnUnitSettingChanged(string path, string? oldValue, string? newValue)
    {
        FileLoggerSafe.Information($"🔄 Unit setting changed: {path} = '{oldValue}' -> '{newValue}'");

        // Reload all unit preferences
        LoadUnitPreferences();

        // Retransform all cached packets with new units
        RetransformCachedPackets();
    }

    // ========================================
    // Retransformation Logic
    // ========================================

    private void RetransformCachedPackets()
    {
        var cachedCount = _lastPacketCache.Count;
        if (cachedCount == 0)
        {
            FileLoggerSafe.Debug("No cached packets to retransform");
            return;
        }

        FileLoggerSafe.Information($"🔄 Retransforming {cachedCount} cached packets with new unit preferences");

        foreach (var kvp in _lastPacketCache)
        {
            var packetType = kvp.Key;
            var rawPacket = kvp.Value;

            try
            {
                TransformAndPublish(rawPacket, isRetransformation: true);
                FileLoggerSafe.Debug($"✅ Retransformed {packetType} packet: {rawPacket.Id}");
            }
            catch (Exception ex)
            {
                FileLoggerSafe.Error($"❌ Failed to retransform {packetType} packet {rawPacket.Id}: {ex.Message}");
                
                _provenanceTracker?.RecordError(rawPacket.Id, "WeatherDataTransformer", "Retransform", ex);
            }
        }

        FileLoggerSafe.Information($"✅ Retransformation complete: {cachedCount} packets processed");
    }

    // ========================================
    // Raw Packet Event Handler
    // ========================================

    private void OnRawPacketReceived(IRawPacketRecordTyped rawPacket)
    {
        if (rawPacket is null) return;

        try
        {
            // Update cache with latest packet
            _lastPacketCache[rawPacket.PacketEnum] = rawPacket;

            // Transform and publish
            TransformAndPublish(rawPacket, isRetransformation: false);
        }
        catch (Exception ex)
        {
            FileLoggerSafe.Error($"❌ Failed to process packet {rawPacket.Id}: {ex.Message}");
            
            _provenanceTracker?.RecordError(rawPacket.Id, "WeatherDataTransformer", "Transform", ex);
        }
    }

    // ========================================
    // Core Transformation Logic
    // ========================================

    private void TransformAndPublish(IRawPacketRecordTyped rawPacket, bool isRetransformation)
    {
        var transformStart = DateTime.UtcNow;

        IWeatherReading? reading = rawPacket.PacketEnum switch
        {
            PacketEnum.Observation => ParseObservation(rawPacket, isRetransformation),
            PacketEnum.Wind => ParseWind(rawPacket, isRetransformation),
            PacketEnum.Precipitation => ParsePrecipitation(rawPacket, isRetransformation),
            PacketEnum.Lightning => ParseLightning(rawPacket, isRetransformation),
            _ => null
        };

        if (reading is null)
        {
            FileLoggerSafe.Warning($"⚠️ Failed to parse {rawPacket.PacketEnum} packet {rawPacket.Id}");
            return;
        }

        var transformEnd = DateTime.UtcNow;

        // Track transformation in provenance
        _provenanceTracker?.LinkTransformedReading(rawPacket.Id, reading.Id);
        _provenanceTracker?.AddStep(
            rawPacket.Id, 
            isRetransformation ? "Retransformation" : "Transformation",
            "WeatherDataTransformer",
            $"Converted to {reading.PacketType} with user units",
            transformEnd - transformStart);

        // ========================================
        // FIX: Send the SPECIFIC type, not the base interface
        // ========================================
        switch (reading)
        {
            case IObservationReading obs:
                ISingletonEventRelay.Send(obs);  // ✅ Send as IObservationReading
                break;
            case IWindReading wind:
                ISingletonEventRelay.Send(wind);  // ✅ Send as IWindReading
                break;
            case IPrecipitationReading precip:
                ISingletonEventRelay.Send(precip);  // ✅ Send as IPrecipitationReading
                break;
            case ILightningReading lightning:
                ISingletonEventRelay.Send(lightning);  // ✅ Send as ILightningReading
                break;
            default:
                FileLoggerSafe.Warning($"⚠️ Unknown reading type: {reading.GetType().Name}");
                break;
        }

        FileLoggerSafe.Debug($"📤 Published {reading.PacketType} reading: {reading.Id}");
    }

    // ========================================
    // Packet Parsers (Using Public Parser API)
    // ========================================

    private IObservationReading? ParseObservation(IRawPacketRecordTyped rawPacket, bool isRetransformation)
    {
        try
        {
            // Use public parser API (keeps DTOs internal)
            var reading = TempestPacketParser.ParseObservation(rawPacket);
            if (reading is null)
            {
                FileLoggerSafe.Warning($"⚠️ Failed to parse observation packet {rawPacket.Id}");
                FileLoggerSafe.Warning($"observation contents {rawPacket.RawPacketJson}");
                return null;
            }

            // Convert from Tempest's METRIC units to user preferences
            var temperature = new Amount(reading.AirTemperature, TemperatureUnits.DegreeCelsius)
                .ConvertedTo(_preferredTemperatureUnit);
            
            var pressure = new Amount(reading.StationPressure, PressureUnits.MilliBar)
                .ConvertedTo(_preferredPressureUnit);

            return new ObservationReading
            {
                Id = IdGenerator.CreateCombGuid(),
                SourcePacketId = rawPacket.Id,
                Timestamp = DateTimeOffset.FromUnixTimeSeconds(reading.EpochTimestampUtc).UtcDateTime,
                ReceivedUtc = rawPacket.ReceivedTime,
                Temperature = temperature,
                HumidityPercent = reading.RelativeHumidity,
                Pressure = pressure,
                DewPoint = null,  // Can be calculated if needed
                UvIndex = reading.UvIndex,
                SolarRadiation = reading.SolarRadiation,
                Provenance = new ReadingProvenance
                {
                    RawPacketId = rawPacket.Id,
                    UdpReceiptTime = rawPacket.ReceivedTime,
                    TransformStartTime = DateTime.UtcNow,
                    TransformEndTime = DateTime.UtcNow,
                    SourceUnits = "degree celsius, millibar",
                    TargetUnits = $"{_preferredTemperatureUnit.Name}, {_preferredPressureUnit.Name}",
                    TransformerVersion = isRetransformation ? "1.0-retransform" : "1.0"
                }
            };
        }
        catch (Exception ex)
        {
            FileLoggerSafe.Error($"❌ Failed to parse Observation packet: {ex.Message}");
            FileLoggerSafe.Debug($"   Stack trace: {ex.StackTrace}");
            return null;
        }
    }

    private IWindReading? ParseWind(IRawPacketRecordTyped rawPacket, bool isRetransformation)
    {
        try
        {
            // Use public parser API
            var windDto = TempestPacketParser.ParseWind(rawPacket);
            if (windDto is null)
            {
                FileLoggerSafe.Warning($"⚠️ Failed to parse wind packet {rawPacket.Id}");
                return null;
            }

            // Convert from Tempest's METRIC units (m/s) to user preferences
            var speed = new Amount(windDto.WindSpeed, SpeedUnits.MeterPerSecond)
                .ConvertedTo(_preferredSpeedUnit);

            return new WindReading
            {
                Id = IdGenerator.CreateCombGuid(),
                SourcePacketId = rawPacket.Id,
                Timestamp = DateTimeOffset.FromUnixTimeSeconds(windDto.DeviceReceivedUtcTimestampEpoch).UtcDateTime,
                ReceivedUtc = rawPacket.ReceivedTime,
                Speed = speed,
                DirectionDegrees = windDto.WindDirection,
                DirectionCardinal = DegreesToCardinal(windDto.WindDirection),
                GustSpeed = null,  // rapid_wind doesn't include gusts
                Provenance = new ReadingProvenance
                {
                    RawPacketId = rawPacket.Id,
                    UdpReceiptTime = rawPacket.ReceivedTime,
                    TransformStartTime = DateTime.UtcNow,
                    TransformEndTime = DateTime.UtcNow,
                    SourceUnits = "meter/second",
                    TargetUnits = _preferredSpeedUnit.Name,
                    TransformerVersion = isRetransformation ? "1.0-retransform" : "1.0"
                }
            };
        }
        catch (Exception ex)
        {
            FileLoggerSafe.Error($"❌ Failed to parse Wind packet: {ex.Message}");
            FileLoggerSafe.Debug($"   Stack trace: {ex.StackTrace}");
            return null;
        }
    }

    private IPrecipitationReading? ParsePrecipitation(IRawPacketRecordTyped rawPacket, bool isRetransformation)
    {
        try
        {
            // Use public parser API
            var precipDto = TempestPacketParser.ParsePrecipitation(rawPacket);
            if (precipDto is null)
            {
                FileLoggerSafe.Warning($"⚠️ Failed to parse precipitation packet {rawPacket.Id}");
                return null;
            }

            // evt_precip is just a notification event (no rate data in the event itself)
            return new PrecipitationReading
            {
                Id = IdGenerator.CreateCombGuid(),
                SourcePacketId = rawPacket.Id,
                Timestamp = DateTimeOffset.FromUnixTimeSeconds(precipDto.DeviceReceivedUtcTimestampEpoch).UtcDateTime,
                ReceivedUtc = rawPacket.ReceivedTime,
                RainRate = new Amount(0, LengthUnits.MilliMeter).ConvertedTo(_preferredPrecipitationUnit),  // Event only
                DailyAccumulation = null,  // Get from observation packet
                Provenance = new ReadingProvenance
                {
                    RawPacketId = rawPacket.Id,
                    UdpReceiptTime = rawPacket.ReceivedTime,
                    TransformStartTime = DateTime.UtcNow,
                    TransformEndTime = DateTime.UtcNow,
                    SourceUnits = "millimeter",
                    TargetUnits = _preferredPrecipitationUnit.Name,
                    TransformerVersion = isRetransformation ? "1.0-retransform" : "1.0"
                }
            };
        }
        catch (Exception ex)
        {
            FileLoggerSafe.Error($"❌ Failed to parse Precipitation packet: {ex.Message}");
            FileLoggerSafe.Debug($"   Stack trace: {ex.StackTrace}");
            return null;
        }
    }

    private ILightningReading? ParseLightning(IRawPacketRecordTyped rawPacket, bool isRetransformation)
    {
        try
        {
            // Use public parser API
            var lightningDto = TempestPacketParser.ParseLightning(rawPacket);
            if (lightningDto is null)
            {
                FileLoggerSafe.Warning($"⚠️ Failed to parse lightning packet {rawPacket.Id}");
                return null;
            }

            // Convert from Tempest's METRIC units (km) to user preferences
            var strikeDistance = new Amount(lightningDto.LightningStrikeDistanceKm, LengthUnits.KiloMeter)
                .ConvertedTo(_preferredDistanceUnit);

            return new LightningReading
            {
                Id = IdGenerator.CreateCombGuid(),
                SourcePacketId = rawPacket.Id,
                Timestamp = DateTimeOffset.FromUnixTimeSeconds(lightningDto.DeviceReceivedUtcTimestampEpoch).UtcDateTime,
                ReceivedUtc = rawPacket.ReceivedTime,
                StrikeDistance = strikeDistance,
                StrikeCount = 1,  // Each event is one strike
                Provenance = new ReadingProvenance
                {
                    RawPacketId = rawPacket.Id,
                    UdpReceiptTime = rawPacket.ReceivedTime,
                    TransformStartTime = DateTime.UtcNow,
                    TransformEndTime = DateTime.UtcNow,
                    SourceUnits = "kilometer",
                    TargetUnits = _preferredDistanceUnit.Name,
                    TransformerVersion = isRetransformation ? "1.0-retransform" : "1.0"
                }
            };
        }
        catch (Exception ex)
        {
            FileLoggerSafe.Error($"❌ Failed to parse Lightning packet: {ex.Message}");
            FileLoggerSafe.Debug($"   Stack trace: {ex.StackTrace}");
            return null;
        }
    }

    // ========================================
    // Helper Methods
    // ========================================

    /// <summary>
    /// Converts wind direction degrees to cardinal/intercardinal direction.
    /// </summary>
    private static string DegreesToCardinal(double degrees)
    {
        // Normalize to 0-360
        degrees = degrees % 360;
        if (degrees < 0) degrees += 360;

        return degrees switch
        {
            >= 348.75 or < 11.25 => "N",
            >= 11.25 and < 33.75 => "NNE",
            >= 33.75 and < 56.25 => "NE",
            >= 56.25 and < 78.75 => "ENE",
            >= 78.75 and < 101.25 => "E",
            >= 101.25 and < 123.75 => "ESE",
            >= 123.75 and < 146.25 => "SE",
            >= 146.25 and < 168.75 => "SSE",
            >= 168.75 and < 191.25 => "S",
            >= 191.25 and < 213.75 => "SSW",
            >= 213.75 and < 236.25 => "SW",
            >= 236.25 and < 258.75 => "WSW",
            >= 258.75 and < 281.25 => "W",
            >= 281.25 and < 303.75 => "WNW",
            >= 303.75 and < 326.25 => "NW",
            >= 326.25 and < 348.75 => "NNW",
            _ => "N"
        };
    }

    // ========================================
    // Disposal
    // ========================================

    public async ValueTask DisposeAsync()
    {
        try
        {
            // Unsubscribe from events
            ISingletonEventRelay.Unregister<IRawPacketRecordTyped>(this);

            FileLoggerSafe.Information("🛑 WeatherDataTransformer disposed");
        }
        catch (Exception ex)
        {
            FileLoggerSafe?.Warning($"⚠️ Error during WeatherDataTransformer disposal: {ex.Message}");
        }

        await Task.CompletedTask;
    }
}