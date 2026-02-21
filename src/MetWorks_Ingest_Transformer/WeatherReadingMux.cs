namespace MetWorks.Ingest.Transformer;

using System.Threading;
using System.Text.Json;
using MetWorks.Common;
using MetWorks.Constants;
using MetWorks.Interfaces;
using MetWorks.Models.Observables.Weather;

/// <summary>
/// Selects a single canonical UI stream of weather readings from either UDP-derived readings or REST-derived readings.
/// </summary>
public sealed class WeatherReadingMux : ServiceBase
{
    const int DefaultUdpStaleSeconds = 90;
    const int DefaultRestStaleMinutes = 20;
    static readonly TimeSpan StatusTickInterval = TimeSpan.FromSeconds(5);

    readonly object _gate = new();

    WeatherIngestSourceMode _sourceMode = WeatherIngestSourceMode.Auto;
    int _udpStaleSeconds = DefaultUdpStaleSeconds;
    int _restStaleMinutes = DefaultRestStaleMinutes;

    WeatherIngestSource _activeSource = WeatherIngestSource.None;

    DateTimeOffset? _udpLastReceivedUtc;
    DateTimeOffset? _restLastRetrievedUtc;

    ObservationReading? _udpObservation;
    WindReading? _udpWind;

    ObservationReading? _restObservation;
    WindReading? _restWind;

    string? _udpLastError;
    string? _restLastError;

    WeatherIngestStatus? _lastPublishedStatus;

    IStationMetadataProvider? _stationMetadataProvider;

    public WeatherReadingMux()
    {
    }

    public Task<bool> InitializeAsync(
        ILogger iLogger,
        ISettingRepository iSettingRepository,
        IEventRelayBasic iEventRelayBasic,
        CancellationToken externalCancellation = default,
        ProvenanceTracker? provenanceTracker = null,
        IStationMetadataProvider? iStationMetadataProvider = null)
    {
        ArgumentNullException.ThrowIfNull(iLogger);
        ArgumentNullException.ThrowIfNull(iSettingRepository);
        ArgumentNullException.ThrowIfNull(iEventRelayBasic);

        InitializeBase(
            iLogger.ForContext(GetType()),
            iSettingRepository,
            iEventRelayBasic,
            externalCancellation,
            provenanceTracker);

        _stationMetadataProvider = iStationMetadataProvider;

        LoadSettings();

        var settingsPrefix = LookupDictionaries.WeatherIngestGroupSettingsDefinition.BuildGroupPath();
        IEventRelayPath.Register(settingsPrefix, _ =>
        {
            LoadSettings();
            EvaluateAndPublish(triggerSource: null, udpDelta: null, restDelta: null, isTick: false);
        });

        IEventRelayBasic.Register<IObservationReading>(this, OnUdpObservationReceived);
        IEventRelayBasic.Register<IWindReading>(this, OnUdpWindReceived);
        IEventRelayBasic.Register<TempestRestObservationsSnapshot>(this, OnRestSnapshotReceived);

        StartBackground(StatusTickLoopAsync);

        MarkReady();
        ILogger.Information("WeatherReadingMux initialized");

        return Task.FromResult(true);
    }

    void LoadSettings()
    {
        // Settings are expected to exist in settings.yaml, but we still guard for corrupt values.
        var udpStaleSeconds = ISettingRepository.GetValueOrDefault<int>(
            LookupDictionaries.WeatherIngestGroupSettingsDefinition.BuildPath(SettingConstants.WeatherIngest_udpStaleSeconds));

        var restStaleMinutes = ISettingRepository.GetValueOrDefault<int>(
            LookupDictionaries.WeatherIngestGroupSettingsDefinition.BuildPath(SettingConstants.WeatherIngest_restStaleMinutes));

        var modeText = ISettingRepository.GetValueOrDefault<string>(
            LookupDictionaries.WeatherIngestGroupSettingsDefinition.BuildPath(SettingConstants.WeatherIngest_sourceMode));

        if (udpStaleSeconds <= 0) udpStaleSeconds = DefaultUdpStaleSeconds;
        if (restStaleMinutes <= 0) restStaleMinutes = DefaultRestStaleMinutes;

        if (!Enum.TryParse<WeatherIngestSourceMode>(modeText, ignoreCase: true, out var mode))
            mode = WeatherIngestSourceMode.Auto;

        lock (_gate)
        {
            _udpStaleSeconds = udpStaleSeconds;
            _restStaleMinutes = restStaleMinutes;
            _sourceMode = mode;
        }
    }

