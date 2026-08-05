using System.Collections.Concurrent;
using System.Text.Json;
using MetWorks.Constants;
using MetWorks.Common;
using MetWorks.EnumDefinitions;
using MetWorks.EventRelay;
using MetWorks.Ingest.Transformer.Tests.Fakes;
using MetWorks.Interfaces;
using MetWorks.Models.Observables.Weather;
using MetWorks.RedStar.Amounts.WeatherExtensions;
using RedStar.Amounts;
using RedStar.Amounts.StandardUnits;
using Xunit;

namespace MetWorks.Ingest.Transformer.Tests;

public sealed class WeatherReadingMuxTests
{
    static readonly SemaphoreSlim _unitInitGate = new(1, 1);
    static bool _unitsInitialized;

    static async Task EnsureUnitsInitializedAsync()
    {
        if (_unitsInitialized)
            return;

        await _unitInitGate.WaitAsync();
        try
        {
            if (_unitsInitialized)
                return;

            var ok = await new UnitsOfMeasureInitializer().InitializeAsync(new TestLogger("units"));
            if (!ok)
                throw new InvalidOperationException("UnitsOfMeasureInitializer failed (required for Unit.Parse in REST mapping).");

            _unitsInitialized = true;
        }
        finally
        {
            _unitInitGate.Release();
        }
    }

    static Dictionary<string, string> CreateDefaultSettings()
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            // Weather ingest mux behavior
            [LookupDictionaries.WeatherIngestGroupSettingsDefinition.BuildPath(SettingConstants.WeatherIngest_udpStaleSeconds)] = "90",
            [LookupDictionaries.WeatherIngestGroupSettingsDefinition.BuildPath(SettingConstants.WeatherIngest_restStaleMinutes)] = "20",
            [LookupDictionaries.WeatherIngestGroupSettingsDefinition.BuildPath(SettingConstants.WeatherIngest_sourceMode)] = WeatherIngestSourceMode.Auto.ToString(),

