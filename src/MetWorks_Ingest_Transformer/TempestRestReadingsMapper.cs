namespace MetWorks.Ingest.Transformer;
/// <summary>
/// Best-effort mapping from a Tempest REST observations snapshot to UI-compatible readings.
/// Intended for use by the UDP/REST mux so the UI can remain bound to `IObservationReading` and `IWindReading`.
/// </summary>
internal static class TempestRestReadingsMapper
{
    const string TransformerVersion = "rest-1.0";

    // Tempest station observations "obs" array indexes (matches WeatherFlow docs and aligns with existing UDP observation mapping).
    const int ObsIdx_EpochSeconds = 0;
    const int ObsIdx_WindLullMps = 1;
    const int ObsIdx_WindAvgMps = 2;
    const int ObsIdx_WindGustMps = 3;
    const int ObsIdx_WindDirectionDegrees = 4;
    const int ObsIdx_WindSampleIntervalSeconds = 5;
    const int ObsIdx_StationPressureMbar = 6;
    const int ObsIdx_AirTempC = 7;
    const int ObsIdx_RelativeHumidityPercent = 8;
    const int ObsIdx_IlluminanceLux = 9;
    const int ObsIdx_UvIndex = 10;
    const int ObsIdx_SolarRadiationWPerM2 = 11;
    const int ObsIdx_RainAccumulationMm = 12;
    const int ObsIdx_PrecipType = 13;
    const int ObsIdx_LightningAvgDistanceKm = 14;
    const int ObsIdx_LightningCount = 15;
    const int ObsIdx_BatteryVolts = 16;
    const int ObsIdx_ReportingIntervalMinutes = 17;

    public static async Task<(ObservationReading? Observation, WindReading? Wind, int? TimeZoneOffsetMinutes)> TryMapAsync(
        TempestRestObservationsSnapshot snapshot,
        ILogger logger,
        ISettingRepository settingRepository,
        IStationMetadataProvider? stationMetadataProvider,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(settingRepository);

        cancellationToken.ThrowIfCancellationRequested();

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(snapshot.RawJson);
        }
        catch (JsonException ex)
        {
            logger.Warning($"TempestRestReadingsMapper: invalid JSON payload. {ex.Message}");
            return (null, null, null);
        }

