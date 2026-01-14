namespace MetWorksServices;

//using xxx = Dictionary<MeasurementType, UnitData>;

/// <summary>
/// Transforms raw UDP packets into typed weather readings with user-preferred units.
/// Implements event-driven retransformation: when unit settings change,
/// automatically retransforms cached packets and publishes updated readings.
/// </summary>
public class WeatherDataTransformer : IAsyncDisposable
{
    bool _isInitialized = false;

    ILogger? _iLogger = null;
    ILogger ILogger
    {
        get => NullPropertyGuard.Get(_isInitialized, _iLogger, nameof(ILogger));
        set => _iLogger = value;
    }

    IEventRelayBasic? _iEventRelayBasic = null;
    IEventRelayBasic IEventRelayBasic
    {
        get => NullPropertyGuard.Get(
            _isInitialized, _iEventRelayBasic, nameof(IEventRelayBasic));
        set => _iEventRelayBasic = value;
    }

    IEventRelayPath? _iEventRelayPath = null;
    IEventRelayPath IEventRelayPath
    {
        get => NullPropertyGuard.Get(
            _isInitialized, _iEventRelayPath, nameof(IEventRelayPath));
        set => _iEventRelayPath = value;
    }

    ISettingRepository? _iSettingRepository;
    ISettingRepository ISettingRepository
    {
        get => NullPropertyGuard.Get(
            _isInitialized, _iSettingRepository, nameof(ISettingRepository)
        );
        set => _iSettingRepository = value;
    }

    ProvenanceTracker? _provenanceTracker;
    
    // ========================================
    // Last-Packet Cache (LRU pattern)
    // ========================================
    /// <summary>
    /// Maintains the most recent packet per PacketEnum type for retransformation.
    /// When settings change, we retransform ALL cached packets with new unit preferences.
    /// </summary>
    readonly ConcurrentDictionary<PacketEnum, IRawPacketRecordTyped> _lastPacketCache = new();

    // ========================================
    // Initialization
    // ========================================
    public async Task<bool> InitializeAsync(
        ILogger iLogger,
        ISettingRepository iSettingRepository,
        IEventRelayBasic iEventRelayBasic,
        IEventRelayPath iEventRelayPath,
        ProvenanceTracker? provenanceTracker = null
    )
    {
        try
        {
            ILogger = iLogger;
            ISettingRepository = iSettingRepository;
            IEventRelayBasic = iEventRelayBasic;
            IEventRelayPath = iEventRelayPath;
            _provenanceTracker = provenanceTracker; // Optional

            _isInitialized = true;
            // Load current unit preferences from settings
            if (!LoadUnitPreference(iSettingRepository, iLogger))
            {
                iLogger.Error("Failed to load unit preferences during initialization");
                return false;
            }

            ILogger.Information("🌡️ WeatherDataTransformer initialized successfully");
            foreach(var unitKVP in _preferredUnits)
                ILogger.Information($"   {unitKVP.Key}: {unitKVP.Value.Name}");

            // Subscribe to unit setting changes (wildcard pattern)
            IEventRelayPath.Register(UnitOfMeasureGroupSettingsDefinition.GroupBasePath, OnUnitSettingChanged);

            // Subscribe to raw packet events
            IEventRelayBasic.Register<IRawPacketRecordTyped>(this, OnRawPacketReceived);

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _iLogger.Error($"❌ WeatherDataTransformer initialization failed: {ex.Message}");
            throw;
        }
    }

    private void RegisterUnitConversions()
    {
    }

    // ========================================
    // Unit Preference Management
    // ========================================

    Dictionary<MeasurementTypeEnum, Unit> _preferredUnits = new();