            // Preferred units needed by REST mapper (TempestRestReadingsMapper.LoadPreferredUnits)
            [LookupDictionaries.UnitOfMeasureGroupSettingsDefinition.BuildPath(SettingConstants.UnitOfMeasure_airPressure)] = "millibar",
            [LookupDictionaries.UnitOfMeasureGroupSettingsDefinition.BuildPath(SettingConstants.UnitOfMeasure_airTemperature)] = "degree celsius",
            [LookupDictionaries.UnitOfMeasureGroupSettingsDefinition.BuildPath(SettingConstants.UnitOfMeasure_batteryLevel)] = "volt",
            [LookupDictionaries.UnitOfMeasureGroupSettingsDefinition.BuildPath(SettingConstants.UnitOfMeasure_illuminance)] = "lux",
            [LookupDictionaries.UnitOfMeasureGroupSettingsDefinition.BuildPath(SettingConstants.UnitOfMeasure_lightningDistance)] = "kilometer",
            [LookupDictionaries.UnitOfMeasureGroupSettingsDefinition.BuildPath(SettingConstants.UnitOfMeasure_rainAccumulation)] = "millimeter",
            [LookupDictionaries.UnitOfMeasureGroupSettingsDefinition.BuildPath(SettingConstants.UnitOfMeasure_solarRadiation)] = "watt per square meter",
            [LookupDictionaries.UnitOfMeasureGroupSettingsDefinition.BuildPath(SettingConstants.UnitOfMeasure_windSpeed)] = "meter/second",
        };

        return values;
    }

    [Fact]
    public async Task WhenUdpReadingsArriveThenMuxPublishesConcreteReadingsAndUdpStatus()
    {
        await EnsureUnitsInitializedAsync();

        var relay = new EventRelayBasic();
        var settingRepository = new InMemorySettingRepository(CreateDefaultSettings());
        var logger = new TestLogger("mux");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var mux = new WeatherReadingMux();
        await mux.InitializeAsync(logger, settingRepository, relay, cts.Token);

        var observations = new ConcurrentQueue<ObservationReading>();
        var winds = new ConcurrentQueue<WindReading>();
        var statuses = new ConcurrentQueue<WeatherIngestStatus>();

        object recipient = new();
        relay.Register<ObservationReading>(recipient, observations.Enqueue);
        relay.Register<WindReading>(recipient, winds.Enqueue);
        relay.Register<WeatherIngestStatus>(recipient, statuses.Enqueue);

        var nowUtc = DateTime.UtcNow;

        var udpObs = new ObservationReading
        {
            Id = Guid.NewGuid(),
            SourcePacketId = Guid.NewGuid(),
            Type = "udp-observation",
            HubSerialNumber = "hub",
            SerialNumber = "device",
            Timestamp = nowUtc,
            ReceivedUtc = nowUtc,
            Provenance = new ReadingProvenance { RawPacketId = Guid.NewGuid(), UdpReceiptTime = nowUtc, TransformStartTime = nowUtc, TransformEndTime = nowUtc },
            AirTemperature = new Amount(10, TemperatureUnits.DegreeCelsius),
            StationPressure = new Amount(1000, PressureUnits.MilliBar),
            RelativeHumidity = 50,
            Illuminance = new Amount(1, LuminousIntensityUnits.Lux),
            UvIndex = 0,
            SolarRadiation = new Amount(0, SolarRadiationUnits.WattPerSquareMeter),
            RainAccumulation = new Amount(0, LengthUnits.MilliMeter),
            PrecipitationType = 0,
            LightningStrikeAverageDistance = new Amount(0, LengthUnits.KiloMeter),
            LightningStrikeCount = 0,
            BatteryLevel = new Amount(3, ElectricUnits.Volt),
            EpochTimeOfMeasurement = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            WindAverage = new Amount(0, SpeedUnits.MeterPerSecond),
            WindGust = new Amount(0, SpeedUnits.MeterPerSecond),
            WindLull = new Amount(0, SpeedUnits.MeterPerSecond),
            WindDirection = 0,
            WindSampleInterval = 60,
            ReportingInterval = 1,
            AtmosphericPressure = new Amount(1000, PressureUnits.MilliBar),
            DewPoint = new Amount(0, TemperatureUnits.DegreeCelsius),
            WindChill = new Amount(0, TemperatureUnits.DegreeCelsius),
            HeatIndex = new Amount(0, TemperatureUnits.DegreeCelsius),
            FeelsLike = new Amount(0, TemperatureUnits.DegreeCelsius),
        };

        IObservationReading udpObsMessage = udpObs;
        relay.Send(udpObsMessage);

        var udpWind = new WindReading
        {
            Id = Guid.NewGuid(),
            SourcePacketId = Guid.NewGuid(),
            Type = "udp-wind",
            HubSerialNumber = "hub",
            SerialNumber = "device",
            Timestamp = nowUtc,
            ReceivedUtc = nowUtc,
            Provenance = new ReadingProvenance { RawPacketId = Guid.NewGuid(), UdpReceiptTime = nowUtc, TransformStartTime = nowUtc, TransformEndTime = nowUtc },
            DeviceReceivedUtcTimestampEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Speed = new Amount(1, SpeedUnits.MeterPerSecond),
            DirectionDegrees = 90,
            DirectionCardinal = "E"
        };

        IWindReading udpWindMessage = udpWind;
        relay.Send(udpWindMessage);

        // Wait for at least one status update.
        await WaitForAsync(() => !statuses.IsEmpty, cts.Token);

        Assert.Contains(statuses, s => s.ActiveSource == WeatherIngestSource.Udp);
        Assert.True(observations.Count >= 1);
        Assert.True(winds.Count >= 1);

        await mux.DisposeAsync();
        relay.Unregister<ObservationReading>(recipient);
        relay.Unregister<WindReading>(recipient);
        relay.Unregister<WeatherIngestStatus>(recipient);
    }

    [Fact]
    public async Task WhenUdpIsStaleAndWebSocketIsFreshThenMuxSelectsWebSocketAndPublishesConcreteReadings()
    {
        await EnsureUnitsInitializedAsync();

        var settings = CreateDefaultSettings();
        settings[LookupDictionaries.WeatherIngestGroupSettingsDefinition.BuildPath(SettingConstants.WeatherIngest_udpStaleSeconds)] = "1";
        settings[LookupDictionaries.WeatherIngestGroupSettingsDefinition.BuildPath(SettingConstants.WeatherIngest_sourceMode)] = WeatherIngestSourceMode.Auto.ToString();
        settings[LookupDictionaries.TempestGroupSettingsDefinition.BuildPath(SettingConstants.Tempest_websocket_enabled)] = "true";

        var relay = new EventRelayBasic();
        var settingRepository = new InMemorySettingRepository(settings);
        var logger = new TestLogger("mux");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var mux = new WeatherReadingMux();
        await mux.InitializeAsync(logger, settingRepository, relay, cts.Token);

        var observations = new ConcurrentQueue<ObservationReading>();
        var winds = new ConcurrentQueue<WindReading>();
        var statuses = new ConcurrentQueue<WeatherIngestStatus>();

        object recipient = new();
        relay.Register<ObservationReading>(recipient, observations.Enqueue);
        relay.Register<WindReading>(recipient, winds.Enqueue);
        relay.Register<WeatherIngestStatus>(recipient, statuses.Enqueue);

        // Send a UDP reading with a stale ReceivedUtc.
        var staleUtc = DateTime.UtcNow.AddMinutes(-10);
        relay.Send<IObservationReading>(new ObservationReading
        {
            Id = Guid.NewGuid(),
            SourcePacketId = Guid.NewGuid(),
            Type = "udp-observation",
            HubSerialNumber = "hub",
            SerialNumber = "device",
            Timestamp = staleUtc,
            ReceivedUtc = staleUtc,
            Provenance = new ReadingProvenance { RawPacketId = Guid.NewGuid(), UdpReceiptTime = staleUtc, TransformStartTime = staleUtc, TransformEndTime = staleUtc },
            AirTemperature = new Amount(10, TemperatureUnits.DegreeCelsius),
            StationPressure = new Amount(1000, PressureUnits.MilliBar),
            RelativeHumidity = 50,
            Illuminance = new Amount(1, LuminousIntensityUnits.Lux),
            UvIndex = 0,
            SolarRadiation = new Amount(0, SolarRadiationUnits.WattPerSquareMeter),
            RainAccumulation = new Amount(0, LengthUnits.MilliMeter),
            PrecipitationType = 0,
            LightningStrikeAverageDistance = new Amount(0, LengthUnits.KiloMeter),
            LightningStrikeCount = 0,
            BatteryLevel = new Amount(3, ElectricUnits.Volt),
            EpochTimeOfMeasurement = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            WindAverage = new Amount(0, SpeedUnits.MeterPerSecond),
            WindGust = new Amount(0, SpeedUnits.MeterPerSecond),
            WindLull = new Amount(0, SpeedUnits.MeterPerSecond),
            WindDirection = 0,
            WindSampleInterval = 60,
            ReportingInterval = 1,
        });

        var wsJson = """
        {
          "type": "obs_st",
          "device_id": 999,
          "hub_sn": "hub",
          "serial_number": "device",
          "obs": [
            [1771642103, 0.0, 1.0, 2.0, 90, 3, 1000.0, 10.0, 50.0, 100.0, 0.0, 0.0, 0.0, 0, 0.0, 0, 3.0, 1]
          ]
        }
        """;

        relay.Send(new TempestWebSocketObservationsSnapshot(
            DeviceId: 999,
            ReceivedUtc: DateTimeOffset.UtcNow,
            MessageType: "obs_st",
            RawJson: wsJson));

        await WaitForAsync(() => statuses.Any(s => s.ActiveSource == WeatherIngestSource.WebSocket), cts.Token);

        Assert.Contains(statuses, s => s.ActiveSource == WeatherIngestSource.WebSocket);
        Assert.True(observations.Count >= 1);
        Assert.True(winds.Count >= 1);

        await mux.DisposeAsync();
        relay.Unregister<ObservationReading>(recipient);
        relay.Unregister<WindReading>(recipient);
        relay.Unregister<WeatherIngestStatus>(recipient);
    }

    [Fact]
    public async Task WhenRestSnapshotArrivesThenMuxPublishesRestObservationInPreferredUnits()
    {
        await EnsureUnitsInitializedAsync();

        var settings = CreateDefaultSettings();
        settings[LookupDictionaries.WeatherIngestGroupSettingsDefinition.BuildPath(SettingConstants.WeatherIngest_sourceMode)] = WeatherIngestSourceMode.RestOnly.ToString();
        settings[LookupDictionaries.UnitOfMeasureGroupSettingsDefinition.BuildPath(SettingConstants.UnitOfMeasure_airTemperature)] = "degree fahrenheit";

        var relay = new EventRelayBasic();
        var settingRepository = new InMemorySettingRepository(settings);
        var logger = new TestLogger("mux");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var mux = new WeatherReadingMux();
        await mux.InitializeAsync(logger, settingRepository, relay, cts.Token);

        var observations = new ConcurrentQueue<ObservationReading>();
        object recipient = new();
        relay.Register<ObservationReading>(recipient, observations.Enqueue);

        // Mimic a Tempest REST payload where `station_units` indicates imperial, but values are metric.
        var json = """
        {
          "timezone_offset_minutes": -360,
          "station_units": { "units_temp": "f", "units_pressure": "inhg", "units_wind": "mph", "units_distance": "mi", "units_precip": "in" },
          "obs": [
            {
              "timestamp": 1771642103,
              "air_temperature": 10.4,
              "station_pressure": 999.9,
              "relative_humidity": 76,
              "wind_avg": 0.4,
              "wind_direction": 24,
              "wind_gust": 0.9,
              "wind_lull": 0.0,
              "solar_radiation": 0,
              "brightness": 0,
              "precip": 0,
              "lightning_strike_last_distance": 25,
              "lightning_strike_count": 0,
              "uv": 0,
              "sea_level_pressure": 1012.2
            }
          ]
        }
        """;

        var restSnapshot = new TempestRestObservationsSnapshot(
            StationId: 123,
            RetrievedUtc: DateTimeOffset.UtcNow,
            RawJson: json);

        relay.Send(restSnapshot);

        await WaitForAsync(() => !observations.IsEmpty, cts.Token);

        var obs = observations.Last();
        Assert.NotNull(obs.AirTemperature);
        Assert.Equal(TemperatureUnits.DegreeFahrenheit.Symbol, obs.AirTemperature.Unit.Symbol);
        Assert.InRange(obs.AirTemperature.Value, 50.5, 51.0);

        await mux.DisposeAsync();
        relay.Unregister<ObservationReading>(recipient);
    }

    [Fact]
    public async Task WhenUdpIsStaleThenRestSnapshotSelectsRestAndPublishesConcreteReadings()
    {
        await EnsureUnitsInitializedAsync();

        var settings = CreateDefaultSettings();
        settings[LookupDictionaries.WeatherIngestGroupSettingsDefinition.BuildPath(SettingConstants.WeatherIngest_udpStaleSeconds)] = "1";
        settings[LookupDictionaries.WeatherIngestGroupSettingsDefinition.BuildPath(SettingConstants.WeatherIngest_restStaleMinutes)] = "20";
        settings[LookupDictionaries.WeatherIngestGroupSettingsDefinition.BuildPath(SettingConstants.WeatherIngest_sourceMode)] = WeatherIngestSourceMode.Auto.ToString();

        var relay = new EventRelayBasic();
        var settingRepository = new InMemorySettingRepository(settings);
        var logger = new TestLogger("mux");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var mux = new WeatherReadingMux();
        await mux.InitializeAsync(logger, settingRepository, relay, cts.Token);

        var observations = new ConcurrentQueue<ObservationReading>();
        var winds = new ConcurrentQueue<WindReading>();
        var statuses = new ConcurrentQueue<WeatherIngestStatus>();

        object recipient = new();
        relay.Register<ObservationReading>(recipient, observations.Enqueue);
        relay.Register<WindReading>(recipient, winds.Enqueue);
        relay.Register<WeatherIngestStatus>(recipient, statuses.Enqueue);

        // Send a UDP reading with a stale ReceivedUtc.
        var staleUtc = DateTime.UtcNow.AddMinutes(-10);
        var udpObs = new ObservationReading
        {
            Id = Guid.NewGuid(),
            SourcePacketId = Guid.NewGuid(),
            Type = "udp-observation",
            HubSerialNumber = "hub",
            SerialNumber = "device",
            Timestamp = staleUtc,
            ReceivedUtc = staleUtc,
            Provenance = new ReadingProvenance { RawPacketId = Guid.NewGuid(), UdpReceiptTime = staleUtc, TransformStartTime = staleUtc, TransformEndTime = staleUtc },
            AirTemperature = new Amount(10, TemperatureUnits.DegreeCelsius),
            StationPressure = new Amount(1000, PressureUnits.MilliBar),
            RelativeHumidity = 50,
            Illuminance = new Amount(1, LuminousIntensityUnits.Lux),
            UvIndex = 0,
            SolarRadiation = new Amount(0, SolarRadiationUnits.WattPerSquareMeter),
            RainAccumulation = new Amount(0, LengthUnits.MilliMeter),
            PrecipitationType = 0,
            LightningStrikeAverageDistance = new Amount(0, LengthUnits.KiloMeter),
            LightningStrikeCount = 0,
            BatteryLevel = new Amount(3, ElectricUnits.Volt),
            EpochTimeOfMeasurement = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            WindAverage = new Amount(0, SpeedUnits.MeterPerSecond),
            WindGust = new Amount(0, SpeedUnits.MeterPerSecond),
            WindLull = new Amount(0, SpeedUnits.MeterPerSecond),
            WindDirection = 0,
            WindSampleInterval = 60,
            ReportingInterval = 1,
            AtmosphericPressure = new Amount(1000, PressureUnits.MilliBar),
            DewPoint = new Amount(0, TemperatureUnits.DegreeCelsius),
            WindChill = new Amount(0, TemperatureUnits.DegreeCelsius),
            HeatIndex = new Amount(0, TemperatureUnits.DegreeCelsius),
            FeelsLike = new Amount(0, TemperatureUnits.DegreeCelsius),
        };

        IObservationReading udpObsMessage = udpObs;
        relay.Send(udpObsMessage);

        // Send a REST snapshot using the sample payload.
        var json = await File.ReadAllTextAsync(Path.Combine("Fixtures", "GetStationObservationResultSample.json"), cts.Token);
        var restSnapshot = new TempestRestObservationsSnapshot(
            StationId: 123,
            RetrievedUtc: DateTimeOffset.UtcNow,
            RawJson: json);

        relay.Send(restSnapshot);

        // Wait until mux publishes a REST status.
        await WaitForAsync(() => statuses.Any(s => s.ActiveSource == WeatherIngestSource.Rest), cts.Token);

        Assert.Contains(statuses, s => s.ActiveSource == WeatherIngestSource.Rest);
        Assert.True(observations.Count >= 1);
        Assert.True(winds.Count >= 1);

        await mux.DisposeAsync();
        relay.Unregister<ObservationReading>(recipient);
        relay.Unregister<WindReading>(recipient);
        relay.Unregister<WeatherIngestStatus>(recipient);
    }

    [Fact]
    public async Task WhenUnitPreferenceChangesThenRestReadingsAreRepublishedInNewUnits()
    {
        await EnsureUnitsInitializedAsync();

        var settings = CreateDefaultSettings();
        settings[LookupDictionaries.WeatherIngestGroupSettingsDefinition.BuildPath(SettingConstants.WeatherIngest_sourceMode)] = WeatherIngestSourceMode.RestOnly.ToString();

        var relay = new EventRelayBasic();
        var settingRepository = new InMemorySettingRepository(settings);
        var logger = new TestLogger("mux");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var mux = new WeatherReadingMux();
        await mux.InitializeAsync(logger, settingRepository, relay, cts.Token);

        var observations = new ConcurrentQueue<ObservationReading>();
        object recipient = new();
        relay.Register<ObservationReading>(recipient, observations.Enqueue);

        var json = await File.ReadAllTextAsync(Path.Combine("Fixtures", "GetStationObservationResultSample.json"), cts.Token);
        var restSnapshot = new TempestRestObservationsSnapshot(
            StationId: 123,
            RetrievedUtc: DateTimeOffset.UtcNow,
            RawJson: json);

        relay.Send(restSnapshot);
        await WaitForAsync(() => observations.Count >= 1, cts.Token);

        var first = observations.Last();

        settingRepository.SetValue(
            LookupDictionaries.UnitOfMeasureGroupSettingsDefinition.BuildPath(SettingConstants.UnitOfMeasure_airTemperature),
            "degree fahrenheit");

        await WaitForAsync(() => observations.Count >= 2, cts.Token);

        var targetUnit = Unit.Parse("degree fahrenheit");
        var latest = observations.Last();

        var expected = first.AirTemperature.ConvertedTo(targetUnit);

        Assert.True(
            first.AirTemperature.Unit != targetUnit &&
            latest.AirTemperature.Unit == targetUnit &&
            Math.Abs(latest.AirTemperature.Value - expected.Value) < 0.25);

        await mux.DisposeAsync();
        relay.Unregister<ObservationReading>(recipient);
    }

    static async Task WaitForAsync(Func<bool> condition, CancellationToken cancellationToken)
    {
        while (!condition())
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(50, cancellationToken);
        }
    }
}