        using (doc)
        {
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return (null, null, null);

            if (!TryGetInt(root, "timezone_offset_minutes", out var tzOffsetMinutes))
                tzOffsetMinutes = null;

            var preferredUnits = LoadPreferredUnits(logger, settingRepository);

            if (!TryGetFirstObsElement(root, out var obsElement))
                return (null, null, tzOffsetMinutes);

            if (obsElement.ValueKind == JsonValueKind.Object)
            {
                return await TryMapFromObsObjectAsync(
                    snapshot,
                    root,
                    obsElement,
                    tzOffsetMinutes,
                    preferredUnits,
                    logger,
                    stationMetadataProvider,
                    cancellationToken).ConfigureAwait(false);
            }

            if (obsElement.ValueKind == JsonValueKind.Array)
            {
                return await TryMapFromObsArrayAsync(
                    snapshot,
                    obsElement,
                    tzOffsetMinutes,
                    preferredUnits,
                    logger,
                    stationMetadataProvider,
                    cancellationToken).ConfigureAwait(false);
            }

            return (null, null, tzOffsetMinutes);
        }
    }

    static async Task<(ObservationReading? Observation, WindReading? Wind, int? TimeZoneOffsetMinutes)> TryMapFromObsArrayAsync(
        TempestRestObservationsSnapshot snapshot,
        JsonElement obsRow,
        int? tzOffsetMinutes,
        Dictionary<MeasurementTypeEnum, Unit> preferredUnits,
        ILogger logger,
        IStationMetadataProvider? stationMetadataProvider,
        CancellationToken cancellationToken
    )
    {
        if (!TryGetInt64(obsRow, ObsIdx_EpochSeconds, out var epochSeconds) || epochSeconds <= 0)
            return (null, null, tzOffsetMinutes);

        var transformStartUtc = DateTime.UtcNow;

        var receivedUtc = snapshot.RetrievedUtc.UtcDateTime;
        var utc = DateTimeOffset.FromUnixTimeSeconds(epochSeconds);
        var local = tzOffsetMinutes is not null
            ? utc.ToOffset(TimeSpan.FromMinutes(tzOffsetMinutes.Value))
            : utc.ToLocalTime();

        var idBase = IdGenerator.CreateCombGuid();

        // Direct-from-device values (REST payload is expected to be metric).
        var stationPressure = TryGetDouble(obsRow, ObsIdx_StationPressureMbar, out var stationPressureMbar)
            ? new Amount(stationPressureMbar, PressureUnits.MilliBar)
            : null;

        var airTemperature = TryGetDouble(obsRow, ObsIdx_AirTempC, out var airTempC)
            ? new Amount(airTempC, TemperatureUnits.DegreeCelsius)
            : null;

        var rh = TryGetDouble(obsRow, ObsIdx_RelativeHumidityPercent, out var rhPercent)
            ? rhPercent
            : (double?)null;

        var windAvg = TryGetDouble(obsRow, ObsIdx_WindAvgMps, out var windAvgMps)
            ? new Amount(windAvgMps, SpeedUnits.MeterPerSecond)
            : null;

        var windGust = TryGetDouble(obsRow, ObsIdx_WindGustMps, out var windGustMps)
            ? new Amount(windGustMps, SpeedUnits.MeterPerSecond)
            : null;

        var windLull = TryGetDouble(obsRow, ObsIdx_WindLullMps, out var windLullMps)
            ? new Amount(windLullMps, SpeedUnits.MeterPerSecond)
            : null;

        var illuminance = TryGetDouble(obsRow, ObsIdx_IlluminanceLux, out var illumLux)
            ? new Amount(illumLux, LuminousIntensityUnits.Lux)
            : null;

        var uv = TryGetDouble(obsRow, ObsIdx_UvIndex, out var uvIdx)
            ? uvIdx
            : (double?)null;

        var solar = TryGetDouble(obsRow, ObsIdx_SolarRadiationWPerM2, out var solarWPerM2)
            ? new Amount(solarWPerM2, SolarRadiationUnits.WattPerSquareMeter)
            : null;

        var rain = TryGetDouble(obsRow, ObsIdx_RainAccumulationMm, out var rainMm)
            ? new Amount(rainMm, LengthUnits.MilliMeter)
            : null;

        var lightningDist = TryGetDouble(obsRow, ObsIdx_LightningAvgDistanceKm, out var lightningKm)
            ? new Amount(lightningKm, LengthUnits.KiloMeter)
            : null;

        _ = TryGetInt(obsRow, ObsIdx_LightningCount, out var lightningCount);
        _ = TryGetInt(obsRow, ObsIdx_PrecipType, out var precipType);
        _ = TryGetInt(obsRow, ObsIdx_WindDirectionDegrees, out var windDirDegrees);
        _ = TryGetInt(obsRow, ObsIdx_WindSampleIntervalSeconds, out var windSampleInterval);
        _ = TryGetInt(obsRow, ObsIdx_ReportingIntervalMinutes, out var reportingInterval);

        var battery = TryGetDouble(obsRow, ObsIdx_BatteryVolts, out var batteryVolts)
            ? new Amount(batteryVolts, ElectricUnits.Volt)
            : null;

            // Convert to preferred units.
            Amount? Convert(Amount? amount, MeasurementTypeEnum measurementType)
            {
                if (amount is null) return null;
                return preferredUnits.TryGetValue(measurementType, out var unit)
                    ? amount.ConvertedTo(unit)
                    : amount;
            }

            var stationPressurePreferred = Convert(stationPressure, MeasurementTypeEnum.AirPressure);
            var airTemperaturePreferred = Convert(airTemperature, MeasurementTypeEnum.AirTemperature);
            var windAvgPreferred = Convert(windAvg, MeasurementTypeEnum.WindSpeed);
            var windGustPreferred = Convert(windGust, MeasurementTypeEnum.WindSpeed);
            var windLullPreferred = Convert(windLull, MeasurementTypeEnum.WindSpeed);
            var illuminancePreferred = Convert(illuminance, MeasurementTypeEnum.Illuminance);
            var rainPreferred = Convert(rain, MeasurementTypeEnum.RainAccumulation);
            var lightningDistPreferred = Convert(lightningDist, MeasurementTypeEnum.LightningDistance);
            var solarPreferred = Convert(solar, MeasurementTypeEnum.SolarRadiation);
            var batteryPreferred = Convert(battery, MeasurementTypeEnum.BatteryLevel);

            // Derived values.
            var dewPoint = DerivedObservationCalculator.TryComputeDewPoint(airTemperaturePreferred, rh);
            var windChill = DerivedObservationCalculator.TryComputeWindChill(airTemperaturePreferred, windAvgPreferred);
            var heatIndex = DerivedObservationCalculator.TryComputeHeatIndex(airTemperaturePreferred, rh);
            var feelsLike = airTemperaturePreferred is not null
                ? DerivedObservationCalculator.ComputeFeelsLike(airTemperaturePreferred, windChill, heatIndex)
                : null;

            double? elevationMeters = null;
            if (stationMetadataProvider is not null)
            {
                try
                {
                    elevationMeters = await stationMetadataProvider.GetStationElevationMetersAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (InvalidOperationException ex)
                {
                    // Offline or missing snapshot is expected sometimes; keep sea-level pressure optional.
                    logger.Warning($"TempestRestReadingsMapper: failed to get station elevation. {ex.Message}");
                }
                catch (IOException ex)
                {
                    logger.Warning($"TempestRestReadingsMapper: failed to read station elevation. {ex.Message}");
                }
            }

            var seaLevelPressure = DerivedObservationCalculator.TryComputeSeaLevelPressure(
                stationPressurePreferred,
                airTemperaturePreferred,
                elevationMeters);

            var transformEndUtc = DateTime.UtcNow;

            var provenance = new ReadingProvenance
            {
                RawPacketId = Guid.Empty,
                UdpReceiptTime = receivedUtc,
                TransformStartTime = transformStartUtc,
                TransformEndTime = transformEndUtc,
                SourceUnits = "c, mbar, mps, lux, w/m2, mm, km, v",
                TargetUnits = BuildTargetUnits(preferredUnits),
                TransformerVersion = TransformerVersion
            };

            // These base fields are required by the existing model.
            var hub = $"rest:{snapshot.StationId.ToString(CultureInfo.InvariantCulture)}";

        ObservationReading? observationReading = null;
            if (airTemperaturePreferred is not null
                && stationPressurePreferred is not null
                && windAvgPreferred is not null
                && windGustPreferred is not null
                && windLullPreferred is not null
                && illuminancePreferred is not null
                && lightningDistPreferred is not null
                && rainPreferred is not null
                && solarPreferred is not null
                && batteryPreferred is not null
                && rh is not null
                && uv is not null
            )
            {
                observationReading = new ObservationReading
                {
                    HubSerialNumber = hub,
                    SerialNumber = hub,
                    Type = "rest-observation",

                    Id = idBase,
                    SourcePacketId = Guid.Empty,
                    Timestamp = local.DateTime,
                    ReceivedUtc = receivedUtc,
                    Provenance = provenance,

                    AirTemperature = airTemperaturePreferred,
                    BatteryLevel = batteryPreferred,
                    EpochTimeOfMeasurement = epochSeconds,
                    Illuminance = illuminancePreferred,
                    LightningStrikeAverageDistance = lightningDistPreferred,
                    LightningStrikeCount = lightningCount ?? 0,
                    PrecipitationType = precipType ?? 0,
                    RainAccumulation = rainPreferred,
                    RelativeHumidity = rh.Value,
                    ReportingInterval = reportingInterval ?? 0,
                    SolarRadiation = solarPreferred,
                    StationPressure = stationPressurePreferred,
                    UvIndex = uv.Value,
                    WindAverage = windAvgPreferred,
                    WindDirection = windDirDegrees ?? 0,
                    WindGust = windGustPreferred,
                    WindLull = windLullPreferred,
                    WindSampleInterval = windSampleInterval ?? 0,

                    DewPoint = dewPoint,
                    WindChill = windChill,
                    HeatIndex = heatIndex,
                    FeelsLike = feelsLike,
                    AtmosphericPressure = seaLevelPressure
                };
            }

        WindReading? windReading = null;
            if (windAvgPreferred is not null && windDirDegrees is not null)
            {
                windReading = new WindReading
                {
                    HubSerialNumber = hub,
                    SerialNumber = hub,
                    Type = "rest-wind",

                    Id = IdGenerator.CreateCombGuid(),
                    SourcePacketId = Guid.Empty,
                    Timestamp = local.DateTime,
                    ReceivedUtc = receivedUtc,
                    Provenance = provenance,

                    DeviceReceivedUtcTimestampEpoch = epochSeconds,
                    Speed = windAvgPreferred,
                    DirectionDegrees = windDirDegrees.Value,
                    DirectionCardinal = DegreesToCardinal(windDirDegrees.Value)
                };
            }

            return (observationReading, windReading, tzOffsetMinutes);
    }

    static async Task<(ObservationReading? Observation, WindReading? Wind, int? TimeZoneOffsetMinutes)> TryMapFromObsObjectAsync(
        TempestRestObservationsSnapshot snapshot,
        JsonElement root,
        JsonElement obs,
        int? tzOffsetMinutes,
        Dictionary<MeasurementTypeEnum, Unit> preferredUnits,
        ILogger logger,
        IStationMetadataProvider? stationMetadataProvider,
        CancellationToken cancellationToken)
    {
        // Required core fields for a usable reading.
        if (!TryGetDouble(obs, "air_temperature", out var airTempRaw))
            return (null, null, tzOffsetMinutes);

        if (!TryGetDouble(obs, "station_pressure", out var stationPressureRaw)
            && !TryGetDouble(obs, "barometric_pressure", out stationPressureRaw))
            return (null, null, tzOffsetMinutes);

        if (!TryGetDouble(obs, "relative_humidity", out var relativeHumidityRaw))
            return (null, null, tzOffsetMinutes);

        if (!TryGetDouble(obs, "wind_avg", out var windAvgRaw))
            return (null, null, tzOffsetMinutes);

        if (!TryGetDouble(obs, "wind_direction", out var windDirRaw))
            return (null, null, tzOffsetMinutes);

        if (!TryGetInt64(obs, "timestamp", out var epochSeconds) || epochSeconds <= 0)
            return (null, null, tzOffsetMinutes);

        var transformStartUtc = DateTime.UtcNow;

        var receivedUtc = snapshot.RetrievedUtc.UtcDateTime;
        var utc = DateTimeOffset.FromUnixTimeSeconds(epochSeconds);
        var local = tzOffsetMinutes is not null
            ? utc.ToOffset(TimeSpan.FromMinutes(tzOffsetMinutes.Value))
            : utc.ToLocalTime();

        Amount Convert(Amount source, MeasurementTypeEnum measurementType)
        {
            return preferredUnits.TryGetValue(measurementType, out var unit)
                ? source.ConvertedTo(unit)
                : source;
        }

        Amount ConvertOrDefault(Amount? source, MeasurementTypeEnum measurementType, Unit defaultSourceUnit)
        {
            return Convert(source ?? new Amount(0, defaultSourceUnit), measurementType);
        }

        // Source amounts from Tempest observations are treated as metric (matches obs-array payload and field magnitudes).
        // The response includes a `station_units` block, but observed payload values are still metric in practice.
        var airTemperature = new Amount(airTempRaw, TemperatureUnits.DegreeCelsius);
        var stationPressure = new Amount(stationPressureRaw, PressureUnits.MilliBar);
        var windAvg = new Amount(windAvgRaw, SpeedUnits.MeterPerSecond);

        var windGust = TryGetDouble(obs, "wind_gust", out var windGustRaw)
            ? new Amount(windGustRaw, SpeedUnits.MeterPerSecond)
            : (Amount?)null;

        var windLull = TryGetDouble(obs, "wind_lull", out var windLullRaw)
            ? new Amount(windLullRaw, SpeedUnits.MeterPerSecond)
            : (Amount?)null;

        var solar = TryGetDouble(obs, "solar_radiation", out var solarRaw)
            ? new Amount(solarRaw, SolarRadiationUnits.WattPerSquareMeter)
            : (Amount?)null;

        // Tempest uses "brightness" in this payload; treat as lux.
        var illuminance = TryGetDouble(obs, "brightness", out var brightnessRaw)
            ? new Amount(brightnessRaw, LuminousIntensityUnits.Lux)
            : (Amount?)null;

        // Tempest "precip" is treated as mm.
        var rain = TryGetDouble(obs, "precip", out var precipRaw)
            ? new Amount(precipRaw, LengthUnits.MilliMeter)
            : (Amount?)null;

        var lightningDistance = TryGetDouble(obs, "lightning_strike_last_distance", out var lightningDistRaw)
            ? new Amount(lightningDistRaw, LengthUnits.KiloMeter)
            : (Amount?)null;

        _ = TryGetInt(obs, "lightning_strike_count", out var lightningCount);

        var uv = TryGetDouble(obs, "uv", out var uvIdx)
            ? uvIdx
            : (double?)null;

        // Convert to preferred units (or default to 0 values when REST doesn't provide those fields).
        var airTemperaturePreferred = Convert(airTemperature, MeasurementTypeEnum.AirTemperature);
        var stationPressurePreferred = Convert(stationPressure, MeasurementTypeEnum.AirPressure);
        var windAvgPreferred = Convert(windAvg, MeasurementTypeEnum.WindSpeed);
        var windGustPreferred = ConvertOrDefault(windGust, MeasurementTypeEnum.WindSpeed, SpeedUnits.MeterPerSecond);
        var windLullPreferred = ConvertOrDefault(windLull, MeasurementTypeEnum.WindSpeed, SpeedUnits.MeterPerSecond);
        var solarPreferred = ConvertOrDefault(solar, MeasurementTypeEnum.SolarRadiation, SolarRadiationUnits.WattPerSquareMeter);
        var illuminancePreferred = ConvertOrDefault(illuminance, MeasurementTypeEnum.Illuminance, LuminousIntensityUnits.Lux);
        var rainPreferred = ConvertOrDefault(rain, MeasurementTypeEnum.RainAccumulation, LengthUnits.MilliMeter);
        var lightningDistPreferred = ConvertOrDefault(lightningDistance, MeasurementTypeEnum.LightningDistance, LengthUnits.KiloMeter);
        var batteryPreferred = ConvertOrDefault(null, MeasurementTypeEnum.BatteryLevel, ElectricUnits.Volt);

        // Optional fields in this payload.
        var dewPointProvided = TryGetDouble(obs, "dew_point", out var dewPointRaw)
            ? Convert(new Amount(dewPointRaw, TemperatureUnits.DegreeCelsius), MeasurementTypeEnum.AirTemperature)
            : (Amount?)null;

        var windChillProvided = TryGetDouble(obs, "wind_chill", out var windChillRaw)
            ? Convert(new Amount(windChillRaw, TemperatureUnits.DegreeCelsius), MeasurementTypeEnum.AirTemperature)
            : (Amount?)null;

        var heatIndexProvided = TryGetDouble(obs, "heat_index", out var heatIndexRaw)
            ? Convert(new Amount(heatIndexRaw, TemperatureUnits.DegreeCelsius), MeasurementTypeEnum.AirTemperature)
            : (Amount?)null;

        var feelsLikeProvided = TryGetDouble(obs, "feels_like", out var feelsLikeRaw)
            ? Convert(new Amount(feelsLikeRaw, TemperatureUnits.DegreeCelsius), MeasurementTypeEnum.AirTemperature)
            : (Amount?)null;

        var dewPoint = dewPointProvided ?? DerivedObservationCalculator.TryComputeDewPoint(airTemperaturePreferred, relativeHumidityRaw);
        var windChill = windChillProvided ?? DerivedObservationCalculator.TryComputeWindChill(airTemperaturePreferred, windAvgPreferred);
        var heatIndex = heatIndexProvided ?? DerivedObservationCalculator.TryComputeHeatIndex(airTemperaturePreferred, relativeHumidityRaw);
        var feelsLike = feelsLikeProvided ?? DerivedObservationCalculator.ComputeFeelsLike(airTemperaturePreferred, windChill, heatIndex);

        Amount? seaLevelPressurePreferred = null;
        if (TryGetDouble(obs, "sea_level_pressure", out var slpRaw))
        {
            seaLevelPressurePreferred = Convert(new Amount(slpRaw, PressureUnits.MilliBar), MeasurementTypeEnum.AirPressure);
        }
        else
        {
            double? elevationMeters = null;
            if (TryGetDouble(root, "elevation", out var elevationRaw))
            {
                elevationMeters = elevationRaw;
            }
            else if (stationMetadataProvider is not null)
            {
                try
                {
                    elevationMeters = await stationMetadataProvider.GetStationElevationMetersAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (InvalidOperationException ex)
                {
                    logger.Warning($"TempestRestReadingsMapper: failed to get station elevation. {ex.Message}");
                }
                catch (IOException ex)
                {
                    logger.Warning($"TempestRestReadingsMapper: failed to read station elevation. {ex.Message}");
                }
            }

            seaLevelPressurePreferred = DerivedObservationCalculator.TryComputeSeaLevelPressure(
                stationPressurePreferred,
                airTemperaturePreferred,
                elevationMeters);
        }

        var transformEndUtc = DateTime.UtcNow;

        var provenance = new ReadingProvenance
        {
            RawPacketId = Guid.Empty,
            UdpReceiptTime = receivedUtc,
            TransformStartTime = transformStartUtc,
            TransformEndTime = transformEndUtc,
            SourceUnits = "c, mbar, mps, lux, w/m2, mm, km, v",
            TargetUnits = BuildTargetUnits(preferredUnits),
            TransformerVersion = TransformerVersion
        };

        var hub = $"rest:{snapshot.StationId.ToString(CultureInfo.InvariantCulture)}";

        var observationReading = new ObservationReading
        {
            HubSerialNumber = hub,
            SerialNumber = hub,
            Type = "rest-observation",

            Id = IdGenerator.CreateCombGuid(),
            SourcePacketId = Guid.Empty,
            Timestamp = local.DateTime,
            ReceivedUtc = receivedUtc,
            Provenance = provenance,

            AirTemperature = airTemperaturePreferred,
            BatteryLevel = batteryPreferred,
            EpochTimeOfMeasurement = epochSeconds,
            Illuminance = illuminancePreferred,
            LightningStrikeAverageDistance = lightningDistPreferred,
            LightningStrikeCount = lightningCount ?? 0,
            PrecipitationType = 0,
            RainAccumulation = rainPreferred,
            RelativeHumidity = relativeHumidityRaw,
            ReportingInterval = 0,
            SolarRadiation = solarPreferred,
            StationPressure = stationPressurePreferred,
            UvIndex = uv ?? 0,
            WindAverage = windAvgPreferred,
            WindDirection = (int)Math.Round(windDirRaw, MidpointRounding.AwayFromZero),
            WindGust = windGustPreferred,
            WindLull = windLullPreferred,
            WindSampleInterval = 0,

            DewPoint = dewPoint,
            WindChill = windChill,
            HeatIndex = heatIndex,
            FeelsLike = feelsLike,
            AtmosphericPressure = seaLevelPressurePreferred
        };

        var windReading = new WindReading
        {
            HubSerialNumber = hub,
            SerialNumber = hub,
            Type = "rest-wind",

            Id = IdGenerator.CreateCombGuid(),
            SourcePacketId = Guid.Empty,
            Timestamp = local.DateTime,
            ReceivedUtc = receivedUtc,
            Provenance = provenance,

            DeviceReceivedUtcTimestampEpoch = epochSeconds,
            Speed = windAvgPreferred,
            DirectionDegrees = windDirRaw,
            DirectionCardinal = DegreesToCardinal(windDirRaw)
        };

        return (observationReading, windReading, tzOffsetMinutes);
    }

    static Dictionary<MeasurementTypeEnum, Unit> LoadPreferredUnits(ILogger logger, ISettingRepository settingRepository)
    {
        var preferred = new Dictionary<MeasurementTypeEnum, Unit>();
        try
        {
            preferred[MeasurementTypeEnum.AirPressure] = Unit.Parse(settingRepository.GetValueOrDefault<string>(
                LookupDictionaries.UnitOfMeasureGroupSettingsDefinition.BuildPath(SettingConstants.UnitOfMeasure_airPressure)));

            preferred[MeasurementTypeEnum.AirTemperature] = Unit.Parse(settingRepository.GetValueOrDefault<string>(
                LookupDictionaries.UnitOfMeasureGroupSettingsDefinition.BuildPath(SettingConstants.UnitOfMeasure_airTemperature)));

            preferred[MeasurementTypeEnum.BatteryLevel] = Unit.Parse(settingRepository.GetValueOrDefault<string>(
                LookupDictionaries.UnitOfMeasureGroupSettingsDefinition.BuildPath(SettingConstants.UnitOfMeasure_batteryLevel)));

            preferred[MeasurementTypeEnum.Illuminance] = Unit.Parse(settingRepository.GetValueOrDefault<string>(
                LookupDictionaries.UnitOfMeasureGroupSettingsDefinition.BuildPath(SettingConstants.UnitOfMeasure_illuminance)));

            preferred[MeasurementTypeEnum.LightningDistance] = Unit.Parse(settingRepository.GetValueOrDefault<string>(
                LookupDictionaries.UnitOfMeasureGroupSettingsDefinition.BuildPath(SettingConstants.UnitOfMeasure_lightningDistance)));

            preferred[MeasurementTypeEnum.RainAccumulation] = Unit.Parse(settingRepository.GetValueOrDefault<string>(
                LookupDictionaries.UnitOfMeasureGroupSettingsDefinition.BuildPath(SettingConstants.UnitOfMeasure_rainAccumulation)));

            preferred[MeasurementTypeEnum.SolarRadiation] = Unit.Parse(settingRepository.GetValueOrDefault<string>(
                LookupDictionaries.UnitOfMeasureGroupSettingsDefinition.BuildPath(SettingConstants.UnitOfMeasure_solarRadiation)));

            preferred[MeasurementTypeEnum.WindSpeed] = Unit.Parse(settingRepository.GetValueOrDefault<string>(
                LookupDictionaries.UnitOfMeasureGroupSettingsDefinition.BuildPath(SettingConstants.UnitOfMeasure_windSpeed)));
        }
        catch (UnknownUnitException ex)
        {
            logger.Warning($"TempestRestReadingsMapper: failed to parse preferred units; will emit source units. {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            logger.Warning($"TempestRestReadingsMapper: failed to load preferred units; will emit source units. {ex.Message}");
        }
        catch (ArgumentException ex)
        {
            logger.Warning($"TempestRestReadingsMapper: invalid preferred units; will emit source units. {ex.Message}");
        }

        return preferred;
    }

    static string? BuildTargetUnits(Dictionary<MeasurementTypeEnum, Unit> preferred)
    {
        if (preferred.Count == 0) return null;

        return string.Join(", ", preferred
            .OrderBy(kvp => kvp.Key.ToString(), StringComparer.Ordinal)
            .Select(kvp => $"{kvp.Key}:{kvp.Value.Name}"));
    }

    static bool TryGetFirstObsElement(JsonElement root, out JsonElement obsElement)
    {
        obsElement = default;

        if (!root.TryGetProperty("obs", out var obsArray) || obsArray.ValueKind != JsonValueKind.Array)
            return false;

        if (obsArray.GetArrayLength() == 0)
            return false;

        var first = obsArray[0];
        if (first.ValueKind is not (JsonValueKind.Array or JsonValueKind.Object))
            return false;

        obsElement = first;
        return true;
    }

    static bool TryGetDouble(JsonElement array, int index, out double value)
    {
        value = default;
        if (!TryGetElement(array, index, out var el)) return false;
        if (el.ValueKind != JsonValueKind.Number) return false;
        return el.TryGetDouble(out value);
    }

    static bool TryGetInt(JsonElement arrayOrObject, int index, out int? value)
    {
        value = null;
        if (!TryGetElement(arrayOrObject, index, out var el)) return false;
        if (el.ValueKind != JsonValueKind.Number) return false;
        if (!el.TryGetInt32(out var v)) return false;
        value = v;
        return true;
    }

    static bool TryGetInt64(JsonElement array, int index, out long value)
    {
        value = default;
        if (!TryGetElement(array, index, out var el)) return false;
        if (el.ValueKind != JsonValueKind.Number) return false;
        return el.TryGetInt64(out value);
    }

    static bool TryGetInt(JsonElement obj, string propertyName, out int? value)
    {
        value = null;
        if (!obj.TryGetProperty(propertyName, out var el)) return false;
        if (el.ValueKind != JsonValueKind.Number) return false;
        if (!el.TryGetInt32(out var v)) return false;
        value = v;
        return true;
    }

    static bool TryGetDouble(JsonElement obj, string propertyName, out double value)
    {
        value = default;
        if (!obj.TryGetProperty(propertyName, out var el)) return false;
        if (el.ValueKind != JsonValueKind.Number) return false;
        return el.TryGetDouble(out value);
    }

    static bool TryGetInt64(JsonElement obj, string propertyName, out long value)
    {
        value = default;
        if (!obj.TryGetProperty(propertyName, out var el)) return false;
        if (el.ValueKind != JsonValueKind.Number) return false;
        return el.TryGetInt64(out value);
    }

    static bool TryGetString(JsonElement obj, string propertyName, out string value)
    {
        value = string.Empty;
        if (!obj.TryGetProperty(propertyName, out var el)) return false;
        if (el.ValueKind != JsonValueKind.String) return false;
        value = el.GetString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }

    static bool TryGetElement(JsonElement array, int index, out JsonElement el)
    {
        el = default;
        if (array.ValueKind != JsonValueKind.Array) return false;
        if (index < 0 || index >= array.GetArrayLength()) return false;
        el = array[index];
        return true;
    }

    static string DegreesToCardinal(double degrees)
    {
        // Normalize to [0, 360)
        degrees %= 360;
        if (degrees < 0) degrees += 360;

        string[] cardinals = ["N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW"];
        var index = (int)Math.Round(degrees / 22.5) % 16;
        return cardinals[index];
    }
}