    bool LoadUnitPreference(ISettingRepository iSettingsRepository, ILogger iLogger)
    {
        try
        {
            var unitOfMeasure_airPressure = iSettingsRepository.GetValueOrDefault<string>(
                    UnitOfMeasureGroupSettingsDefinition.BuildSettingPath(UnitOfMeasure_airPressure)
                );
            _preferredUnits[MeasurementTypeEnum.AirPressure] = Unit.Parse(unitOfMeasure_airPressure);

            var unitOfMeasure_airTemperature = iSettingsRepository.GetValueOrDefault<string>(
                    UnitOfMeasureGroupSettingsDefinition.BuildSettingPath(UnitOfMeasure_airTemperature)
                );
            _preferredUnits[MeasurementTypeEnum.AirTemperature] = Unit.Parse(unitOfMeasure_airTemperature);

            var unitOfMeasure_lightningDistance = iSettingsRepository.GetValueOrDefault<string>(
                    UnitOfMeasureGroupSettingsDefinition.BuildSettingPath(UnitOfMeasure_lightningDistance)
                );
            _preferredUnits[MeasurementTypeEnum.LightningDistance] = Unit.Parse(unitOfMeasure_lightningDistance);

            var unitOfMeasure_precipitationAmount = iSettingsRepository.GetValueOrDefault<string>(
                    UnitOfMeasureGroupSettingsDefinition.BuildSettingPath(UnitOfMeasure_precipitationAmount)
                );
            _preferredUnits[MeasurementTypeEnum.PrecipitationAmount] = Unit.Parse(unitOfMeasure_precipitationAmount);

            var unitOfMeasure_windSpeed = iSettingsRepository.GetValueOrDefault<string>(
                    UnitOfMeasureGroupSettingsDefinition.BuildSettingPath(UnitOfMeasure_windSpeed)
                );
            _preferredUnits[MeasurementTypeEnum.WindSpeed] = Unit.Parse(unitOfMeasure_windSpeed);

            return true;
        }        
        
        catch (Exception ex)
        {
            iLogger.Error(
                $"❌ Failed to load unit preferences: {ex.Message}"
            );
            return false;
        }   
    }

    // ========================================
    // Settings Change Handler
    // ========================================

    private void OnUnitSettingChanged(ISettingValue iSettingValue)
    {
        ILogger.Information($"🔄 Unit setting changed");

        LoadUnitPreference(ISettingRepository, ILogger);

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
            ILogger.Debug("No cached packets to retransform");
            return;
        }

        ILogger.Information($"🔄 Retransforming {cachedCount} cached packets with new unit preferences");

        foreach (var kvp in _lastPacketCache)
        {
            var packetType = kvp.Key;
            var rawPacket = kvp.Value;

            try
            {
                TransformAndPublish(rawPacket, isRetransformation: true);
                ILogger.Debug($"✅ Retransformed {packetType} packet: {rawPacket.Id}");
            }
            catch (Exception ex)
            {
                ILogger.Error($"❌ Failed to retransform {packetType} packet {rawPacket.Id}: {ex.Message}");
                
                _provenanceTracker?.RecordError(rawPacket.Id, "WeatherDataTransformer", "Retransform", ex);
            }
        }

        ILogger.Information($"✅ Retransformation complete: {cachedCount} packets processed");
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
            ILogger.Error($"❌ Failed to process packet {rawPacket.Id}: {ex.Message}");
            
