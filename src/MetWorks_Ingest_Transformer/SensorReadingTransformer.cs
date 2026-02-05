namespace MetWorks.Ingest.Transformer;
/// <summary>
/// Transforms raw UDP packets into typed weather readings with user-preferred units.
/// Implements event-driven retransformation: when unit settings change,
/// automatically retransforms cached packets and publishes updated readings.
/// </summary>
public class SensorReadingTransformer : ServiceBase
{
    IStationMetadataProvider? _stationMetadataProvider;
    IStationMetadataProvider IStationMetadataProvider => NullPropertyGuard.Get(_isInitialized, _stationMetadataProvider, nameof(IStationMetadataProvider));

    // ========================================
    // Last-Packet Cache (LRU pattern)
    // ========================================
    /// <summary>
    /// Maintains the most recent packet per PacketEnum type for retransformation.
    /// When settings change, we retransform ALL cached packets with new unit preferences.
    /// </summary>
    readonly ConcurrentDictionary<PacketEnum, IRawPacketRecordTyped> _lastPacketCache = new();
    // ========================================
    // Track if a retransformation is in progress (optional)
    // ========================================
    // (Background tasks are tracked by ServiceBase via StartBackground)
    // ========================================
    // Initialization
    // ========================================
    public async Task<bool> InitializeAsync(
        ILoggerResilient iLoggerResilient,
        ISettingRepository iSettingRepository,
        IEventRelayBasic iEventRelayBasic,
        CancellationToken externalCancellation = default,
        ProvenanceTracker? provenanceTracker = null,
        IStationMetadataProvider? iStationMetadataProvider = null
    )
    {
        try
        {
            InitializeBase(
                iLoggerResilient.ForContext(this.GetType()),
                iSettingRepository,
                iEventRelayBasic,
                externalCancellation,
                provenanceTracker
            );

            _stationMetadataProvider = iStationMetadataProvider;
            iLoggerResilient.Information($"🔍 Provenance tracking {(HaveProvenanceTracker ? string.Empty : "NOT")} enabled for sensor readings");
            // Load current unit preferences from settings
            if (!LoadUnitPreference(iLoggerResilient, iSettingRepository))
            {
                iLoggerResilient.Error("Failed to load unit preferences during initialization");
                return false;
            }
            iLoggerResilient.Information("🌡️ WeatherDataTransformer initialized successfully");
            foreach(var unitKVP in _preferredUnits)
                iLoggerResilient.Information($"   {unitKVP.Key}: {unitKVP.Value.Name}");
            // Subscribe to unit setting changes for the whole group (prefix match).
            // Use the GroupSettingDefinition to build the canonical settings path prefix
            // so the value is not hard-coded (DRY).
            var unitGroupPrefix = LookupDictionaries.UnitOfMeasureGroupSettingsDefinition.BuildGroupPath();
            IEventRelayPath.Register(unitGroupPrefix, OnUnitSettingChanged);
            // Subscribe to raw packet events
            IEventRelayBasic.Register<IRawPacketRecordTyped>(this, OnRawPacketReceived);
            MarkReady();
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            try { ILogger?.Error($"❌ WeatherDataTransformer initialization failed: {ex.Message}"); } catch { }
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
    bool LoadUnitPreference(
        ILogger iLogger,
        ISettingRepository iSettingRepository
    )
    {
        try
        {
            var unitOfMeasure_airPressure = iSettingRepository.GetValueOrDefault<string>(
                    LookupDictionaries.UnitOfMeasureGroupSettingsDefinition.BuildSettingPath(SettingConstants.UnitOfMeasure_airPressure)
                );
            _preferredUnits[MeasurementTypeEnum.AirPressure] = Unit.Parse(unitOfMeasure_airPressure);

            var unitOfMeasure_airTemperature = iSettingRepository.GetValueOrDefault<string>(
                    LookupDictionaries.UnitOfMeasureGroupSettingsDefinition.BuildSettingPath(SettingConstants.UnitOfMeasure_airTemperature)
                );
            _preferredUnits[MeasurementTypeEnum.AirTemperature] = Unit.Parse(unitOfMeasure_airTemperature);

            var unitOfMeasure_batteryLevel = iSettingRepository.GetValueOrDefault<string>(
                    LookupDictionaries.UnitOfMeasureGroupSettingsDefinition.BuildSettingPath(SettingConstants.UnitOfMeasure_batteryLevel)
                );
            _preferredUnits[MeasurementTypeEnum.BatteryLevel] = Unit.Parse(unitOfMeasure_batteryLevel);

            var unitOfMeasure_illuminance = iSettingRepository.GetValueOrDefault<string>(
                    LookupDictionaries.UnitOfMeasureGroupSettingsDefinition.BuildSettingPath(SettingConstants.UnitOfMeasure_illuminance)
                );
            _preferredUnits[MeasurementTypeEnum.Illuminance] = Unit.Parse(unitOfMeasure_illuminance);

            var unitOfMeasure_lightningDistance = iSettingRepository.GetValueOrDefault<string>(
                    LookupDictionaries.UnitOfMeasureGroupSettingsDefinition.BuildSettingPath(SettingConstants.UnitOfMeasure_lightningDistance)
                );
            _preferredUnits[MeasurementTypeEnum.LightningDistance] = Unit.Parse(unitOfMeasure_lightningDistance);

            var unitOfMeasure_rainAccumulation = iSettingRepository.GetValueOrDefault<string>(
                    LookupDictionaries.UnitOfMeasureGroupSettingsDefinition.BuildSettingPath(SettingConstants.UnitOfMeasure_rainAccumulation)
                );
            _preferredUnits[MeasurementTypeEnum.RainAccumulation] = Unit.Parse(unitOfMeasure_rainAccumulation);

            var unitOfMeasure_solarRadiation = iSettingRepository.GetValueOrDefault<string>(
                    LookupDictionaries.UnitOfMeasureGroupSettingsDefinition.BuildSettingPath(SettingConstants.UnitOfMeasure_solarRadiation)
                );
            _preferredUnits[MeasurementTypeEnum.SolarRadiation] = Unit.Parse(unitOfMeasure_solarRadiation);

            var unitOfMeasure_windSpeed = iSettingRepository.GetValueOrDefault<string>(
                    LookupDictionaries.UnitOfMeasureGroupSettingsDefinition.BuildSettingPath(SettingConstants.UnitOfMeasure_windSpeed)
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
        // Use ServiceBase StartBackground so the background task observes LinkedCancellationToken
        StartBackground(token =>
        {
            if (token.IsCancellationRequested) return Task.CompletedTask;

            try
            {
                ILogger.Information($"🔄 Unit setting changed");
                LoadUnitPreference(ILogger, ISettingRepository);

                // Schedule retransformation in background (tracked by ServiceBase)
                StartBackground(innerToken =>
                {
                    if (innerToken.IsCancellationRequested) return Task.CompletedTask;
                    RetransformCachedPackets();
                    return Task.CompletedTask;
                });

            }
            catch (Exception ex)
            {
                try { ILogger.Warning($"⚠️ Unit setting handler failed: {ex.Message}"); } catch { }
            }

            return Task.CompletedTask;
        });
    }
    // ========================================
    // Retransformation Logic
    // ========================================
    private void RetransformCachedPackets()
    {
        if (LinkedCancellationToken.IsCancellationRequested) return;

        var cachedCount = _lastPacketCache.Count;
        if (cachedCount == 0)
        {
            ILogger.Debug("No cached packets to retransform");
            return;
        }

        ILogger.Information($"🔄 Retransforming {cachedCount} cached packets with new unit preferences");

        foreach (var kvp in _lastPacketCache)
        {
            if (LinkedCancellationToken.IsCancellationRequested) break;

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
                
                ProvenanceTracker?.RecordError(rawPacket.Id, "WeatherDataTransformer", "Retransform", ex);
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

        // Use ServiceBase StartBackground so the background task observes LinkedCancellationToken
        StartBackground(token =>
        {
            if (token.IsCancellationRequested) return Task.CompletedTask;

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

                ProvenanceTracker?.RecordError(rawPacket.Id, "WeatherDataTransformer", "Transform", ex);
            }

            return Task.CompletedTask;
        });
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
        ProvenanceTracker?.LinkTransformedReading(iRawPacketRecordTyped.Id, reading.Id);
        ProvenanceTracker?.AddStep(
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
            case IObservationReading iObservationReading:
                IEventRelayBasic.Send(iObservationReading);  // ✅ Send as IObservationReading
                break;
            case IWindReading iWindReading:
                IEventRelayBasic.Send(iWindReading);  // ✅ Send as IWindReading
                break;
            case IPrecipitationReading iPrecipitationReading:
                IEventRelayBasic.Send(iPrecipitationReading);  // ✅ Send as IPrecipitationReading
                break;
            case ILightningReading iLightningReading:
                IEventRelayBasic.Send(iLightningReading);  // ✅ Send as ILightningReading
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
    private IObservationReading? ParseObservation(
        IRawPacketRecordTyped rawPacket, 
        bool isRetransformation
    )
    {
        try
        {
            // Use public parser API (keeps DTOs internal)
            var iObservationReadingDto = TempestPacketParser.ParseObservation(rawPacket);
            if (iObservationReadingDto is null)
            {
                ILogger.Warning($"⚠️ Failed to parse observation packet {rawPacket.Id}");
                ILogger.Warning($"observation contents {rawPacket.RawPacketJson}");
                return null;
            }

            var airTemperature = new Amount(iObservationReadingDto.AirTemperature, TemperatureUnits.DegreeCelsius)
                .ConvertedTo(_preferredUnits[MeasurementTypeEnum.AirTemperature]);

            var windAverage = new Amount(iObservationReadingDto.WindAverage, SpeedUnits.MeterPerSecond)
                .ConvertedTo(_preferredUnits[MeasurementTypeEnum.WindSpeed]);

            var stationPressure = new Amount(iObservationReadingDto.StationPressure, PressureUnits.MilliBar)
                .ConvertedTo(_preferredUnits[MeasurementTypeEnum.AirPressure]);

            var stationElevation = IStationMetadataProvider.GetStationElevationMetersAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            var atmosphericPressure = DerivedObservationCalculator.TryComputeSeaLevelPressure(
                stationPressure,
                airTemperature,
                stationElevation
            )
                ?.ConvertedTo(_preferredUnits[MeasurementTypeEnum.AirPressure]);

            var dewPoint = DerivedObservationCalculator.TryComputeDewPoint(airTemperature, iObservationReadingDto.RelativeHumidity)
                ?.ConvertedTo(_preferredUnits[MeasurementTypeEnum.AirTemperature]);

            var windChill = DerivedObservationCalculator.TryComputeWindChill(airTemperature, windAverage)
                ?.ConvertedTo(_preferredUnits[MeasurementTypeEnum.AirTemperature]);

            var heatIndex = DerivedObservationCalculator.TryComputeHeatIndex(airTemperature, iObservationReadingDto.RelativeHumidity)
                ?.ConvertedTo(_preferredUnits[MeasurementTypeEnum.AirTemperature]);

            var feelsLike = DerivedObservationCalculator.ComputeFeelsLike(airTemperature, windChill, heatIndex)
                ?.ConvertedTo(_preferredUnits[MeasurementTypeEnum.AirTemperature]);

            return new ObservationReading
            {
                HubSerialNumber = iObservationReadingDto.HubSerialNumber,
                SerialNumber = iObservationReadingDto.SerialNumber,
                Type = iObservationReadingDto.Type,

                Id = IdGenerator.CreateCombGuid(),
                SourcePacketId = rawPacket.Id,
                Timestamp = DateTimeOffset.FromUnixTimeSeconds(iObservationReadingDto.EpochTimestampUtc).UtcDateTime.ToLocalTime(),
                ReceivedUtc = rawPacket.ReceivedTime,
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
                },

                AirTemperature = airTemperature,
                BatteryLevel = new Amount(iObservationReadingDto.BatteryLevel, ElectricUnits.Volt)
                    .ConvertedTo(_preferredUnits[MeasurementTypeEnum.BatteryLevel]),
                EpochTimeOfMeasurement = iObservationReadingDto.EpochTimestampUtc,
                Illuminance = new Amount(iObservationReadingDto.Illuminance, LuminousIntensityUnits.Lux)
                    .ConvertedTo(_preferredUnits[MeasurementTypeEnum.Illuminance]),
                LightningStrikeAverageDistance = new Amount(iObservationReadingDto.LightningStrikeAverageDistance, LengthUnits.KiloMeter)
                    .ConvertedTo(_preferredUnits[MeasurementTypeEnum.LightningDistance]),
                LightningStrikeCount = iObservationReadingDto.LightningStrikeCount,
                PrecipitationType = iObservationReadingDto.PrecipitationType,
                RainAccumulation = new Amount(iObservationReadingDto.RainAccumulation, LengthUnits.MilliMeter)
                    .ConvertedTo(_preferredUnits[MeasurementTypeEnum.RainAccumulation]),
                ReportingInterval = iObservationReadingDto.ReportingInterval,
                WindAverage = windAverage,
                WindDirection = iObservationReadingDto.WindDirection,
                WindGust = new Amount(iObservationReadingDto.WindGust, SpeedUnits.MeterPerSecond)
                    .ConvertedTo(_preferredUnits[MeasurementTypeEnum.WindSpeed]),
                WindLull = new Amount(iObservationReadingDto.WindLull, SpeedUnits.MeterPerSecond)
                    .ConvertedTo(_preferredUnits[MeasurementTypeEnum.WindSpeed]),
                WindSampleInterval = iObservationReadingDto.WindSampleInterval,
                RelativeHumidity = iObservationReadingDto.RelativeHumidity,
                StationPressure = stationPressure,
                UvIndex = iObservationReadingDto.UvIndex,
                SolarRadiation = new Amount(iObservationReadingDto.StationPressure, SolarRadiationUnits.WattPerSquareMeter)
                    .ConvertedTo(_preferredUnits[MeasurementTypeEnum.SolarRadiation]),

                // Derived
                DewPoint = dewPoint,
                WindChill = windChill,
                HeatIndex = heatIndex,
                FeelsLike = feelsLike,
                AtmosphericPressure = atmosphericPressure
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
                HubSerialNumber = windDto.HubSerialNumber,
                SerialNumber = windDto.SerialNumber,
                Type = windDto.Type,

                Id = IdGenerator.CreateCombGuid(),
                SourcePacketId = rawPacket.Id,
                Timestamp = DateTimeOffset.FromUnixTimeSeconds(windDto.DeviceReceivedUtcTimestampEpoch).UtcDateTime.ToLocalTime(),
                ReceivedUtc = rawPacket.ReceivedTime,
                Speed = speed,
                DeviceReceivedUtcTimestampEpoch = windDto.DeviceReceivedUtcTimestampEpoch,
                DirectionDegrees = windDto.WindDirection,
                DirectionCardinal = DegreesToCardinal(windDto.WindDirection),
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
    private IPrecipitationReading? ParsePrecipitation(
        IRawPacketRecordTyped rawPacket, 
        bool isRetransformation
    )
    {
        try
        {
            // Use public parser API
            var iPrecipitationDto = TempestPacketParser.ParsePrecipitation(rawPacket);
            if (iPrecipitationDto is null)
            {
                ILogger.Warning($"⚠️ Failed to parse precipitation packet {rawPacket.Id}");
                return null;
            }
            return new PrecipitationReading
            {
                HubSerialNumber = iPrecipitationDto.HubSerialNumber,
                SerialNumber = iPrecipitationDto.SerialNumber,
                Type = iPrecipitationDto.Type,

                Id = IdGenerator.CreateCombGuid(),
                SourcePacketId = rawPacket.Id,
                Timestamp = DateTimeOffset.FromUnixTimeSeconds(iPrecipitationDto.DeviceReceivedUtcTimestampEpoch).UtcDateTime.ToLocalTime(),
                ReceivedUtc = rawPacket.ReceivedTime,
                RainRate = new Amount(0, LengthUnits.MilliMeter).ConvertedTo(_preferredUnits[MeasurementTypeEnum.RainAccumulation]),
                DailyAccumulation = null,  // Get from observation packet
                Provenance = new ReadingProvenance
                {
                    RawPacketId = rawPacket.Id,
                    UdpReceiptTime = rawPacket.ReceivedTime,
                    TransformStartTime = DateTime.UtcNow,
                    TransformEndTime = DateTime.UtcNow,
                    SourceUnits = "millimeter",
                    TargetUnits = _preferredUnits[MeasurementTypeEnum.RainAccumulation].Name,
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
            var iLightningDto = TempestPacketParser.ParseLightning(rawPacket);
            if (iLightningDto is null)
            {
                ILogger.Warning($"⚠️ Failed to parse lightning packet {rawPacket.Id}");
                return null;
            }

            return new LightningReading
            {
                HubSerialNumber = iLightningDto.HubSerialNumber,
                SerialNumber = iLightningDto.SerialNumber,
                Type = iLightningDto.Type,

                Id = IdGenerator.CreateCombGuid(),
                SourcePacketId = rawPacket.Id,
                Timestamp = DateTimeOffset.FromUnixTimeSeconds(iLightningDto.DeviceReceivedUtcTimestampEpoch).UtcDateTime.ToLocalTime(),
                ReceivedUtc = rawPacket.ReceivedTime,
                StrikeDistance = new Amount(iLightningDto.LightningStrikeDistanceKm, LengthUnits.KiloMeter)
                    .ConvertedTo(_preferredUnits[MeasurementTypeEnum.LightningDistance]),
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
    protected override Task OnDisposeAsync()
    {
        try
        {
            // Unregister event handlers (use backing fields to avoid NullPropertyGuard issues if Dispose called early)
            try { IEventRelayBasic?.Unregister<IRawPacketRecordTyped>(this); } catch { /* swallow */ }
            try { IEventRelayPath?.Unregister(LookupDictionaries.UnitOfMeasureGroupSettingsDefinition.BuildGroupPath(), OnUnitSettingChanged); } catch { /* swallow */ }

            ILogger.Information("🛑 WeatherDataTransformer disposing");
        }
        catch (Exception ex)
        {
            try { ILogger?.Warning($"⚠️ Error during WeatherDataTransformer disposal: {ex.Message}"); } catch { }
        }

        return Task.CompletedTask;
    }
}