    async Task StatusTickLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await Task.Delay(StatusTickInterval, token).ConfigureAwait(false);
            EvaluateAndPublish(triggerSource: null, udpDelta: null, restDelta: null, isTick: true);
        }
    }

    void OnUdpObservationReceived(IObservationReading reading)
    {
        if (reading is null) return;

        ObservationReading? concrete = reading as ObservationReading;
        if (concrete is null)
        {
            lock (_gate) { _udpLastError = $"Unexpected observation type: {reading.GetType().Name}"; }
            EvaluateAndPublish(triggerSource: WeatherIngestSource.Udp, udpDelta: null, restDelta: null, isTick: false);
            return;
        }

        lock (_gate)
        {
            _udpObservation = concrete;
            _udpLastReceivedUtc = ToUtcOffset(concrete.ReceivedUtc);
            _udpLastError = null;
        }

        EvaluateAndPublish(triggerSource: WeatherIngestSource.Udp, udpDelta: concrete, restDelta: null, isTick: false);
    }

    void OnUdpWindReceived(IWindReading reading)
    {
        if (reading is null) return;

        WindReading? concrete = reading as WindReading;
        if (concrete is null)
        {
            lock (_gate) { _udpLastError = $"Unexpected wind type: {reading.GetType().Name}"; }
            EvaluateAndPublish(triggerSource: WeatherIngestSource.Udp, udpDelta: null, restDelta: null, isTick: false);
            return;
        }

        lock (_gate)
        {
            _udpWind = concrete;
            _udpLastReceivedUtc = ToUtcOffset(concrete.ReceivedUtc);
            _udpLastError = null;
        }

        EvaluateAndPublish(triggerSource: WeatherIngestSource.Udp, udpDelta: null, restDelta: concrete, isTick: false);
    }

    void OnRestSnapshotReceived(TempestRestObservationsSnapshot snapshot)
    {
        if (snapshot is null) return;

        StartBackground(async token =>
        {
            (ObservationReading? Observation, WindReading? Wind, int? TimeZoneOffsetMinutes) mapped;
            try
            {
                mapped = await TempestRestReadingsMapper.TryMapAsync(
                    snapshot,
                    ILogger,
                    ISettingRepository,
                    _stationMetadataProvider,
                    token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                return;
            }
            catch (JsonException ex)
            {
                lock (_gate)
                {
                    _restLastRetrievedUtc = snapshot.RetrievedUtc;
                    _restLastError = ex.Message;
                }

                EvaluateAndPublish(triggerSource: WeatherIngestSource.Rest, udpDelta: null, restDelta: null, isTick: false);
                return;
            }
            catch (InvalidOperationException ex)
            {
                lock (_gate)
                {
                    _restLastRetrievedUtc = snapshot.RetrievedUtc;
                    _restLastError = ex.Message;
                }

                EvaluateAndPublish(triggerSource: WeatherIngestSource.Rest, udpDelta: null, restDelta: null, isTick: false);
                return;
            }

            lock (_gate)
            {
                _restLastRetrievedUtc = snapshot.RetrievedUtc;
                _restObservation = mapped.Observation;
                _restWind = mapped.Wind;
                _restLastError = null;
            }

            EvaluateAndPublish(
                triggerSource: WeatherIngestSource.Rest,
                udpDelta: mapped.Observation,
                restDelta: mapped.Wind,
                isTick: false);
        });
    }

    // udpDelta: optional observation, restDelta: optional wind (naming kept to avoid extra tuples)
    void EvaluateAndPublish(WeatherIngestSource? triggerSource, ObservationReading? udpDelta, WindReading? restDelta, bool isTick)
    {
        WeatherIngestSourceMode mode;
        int udpStaleSeconds;
        int restStaleMinutes;

        WeatherIngestSource activeSource;

        DateTimeOffset? udpLast;
        DateTimeOffset? restLast;

        ObservationReading? udpObs;
        WindReading? udpWind;

        ObservationReading? restObs;
        WindReading? restWind;

        string? udpError;
        string? restError;

        lock (_gate)
        {
            mode = _sourceMode;
            udpStaleSeconds = _udpStaleSeconds;
            restStaleMinutes = _restStaleMinutes;

            activeSource = _activeSource;

            udpLast = _udpLastReceivedUtc;
            restLast = _restLastRetrievedUtc;

            udpObs = _udpObservation;
            udpWind = _udpWind;

            restObs = _restObservation;
            restWind = _restWind;

            udpError = _udpLastError;
            restError = _restLastError;
        }

        var nowUtc = DateTimeOffset.UtcNow;

        var udpAvailable = udpLast is not null;
        var udpIsFresh = udpAvailable && (nowUtc - udpLast.Value) <= TimeSpan.FromSeconds(udpStaleSeconds);

        var restAvailable = restLast is not null;
        var restIsFresh = restAvailable && (nowUtc - restLast.Value) <= TimeSpan.FromMinutes(restStaleMinutes);

        WeatherIngestSource desired = mode switch
        {
            WeatherIngestSourceMode.UdpOnly => udpIsFresh ? WeatherIngestSource.Udp : WeatherIngestSource.None,
            WeatherIngestSourceMode.RestOnly => restIsFresh ? WeatherIngestSource.Rest : WeatherIngestSource.None,
            _ => udpIsFresh
                ? WeatherIngestSource.Udp
                : restIsFresh
                    ? WeatherIngestSource.Rest
                    : WeatherIngestSource.None
        };

        var sourceChanged = desired != activeSource;
        if (sourceChanged)
        {
            lock (_gate) { _activeSource = desired; }
            activeSource = desired;
        }

        // Publish canonical readings
        if (sourceChanged)
        {
            PublishCachedFor(activeSource, udpObs, udpWind, restObs, restWind);
        }
        else if (triggerSource is not null && triggerSource.Value == activeSource)
        {
            if (udpDelta is not null)
                IEventRelayBasic.Send(udpDelta);

            if (restDelta is not null)
                IEventRelayBasic.Send(restDelta);
        }

        var status = new WeatherIngestStatus(
            SourceMode: mode,
            ActiveSource: activeSource,
            UdpAvailable: udpAvailable,
            UdpIsFresh: udpIsFresh,
            UdpLastReceivedUtc: udpLast,
            RestAvailable: restAvailable,
            RestIsFresh: restIsFresh,
            RestLastRetrievedUtc: restLast,
            UdpLastError: udpError,
            RestLastError: restError);

        var shouldPublishStatus = sourceChanged || isTick || _lastPublishedStatus != status;
        if (shouldPublishStatus)
        {
            _lastPublishedStatus = status;
            IEventRelayBasic.Send(status);
        }
    }

    void PublishCachedFor(
        WeatherIngestSource active,
        ObservationReading? udpObs,
        WindReading? udpWind,
        ObservationReading? restObs,
        WindReading? restWind)
    {
        if (active == WeatherIngestSource.Udp)
        {
            if (udpObs is not null) IEventRelayBasic.Send(udpObs);
            if (udpWind is not null) IEventRelayBasic.Send(udpWind);
        }
        else if (active == WeatherIngestSource.Rest)
        {
            if (restObs is not null) IEventRelayBasic.Send(restObs);
            if (restWind is not null) IEventRelayBasic.Send(restWind);
        }
    }

    static DateTimeOffset ToUtcOffset(DateTime utc)
    {
        if (utc.Kind == DateTimeKind.Utc)
            return new DateTimeOffset(utc, TimeSpan.Zero);

        return new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc), TimeSpan.Zero);
    }

    protected override Task OnDisposeAsync()
    {
        try { IEventRelayBasic.Unregister<IObservationReading>(this); } catch { }
        try { IEventRelayBasic.Unregister<IWindReading>(this); } catch { }
        try { IEventRelayBasic.Unregister<TempestRestObservationsSnapshot>(this); } catch { }
        return Task.CompletedTask;
    }
}