            _provenanceTracker?.RecordError(rawPacket.Id, "WeatherDataTransformer", "Transform", ex);
        }
    }

    // ========================================
    // Core Transformation Logic
    // ========================================

    private void TransformAndPublish(IRawPacketRecordTyped iRawPacketRecordTyped, bool isRetransformation)
    {
        var transformStart = DateTime.UtcNow;

        IWeatherReading? reading = iRawPacketRecordTyped.PacketEnum switch
        {
            PacketEnum.Observation => ParseObservation(iRawPacketRecordTyped, isRetransformation),
            PacketEnum.Wind => ParseWind(iRawPacketRecordTyped, isRetransformation),
            PacketEnum.Precipitation => ParsePrecipitation(iRawPacketRecordTyped, isRetransformation),
            PacketEnum.Lightning => ParseLightning(iRawPacketRecordTyped, isRetransformation),
            _ => null
        };

        if (reading is null)
        {
            ILogger.Warning($"⚠️ Failed to parse {iRawPacketRecordTyped.PacketEnum} packet {iRawPacketRecordTyped.Id}");
            return;
        }

        var transformEnd = DateTime.UtcNow;

        // Track transformation in provenance
        _provenanceTracker?.LinkTransformedReading(iRawPacketRecordTyped.Id, reading.Id);
        _provenanceTracker?.AddStep(
            iRawPacketRecordTyped.Id, 
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
                IEventRelayBasic.Send(obs);  // ✅ Send as IObservationReading
                break;
            case IWindReading wind:
                IEventRelayBasic.Send(wind);  // ✅ Send as IWindReading
                break;
            case IPrecipitationReading precip:
                IEventRelayBasic.Send(precip);  // ✅ Send as IPrecipitationReading
                break;
            case ILightningReading lightning:
                IEventRelayBasic.Send(lightning);  // ✅ Send as ILightningReading
                break;
            default:
                ILogger.Warning($"⚠️ Unknown reading type: {reading.GetType().Name}");
                break;
        }

        ILogger.Debug($"📤 Published {reading.PacketType} reading: {reading.Id}");
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
                ILogger.Warning($"⚠️ Failed to parse observation packet {rawPacket.Id}");
                ILogger.Warning($"observation contents {rawPacket.RawPacketJson}");
                return null;
            }

            // Convert from Tempest's METRIC units to user preferences
            var temperature = new Amount(reading.AirTemperature, TemperatureUnits.DegreeCelsius)
                .ConvertedTo(_preferredUnits[MeasurementTypeEnum.AirTemperature]);
            
            var pressure = new Amount(reading.StationPressure, PressureUnits.MilliBar)
                .ConvertedTo(_preferredUnits[MeasurementTypeEnum.AirPressure]);

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
                    TargetUnits = 
                        $"{_preferredUnits[MeasurementTypeEnum.AirTemperature].Name}," +
                        $"{_preferredUnits[MeasurementTypeEnum.AirPressure].Name}",
                    TransformerVersion = isRetransformation ? "1.0-retransform" : "1.0"
                }
            };
        }
        catch (Exception ex)
        {
            ILogger.Error($"❌ Failed to parse Observation packet: {ex.Message}");
            ILogger.Debug($"   Stack trace: {ex.StackTrace}");
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
                ILogger.Warning($"⚠️ Failed to parse wind packet {rawPacket.Id}");
                return null;
            }

            // Convert from Tempest's METRIC units (m/s) to user preferences
            var speed = new Amount(windDto.WindSpeed, SpeedUnits.MeterPerSecond)
                .ConvertedTo(_preferredUnits[MeasurementTypeEnum.WindSpeed]);

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
                    TargetUnits = _preferredUnits[MeasurementTypeEnum.WindSpeed].Name,
                    TransformerVersion = isRetransformation ? "1.0-retransform" : "1.0"
                }
            };
        }
        catch (Exception ex)
        {
            ILogger.Error($"❌ Failed to parse Wind packet: {ex.Message}");
            ILogger.Debug($"   Stack trace: {ex.StackTrace}");
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
                ILogger.Warning($"⚠️ Failed to parse precipitation packet {rawPacket.Id}");
                return null;
            }

            // evt_precip is just a notification event (no rate data in the event itself)
            return new PrecipitationReading
            {
                Id = IdGenerator.CreateCombGuid(),
                SourcePacketId = rawPacket.Id,
                Timestamp = DateTimeOffset.FromUnixTimeSeconds(precipDto.DeviceReceivedUtcTimestampEpoch).UtcDateTime,
                ReceivedUtc = rawPacket.ReceivedTime,
                RainRate = new Amount(0, LengthUnits.MilliMeter).ConvertedTo(_preferredUnits[MeasurementTypeEnum.PrecipitationAmount]),
                DailyAccumulation = null,  // Get from observation packet
                Provenance = new ReadingProvenance
                {
                    RawPacketId = rawPacket.Id,
                    UdpReceiptTime = rawPacket.ReceivedTime,
                    TransformStartTime = DateTime.UtcNow,
                    TransformEndTime = DateTime.UtcNow,
                    SourceUnits = "millimeter",
                    TargetUnits = _preferredUnits[MeasurementTypeEnum.PrecipitationAmount].Name,
                    TransformerVersion = isRetransformation ? "1.0-retransform" : "1.0"
                }
            };
        }
        catch (Exception ex)
        {
            ILogger.Error($"❌ Failed to parse Precipitation packet: {ex.Message}");
            ILogger.Debug($"   Stack trace: {ex.StackTrace}");
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
                ILogger.Warning($"⚠️ Failed to parse lightning packet {rawPacket.Id}");
                return null;
            }

            // Convert from Tempest's METRIC units (km) to user preferences
            var strikeDistance = new Amount(lightningDto.LightningStrikeDistanceKm, LengthUnits.KiloMeter)
                .ConvertedTo(_preferredUnits[MeasurementTypeEnum.LightningDistance]);

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
                    TargetUnits = _preferredUnits[MeasurementTypeEnum.LightningDistance].Name,
                    TransformerVersion = isRetransformation ? "1.0-retransform" : "1.0"
                }
            };
        }
        catch (Exception ex)
        {
            ILogger.Error($"❌ Failed to parse Lightning packet: {ex.Message}");
            ILogger.Debug($"   Stack trace: {ex.StackTrace}");
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
            IEventRelayBasic.Unregister<IRawPacketRecordTyped>(this);

            ILogger.Information("🛑 WeatherDataTransformer disposed");
        }
        catch (Exception ex)
        {
            ILogger?.Warning($"⚠️ Error during WeatherDataTransformer disposal: {ex.Message}");
        }

        await Task.CompletedTask;
    }
}