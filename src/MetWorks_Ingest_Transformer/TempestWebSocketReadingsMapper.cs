namespace MetWorks.Ingest.Transformer;

/// <summary>
/// Best-effort mapping from a Tempest WebSocket snapshot to UI-compatible readings.
/// Intended for use by the UDP/REST/WS mux so the UI can remain bound to `IObservationReading` and `IWindReading`.
/// </summary>
internal static class TempestWebSocketReadingsMapper
{
    const string TransformerVersion = "ws-1.0";

    // WebSocket obs_st "obs" array indexes (WeatherFlow/Tempest docs)
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

    // WebSocket rapid_wind "ob" array indexes
    const int RapidIdx_EpochSeconds = 0;
    const int RapidIdx_WindSpeedMps = 1;
    const int RapidIdx_WindDirectionDegrees = 2;

    public static async Task<(ObservationReading? Observation, WindReading? Wind)> TryMapAsync(
        TempestWebSocketObservationsSnapshot snapshot,
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
            logger.Warning($"TempestWebSocketReadingsMapper: invalid JSON payload. {ex.Message}");
            return (null, null);
        }

        using (doc)
        {
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return (null, null);

            var msgType = snapshot.MessageType;
            if (TryGetString(root, "type", out var parsedType) && !string.IsNullOrWhiteSpace(parsedType))
                msgType = parsedType;

            if (string.Equals(msgType, "rapid_wind", StringComparison.OrdinalIgnoreCase))
            {
                var wind = TryMapRapidWind(snapshot, root, logger, settingRepository);
                return (null, wind);
            }

            if (string.Equals(msgType, "obs_st", StringComparison.OrdinalIgnoreCase))
            {
                return await TryMapObsStAsync(snapshot, root, logger, settingRepository, stationMetadataProvider, cancellationToken)
                    .ConfigureAwait(false);
            }

            return (null, null);
        }
    }

    static WindReading? TryMapRapidWind(
        TempestWebSocketObservationsSnapshot snapshot,
        JsonElement root,
        ILogger logger,
        ISettingRepository settingRepository
    )
    {
        if (!root.TryGetProperty("ob", out var ob) || ob.ValueKind != JsonValueKind.Array)
            return null;

        if (!TryGetInt64(ob, RapidIdx_EpochSeconds, out var epochSeconds))
            return null;

        if (!TryGetDouble(ob, RapidIdx_WindSpeedMps, out var windSpeedMps))
            return null;

        if (!TryGetDouble(ob, RapidIdx_WindDirectionDegrees, out var windDirDeg))
            return null;

        var preferredUnits = LoadPreferredUnits(logger, settingRepository);

        var receivedUtc = snapshot.ReceivedUtc.UtcDateTime;
        var startUtc = DateTime.UtcNow;

        var speedPreferred = new Amount(windSpeedMps, SpeedUnits.MeterPerSecond)
            .ConvertedTo(preferredUnits.TryGetValue(MeasurementTypeEnum.WindSpeed, out var u) ? u : SpeedUnits.MeterPerSecond);

        var endUtc = DateTime.UtcNow;

        var (hub, serial) = ResolveIdentifiers(snapshot, root);

        return new WindReading
        {
            HubSerialNumber = hub,
            SerialNumber = serial,
            Type = "ws-rapid_wind",

            Id = IdGenerator.CreateCombGuid(),
            SourcePacketId = Guid.Empty,
            Timestamp = DateTimeOffset.FromUnixTimeSeconds(epochSeconds).UtcDateTime.ToLocalTime(),
            ReceivedUtc = receivedUtc,
            Provenance = new ReadingProvenance
            {
                RawPacketId = Guid.Empty,
                UdpReceiptTime = receivedUtc,
                TransformStartTime = startUtc,
                TransformEndTime = endUtc,
                SourceUnits = "meter/second",
                TargetUnits = preferredUnits.TryGetValue(MeasurementTypeEnum.WindSpeed, out var unit) ? unit.Name : null,
                TransformerVersion = TransformerVersion
            },

            DeviceReceivedUtcTimestampEpoch = epochSeconds,
            Speed = speedPreferred,
            DirectionDegrees = windDirDeg,
            DirectionCardinal = DegreesToCardinal(windDirDeg)
        };
    }

    static async Task<(ObservationReading? Observation, WindReading? Wind)> TryMapObsStAsync(
        TempestWebSocketObservationsSnapshot snapshot,
        JsonElement root,
        ILogger logger,
        ISettingRepository settingRepository,
        IStationMetadataProvider? stationMetadataProvider,
        CancellationToken cancellationToken
    )
    {
        if (!TryGetFirstObsArray(root, out var obs))
            return (null, null);

        if (!TryGetInt64(obs, ObsIdx_EpochSeconds, out var epochSeconds))
            return (null, null);

        var preferredUnits = LoadPreferredUnits(logger, settingRepository);

        var receivedUtc = snapshot.ReceivedUtc.UtcDateTime;
        var startUtc = DateTime.UtcNow;

        // Best-effort: if any required reading component is missing, bail out.
        if (!TryGetDouble(obs, ObsIdx_WindLullMps, out var windLullMps)) return (null, null);
        if (!TryGetDouble(obs, ObsIdx_WindAvgMps, out var windAvgMps)) return (null, null);
        if (!TryGetDouble(obs, ObsIdx_WindGustMps, out var windGustMps)) return (null, null);
        if (!TryGetDouble(obs, ObsIdx_WindDirectionDegrees, out var windDirDeg)) return (null, null);
        if (!TryGetInt32(obs, ObsIdx_WindSampleIntervalSeconds, out var windSampleIntervalSeconds)) return (null, null);

        if (!TryGetDouble(obs, ObsIdx_StationPressureMbar, out var stationPressureMbar)) return (null, null);
        if (!TryGetDouble(obs, ObsIdx_AirTempC, out var airTempC)) return (null, null);
        if (!TryGetDouble(obs, ObsIdx_RelativeHumidityPercent, out var relativeHumidity)) return (null, null);

        if (!TryGetDouble(obs, ObsIdx_IlluminanceLux, out var illuminanceLux)) return (null, null);
        if (!TryGetDouble(obs, ObsIdx_UvIndex, out var uvIndex)) return (null, null);
        if (!TryGetDouble(obs, ObsIdx_SolarRadiationWPerM2, out var solarRadiation)) return (null, null);

        if (!TryGetDouble(obs, ObsIdx_RainAccumulationMm, out var rainMm)) return (null, null);
        if (!TryGetInt32(obs, ObsIdx_PrecipType, out var precipType)) return (null, null);

        if (!TryGetDouble(obs, ObsIdx_LightningAvgDistanceKm, out var lightningAvgKm)) return (null, null);
        if (!TryGetInt32(obs, ObsIdx_LightningCount, out var lightningCount)) return (null, null);

        if (!TryGetDouble(obs, ObsIdx_BatteryVolts, out var batteryVolts)) return (null, null);
        if (!TryGetInt32(obs, ObsIdx_ReportingIntervalMinutes, out var reportingIntervalMinutes)) return (null, null);

        var windUnit = preferredUnits.TryGetValue(MeasurementTypeEnum.WindSpeed, out var ws) ? ws : SpeedUnits.MeterPerSecond;
        var tempUnit = preferredUnits.TryGetValue(MeasurementTypeEnum.AirTemperature, out var tu) ? tu : TemperatureUnits.DegreeCelsius;
        var pressureUnit = preferredUnits.TryGetValue(MeasurementTypeEnum.AirPressure, out var pu) ? pu : PressureUnits.MilliBar;
        var batteryUnit = preferredUnits.TryGetValue(MeasurementTypeEnum.BatteryLevel, out var bu) ? bu : ElectricUnits.Volt;
        var illuminanceUnit = preferredUnits.TryGetValue(MeasurementTypeEnum.Illuminance, out var iu) ? iu : LuminousIntensityUnits.Lux;
        var lightningUnit = preferredUnits.TryGetValue(MeasurementTypeEnum.LightningDistance, out var lu) ? lu : LengthUnits.KiloMeter;
        var rainUnit = preferredUnits.TryGetValue(MeasurementTypeEnum.RainAccumulation, out var ru) ? ru : LengthUnits.MilliMeter;
        var solarUnit = preferredUnits.TryGetValue(MeasurementTypeEnum.SolarRadiation, out var su) ? su : SolarRadiationUnits.WattPerSquareMeter;

        var airTemperaturePreferred = new Amount(airTempC, TemperatureUnits.DegreeCelsius).ConvertedTo(tempUnit);
        var stationPressurePreferred = new Amount(stationPressureMbar, PressureUnits.MilliBar).ConvertedTo(pressureUnit);

        var windAvgPreferred = new Amount(windAvgMps, SpeedUnits.MeterPerSecond).ConvertedTo(windUnit);
        var windLullPreferred = new Amount(windLullMps, SpeedUnits.MeterPerSecond).ConvertedTo(windUnit);
        var windGustPreferred = new Amount(windGustMps, SpeedUnits.MeterPerSecond).ConvertedTo(windUnit);

        var batteryPreferred = new Amount(batteryVolts, ElectricUnits.Volt).ConvertedTo(batteryUnit);
        var illuminancePreferred = new Amount(illuminanceLux, LuminousIntensityUnits.Lux).ConvertedTo(illuminanceUnit);
        var lightningDistPreferred = new Amount(lightningAvgKm, LengthUnits.KiloMeter).ConvertedTo(lightningUnit);
        var rainPreferred = new Amount(rainMm, LengthUnits.MilliMeter).ConvertedTo(rainUnit);
        var solarPreferred = new Amount(solarRadiation, SolarRadiationUnits.WattPerSquareMeter).ConvertedTo(solarUnit);

        var dewPoint = DerivedObservationCalculator.TryComputeDewPoint(airTemperaturePreferred, relativeHumidity);
        var windChill = DerivedObservationCalculator.TryComputeWindChill(airTemperaturePreferred, windAvgPreferred);
        var heatIndex = DerivedObservationCalculator.TryComputeHeatIndex(airTemperaturePreferred, relativeHumidity);
        var feelsLike = DerivedObservationCalculator.ComputeFeelsLike(airTemperaturePreferred, windChill, heatIndex);

        double? elevationMeters = null;
        if (stationMetadataProvider is not null)
        {
            try
            {
                elevationMeters = await stationMetadataProvider.GetStationElevationMetersAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (InvalidOperationException ex)
            {
                logger.Warning($"TempestWebSocketReadingsMapper: elevation lookup failed. {ex.Message}");
            }
        }

        var seaLevelPressure = DerivedObservationCalculator.TryComputeSeaLevelPressure(
            stationPressure: new Amount(stationPressureMbar, PressureUnits.MilliBar),
            airTemperature: new Amount(airTempC, TemperatureUnits.DegreeCelsius),
            stationElevationMeters: elevationMeters
        );

        var seaLevelPressurePreferred = seaLevelPressure?.ConvertedTo(pressureUnit);

        var endUtc = DateTime.UtcNow;

        var provenance = new ReadingProvenance
        {
            RawPacketId = Guid.Empty,
            UdpReceiptTime = receivedUtc,
            TransformStartTime = startUtc,
            TransformEndTime = endUtc,
            SourceUnits = "c, mbar, mps, lux, w/m2, mm, km, v",
            TargetUnits = BuildTargetUnits(preferredUnits),
            TransformerVersion = TransformerVersion
        };

        var (hub, serial) = ResolveIdentifiers(snapshot, root);

        var local = DateTimeOffset.FromUnixTimeSeconds(epochSeconds).UtcDateTime.ToLocalTime();

        var observation = new ObservationReading
        {
            HubSerialNumber = hub,
            SerialNumber = serial,
            Type = "ws-obs_st",

            Id = IdGenerator.CreateCombGuid(),
            SourcePacketId = Guid.Empty,
            Timestamp = local,
            ReceivedUtc = receivedUtc,
            Provenance = provenance,

            AirTemperature = airTemperaturePreferred,
            BatteryLevel = batteryPreferred,
            EpochTimeOfMeasurement = epochSeconds,
            Illuminance = illuminancePreferred,
            LightningStrikeAverageDistance = lightningDistPreferred,
            LightningStrikeCount = lightningCount,
            PrecipitationType = precipType,
            RainAccumulation = rainPreferred,
            RelativeHumidity = relativeHumidity,
            ReportingInterval = reportingIntervalMinutes,
            SolarRadiation = solarPreferred,
            StationPressure = stationPressurePreferred,
            UvIndex = uvIndex,
            WindAverage = windAvgPreferred,
            WindDirection = (int)Math.Round(windDirDeg, MidpointRounding.AwayFromZero),
            WindGust = windGustPreferred,
            WindLull = windLullPreferred,
            WindSampleInterval = windSampleIntervalSeconds,

            DewPoint = dewPoint,
            WindChill = windChill,
            HeatIndex = heatIndex,
            FeelsLike = feelsLike,
            AtmosphericPressure = seaLevelPressurePreferred
        };

        var wind = new WindReading
        {
            HubSerialNumber = hub,
            SerialNumber = serial,
            Type = "ws-wind",

            Id = IdGenerator.CreateCombGuid(),
            SourcePacketId = Guid.Empty,
            Timestamp = local,
            ReceivedUtc = receivedUtc,
            Provenance = provenance,

            DeviceReceivedUtcTimestampEpoch = epochSeconds,
            Speed = windAvgPreferred,
            DirectionDegrees = windDirDeg,
            DirectionCardinal = DegreesToCardinal(windDirDeg)
        };

        return (observation, wind);
    }

    static (string Hub, string Serial) ResolveIdentifiers(TempestWebSocketObservationsSnapshot snapshot, JsonElement root)
    {
        var hub = $"ws:{snapshot.DeviceId.ToString(CultureInfo.InvariantCulture)}";
        var serial = hub;

        if (TryGetString(root, "hub_sn", out var hubSn))
            hub = hubSn;

        if (TryGetString(root, "serial_number", out var serialNumber))
            serial = serialNumber;

        return (hub, serial);
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
            logger.Warning($"TempestWebSocketReadingsMapper: failed to parse preferred units; will emit source units. {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            logger.Warning($"TempestWebSocketReadingsMapper: failed to load preferred units; will emit source units. {ex.Message}");
        }
        catch (ArgumentException ex)
        {
            logger.Warning($"TempestWebSocketReadingsMapper: invalid preferred units; will emit source units. {ex.Message}");
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

    static bool TryGetFirstObsArray(JsonElement root, out JsonElement obs)
    {
        obs = default;

        if (!root.TryGetProperty("obs", out var obsArray) || obsArray.ValueKind != JsonValueKind.Array)
            return false;

        if (obsArray.GetArrayLength() == 0)
            return false;

        var first = obsArray[0];
        if (first.ValueKind != JsonValueKind.Array)
            return false;

        obs = first;
        return true;
    }

    static bool TryGetDouble(JsonElement array, int index, out double value)
    {
        value = default;
        if (!TryGetElement(array, index, out var el)) return false;
        if (el.ValueKind != JsonValueKind.Number) return false;
        return el.TryGetDouble(out value);
    }

    static bool TryGetInt32(JsonElement array, int index, out int value)
    {
        value = default;
        if (!TryGetElement(array, index, out var el)) return false;
        if (el.ValueKind != JsonValueKind.Number) return false;
        return el.TryGetInt32(out value);
    }

    static bool TryGetInt64(JsonElement array, int index, out long value)
    {
        value = default;
        if (!TryGetElement(array, index, out var el)) return false;
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
