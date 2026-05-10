namespace MetWorks.Apps.MAUI.WeatherStationMaui.ViewModels;

using System.Net.Http;

using MetWorks.Models.Observables.Weather;
using MetWorks.Constants;
/// <summary>
/// ViewModel for displaying current weather readings.
/// Subscribes to weather reading streams via ISingletonEventRelay.
/// Works with both MockWeatherReadingService and real WeatherDataTransformer.
/// </summary>
public class WeatherViewModel : INotifyPropertyChanged, IDisposable
{
    enum InitializeStateEnum
    {
        Uninitialized = 0,
        Initializing = 1,
        Initialized = 2
    }
    readonly MetWorks.Interfaces.ILogger _iLogger;
    readonly ISettingRepository _iSettingRepository;
    readonly IEventRelayBasic _iEventRelayBasic;
    readonly ITempestOAuthTokenProvider _iTempestOAuthTokenProvider;
    readonly ITempestRestObservationsProvider _iTempestRestObservationsProvider;
    private readonly IInstanceIdentifier _iInstanceIdentifier;
    IWindReading? _currentWind;
    IObservationReading? _currentObservation;
    WeatherIngestStatus? _weatherIngestStatus;
    TaskCompletionSource<WeatherIngestStatus>? _firstStatusTcs;
    SystemTimer? _clockTimer;
    ThreadingTimer? _statusCheckTimer;
    string? _lastServiceStatusLine;
    DateTime _currentTime = DateTime.Now;

    bool _tempestOAuthAuthorizationRequired;
    string? _tempestOAuthAuthorizationReason;
    string? _tempestOAuthLastError;

    public ICommand AuthorizeTempestCommand { get; }

    public WeatherIngestSource ActiveIngestSource => _weatherIngestStatus?.ActiveSource ?? WeatherIngestSource.None;
    public string ActiveIngestSourceDisplay => ActiveIngestSource.ToString();
    public bool RestIsFresh => _weatherIngestStatus?.RestIsFresh ?? false;
    public bool UdpIsFresh => _weatherIngestStatus?.UdpIsFresh ?? false;
    public DateTimeOffset? RestLastRetrievedUtc => _weatherIngestStatus?.RestLastRetrievedUtc;
    public DateTimeOffset? UdpLastReceivedUtc => _weatherIngestStatus?.UdpLastReceivedUtc;

    public bool TempestOAuthAuthorizationRequired => _tempestOAuthAuthorizationRequired;
    public string TempestOAuthAuthorizationReasonDisplay => _tempestOAuthAuthorizationReason ?? string.Empty;
    public string TempestOAuthLastErrorDisplay => _tempestOAuthLastError ?? string.Empty;

    // Lightweight init guard: 0 = not started, 1 = initializing, 2 = initialized
    int _initializeState = (int)InitializeStateEnum.Uninitialized;

    CancellationToken _iExternalCancellationToken;
    // Cancellation pattern for cooperative shutdown (optional)
    CancellationTokenSource? _localCancellation;
    CancellationTokenSource? _linkedCancellation;
    CancellationToken LinkedCancellationToken => _linkedCancellation?.Token ?? CancellationToken.None;
    // ========================================
    // Observation Reading Display Properties
    // ========================================
    // ToDo: Move the conversion to text into the transformer layer or other but should NOT be up to UI
    public string PrecipitationTypeDisplay =>
        CurrentObservation is not null
            ? CurrentObservation.PrecipitationType switch
            {
                0 => "None",
                1 => "Rain",
                2 => "Hail",
                3 => "Rain + Hail",
                _ => "Unknown"
            }
            : "--";
    public string RelativeHumidityUnit => "%";
    // ========================================
    // Time Display Properties
    // ========================================
    public string TimeDayOfWeekDisplay => _currentTime.ToString("ddd");
    public string TimeDateDisplay => _currentTime.ToString("MMM d");
    public string TimeDisplay => _currentTime.ToString("HH:mm");
    public WeatherViewModel(
        MetWorks.Interfaces.ILogger iLogger,
        ISettingRepository iSettingRepository,
        IEventRelayBasic iEventRelayBasic,
        ITempestOAuthTokenProvider iTempestOAuthTokenProvider,
        ITempestRestObservationsProvider iTempestRestObservationsProvider,
        IInstanceIdentifier iInstanceIdentifier,
        CancellationToken externalCancellation
    )
    {
        ArgumentNullException.ThrowIfNull(iLogger);
        ArgumentNullException.ThrowIfNull(iSettingRepository);
        ArgumentNullException.ThrowIfNull(iEventRelayBasic);
        ArgumentNullException.ThrowIfNull(iTempestOAuthTokenProvider);
        ArgumentNullException.ThrowIfNull(iTempestRestObservationsProvider);
        ArgumentNullException.ThrowIfNull(iInstanceIdentifier);

        _iExternalCancellationToken = externalCancellation;

        _iLogger = iLogger;
        _iSettingRepository = iSettingRepository;
        _iEventRelayBasic = iEventRelayBasic;
        _iTempestOAuthTokenProvider = iTempestOAuthTokenProvider;
        _iTempestRestObservationsProvider = iTempestRestObservationsProvider;
        _iInstanceIdentifier = iInstanceIdentifier;

        AuthorizeTempestCommand = new Command(async () => await AuthorizeTempestAsync());

        StartServiceStatusMonitoring();
        // Initialization is event-driven: subscribe to event relay and initialize when data arrives.
    }
    private void StartServiceStatusMonitoring()
    {
        // Check service status every 5 seconds
        _statusCheckTimer = new ThreadingTimer(
            UpdateServiceStatus,
            null,
            TimeSpan.Zero,  // Start immediately
            TimeSpan.FromSeconds(5)
        );
    }
    private void UpdateServiceStatus(object? state)
    {
        MainThread.BeginInvokeOnMainThread(
            () =>
            {
                try
                {
                    var isInitialized = StartupInitializer.IsInitialized;
                    var isDatabaseAvailable = StartupInitializer.IsDatabaseAvailable;

                    var serviceStatus = isInitialized ? "✅ Running" : "⚠️ Initializing";
                    var dbStatus = isDatabaseAvailable ? "💚 Connected" : "🔶 Degraded";

                    var line = $"Service Status: {serviceStatus} | Database: {dbStatus}";
                    if (!string.Equals(_lastServiceStatusLine, line, StringComparison.Ordinal))
                    {
                        _lastServiceStatusLine = line;
                        Debug.WriteLine(line);
                    }

                    // Only attempt to initialize once; InitializeAsync uses an atomic guard
                    if (isInitialized) Task.Run(() => InitializeAsync());
                }
                catch (Exception exception)
                {
                    Debug.WriteLine($"Error checking service status: {exception}");
                }
            }
        );
    }
    async Task<bool> InitializeAsync()
    {
//        await _weatherReadingMux.Ready;
        // Quick check: if already marked initialized return true
        if (
            Interlocked.CompareExchange(
                ref _initializeState,
                (int)InitializeStateEnum.Initialized,
                (int)InitializeStateEnum.Initialized
            ) == (int)InitializeStateEnum.Initialized
        )
            return await Task.FromResult(true);

        // Try to transition from 0 -> 1 (not started -> initializing)
        var prior = Interlocked.CompareExchange(
            ref _initializeState, 
            (int)InitializeStateEnum.Initializing, 
            (int)InitializeStateEnum.Uninitialized
        );
        if (prior == (int)InitializeStateEnum.Initializing)
        {
            // someone else is initializing
            return await Task.FromResult(false);
        }
        if (prior == (int)InitializeStateEnum.Initialized)
        {
            // already initialized
            return await Task.FromResult(true);
        }

        try
        {
            // Acquire dependencies using registry (existing pattern)
            // Create local and linked cancellation sources so we can honor external cancellation if provided.
            _localCancellation = new CancellationTokenSource();
            _linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(_iExternalCancellationToken, _localCancellation.Token);

            Interlocked.Exchange(ref _initializeState, (int)InitializeStateEnum.Initialized);

            // Stop status checks once we begin real initialization
            if (_statusCheckTimer is not null)
            {
                try { _statusCheckTimer.Dispose(); } catch { /* swallow */ }
                _statusCheckTimer = null;
            }

            _firstStatusTcs ??= new TaskCompletionSource<WeatherIngestStatus>(TaskCreationOptions.RunContinuationsAsynchronously);

            // Register for events (consume mux-published canonical readings)
            _iEventRelayBasic.Register<WindReading>(this, OnWindReceived);
            _iEventRelayBasic.Register<ObservationReading>(this, OnObservationReceived);
            _iEventRelayBasic.Register<WeatherIngestStatus>(this, OnWeatherIngestStatusReceived);
            _iEventRelayBasic.Register<TempestOAuthInteractiveAuthRequest>(this, OnTempestOAuthInteractiveAuthRequestReceived);

            await TryWarmStartReadingsAsync().ConfigureAwait(false);

            InitializeClockTimer();

            return await Task.FromResult(true);
        }
        catch (Exception exception)
        {
            // Reset init state so caller can retry later
            Interlocked.Exchange(
                ref _initializeState, 
                (int)InitializeStateEnum.Uninitialized
            );

            _iLogger.Error("Failed to initialize", exception);
            throw;
        }
    }

    void OnTempestOAuthInteractiveAuthRequestReceived(TempestOAuthInteractiveAuthRequest request)
    {
        if (request is null) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            _tempestOAuthAuthorizationRequired = true;
            _tempestOAuthAuthorizationReason = request.Reason;
            _tempestOAuthLastError = null;

            OnPropertyChanged(nameof(TempestOAuthAuthorizationRequired));
            OnPropertyChanged(nameof(TempestOAuthAuthorizationReasonDisplay));
            OnPropertyChanged(nameof(TempestOAuthLastErrorDisplay));
        });
    }

    async Task AuthorizeTempestAsync()
    {
        _tempestOAuthLastError = null;
        OnPropertyChanged(nameof(TempestOAuthLastErrorDisplay));

        try
        {
            var token = await _iTempestOAuthTokenProvider.GetAccessTokenAsync(
                allowInteractive: true,
                cancellationToken: LinkedCancellationToken
            );

            if (string.IsNullOrWhiteSpace(token))
                return;

            _tempestOAuthAuthorizationRequired = false;
            _tempestOAuthAuthorizationReason = null;
            OnPropertyChanged(nameof(TempestOAuthAuthorizationRequired));
            OnPropertyChanged(nameof(TempestOAuthAuthorizationReasonDisplay));
        }
        catch (OperationCanceledException) when (LinkedCancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (InvalidOperationException ex)
        {
            _tempestOAuthLastError = ex.Message;
            OnPropertyChanged(nameof(TempestOAuthLastErrorDisplay));
            _iLogger.Warning($"Tempest OAuth interactive auth failed. {ex.Message}");
        }
        catch (NotSupportedException ex)
        {
            _tempestOAuthLastError = ex.Message;
            OnPropertyChanged(nameof(TempestOAuthLastErrorDisplay));
            _iLogger.Warning($"Tempest OAuth secure storage is not supported. {ex.Message}");
        }
        catch (HttpRequestException ex)
        {
            _tempestOAuthLastError = ex.Message;
            OnPropertyChanged(nameof(TempestOAuthLastErrorDisplay));
            _iLogger.Warning($"Tempest OAuth token exchange failed. {ex.Message}");
        }
    }

    async Task TryWarmStartReadingsAsync()
    {
        // The UI may register after the first readings/status are already published.
        // Ask the mux to immediately re-publish cached canonical readings/status.
        // Then, only request an on-demand REST refresh when it is actually needed (REST is active or UDP is stale).

        _firstStatusTcs ??= new TaskCompletionSource<WeatherIngestStatus>(TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            _iEventRelayBasic.Send(new WeatherIngestWarmStartRequest());
        }
        catch (InvalidOperationException ex)
        {
            _iLogger.Warning($"WeatherViewModel: failed to request warm-start. {ex.Message}");
        }

        WeatherIngestStatus? status = null;
        try
        {
            status = await WaitForFirstStatusAsync(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (LinkedCancellationToken.IsCancellationRequested)
        {
            return;
        }

        var mode = status?.SourceMode ?? ReadSourceModeFromSettings();
        if (mode == WeatherIngestSourceMode.UdpOnly)
            return;

        var shouldRequestRestRefresh = mode == WeatherIngestSourceMode.RestOnly
            || status is null
            || status.ActiveSource == WeatherIngestSource.Rest
            || !status.UdpIsFresh;

        if (!shouldRequestRestRefresh)
            return;

        // If the REST provider already has a latest snapshot (cached from disk or early fetch),
        // re-send it so the mux can map/publish immediately even before the network refresh completes.
        try
        {
            var latest = await _iTempestRestObservationsProvider.GetLatestAsync(LinkedCancellationToken).ConfigureAwait(false);
            if (latest is not null)
                _iEventRelayBasic.Send(latest);
        }
        catch (OperationCanceledException) when (LinkedCancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (InvalidOperationException ex)
        {
            _iLogger.Warning($"WeatherViewModel: failed to load latest REST snapshot. {ex.Message}");
        }

        try
        {
            await _iTempestRestObservationsProvider.RequestRefreshAsync(LinkedCancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (LinkedCancellationToken.IsCancellationRequested)
        {
        }
        catch (InvalidOperationException ex)
        {
            _iLogger.Warning($"WeatherViewModel: failed to request REST refresh. {ex.Message}");
        }
    }

    async Task<WeatherIngestStatus?> WaitForFirstStatusAsync(TimeSpan timeout)
    {
        if (_firstStatusTcs is null)
            return null;

        var delayTask = Task.Delay(timeout, LinkedCancellationToken);
        var completed = await Task.WhenAny(_firstStatusTcs.Task, delayTask).ConfigureAwait(false);
        if (completed != _firstStatusTcs.Task)
            return null;

        return await _firstStatusTcs.Task.ConfigureAwait(false);
    }

    WeatherIngestSourceMode ReadSourceModeFromSettings()
    {
        try
        {
            var modeText = _iSettingRepository.GetValueOrDefault<string>(
                LookupDictionaries.WeatherIngestGroupSettingsDefinition.BuildPath(SettingConstants.WeatherIngest_sourceMode));

            if (!Enum.TryParse(modeText, ignoreCase: true, out WeatherIngestSourceMode mode))
                mode = WeatherIngestSourceMode.Auto;

            return mode;
        }
        catch (InvalidOperationException ex)
        {
            _iLogger.Warning($"WeatherViewModel: failed to read ingest settings. {ex.Message}");
            return WeatherIngestSourceMode.Auto;
        }
    }
    // ========================================
    // Clock Timer Logic
    // ========================================
    private void InitializeClockTimer()
    {
        _currentTime = DateTime.Now;
        OnPropertyChanged(nameof(TimeDayOfWeekDisplay));
        OnPropertyChanged(nameof(TimeDateDisplay));
        OnPropertyChanged(nameof(TimeDisplay));

        // Calculate milliseconds until next minute
        var now = DateTime.Now;
        var nextMinute = now.Date.AddHours(now.Hour).AddMinutes(now.Minute + 1);
        var delayUntilNextMinute = (nextMinute - now).TotalMilliseconds;

        // Start timer that fires at the top of the next minute
        _clockTimer = new SystemTimer(delayUntilNextMinute);
        _clockTimer.Elapsed += OnClockTimerTick;
        _clockTimer.AutoReset = false;
        _clockTimer.Start();
    }
    private void OnClockTimerTick(object? sender, ElapsedEventArgs e)
    {
        UpdateTimeDisplay();

        // Reschedule to the next minute boundary so the display always
        // changes exactly when the minute changes, not on a fixed interval.
        if (_clockTimer is null) return;
        var now = DateTime.Now;
        var nextMinute = now.Date.AddHours(now.Hour).AddMinutes(now.Minute + 1);
        _clockTimer.Interval = Math.Max(1.0, (nextMinute - now).TotalMilliseconds);
        _clockTimer.Start();
    }
    private void UpdateTimeDisplay()
    {
        Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
        {
            _currentTime = DateTime.Now;
            OnPropertyChanged(nameof(TimeDayOfWeekDisplay));
            OnPropertyChanged(nameof(TimeDateDisplay));
            OnPropertyChanged(nameof(TimeDisplay));
        });
    }

    void OnWeatherIngestStatusReceived(WeatherIngestStatus status)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _weatherIngestStatus = status;
            _firstStatusTcs?.TrySetResult(status);
            OnPropertyChanged(nameof(ActiveIngestSource));
            OnPropertyChanged(nameof(ActiveIngestSourceDisplay));
            OnPropertyChanged(nameof(RestIsFresh));
            OnPropertyChanged(nameof(UdpIsFresh));
            OnPropertyChanged(nameof(RestLastRetrievedUtc));
            OnPropertyChanged(nameof(UdpLastReceivedUtc));
        });
    }
    // ========================================
    // Event Handlers
    // ========================================
    private void OnWindReceived(WindReading reading)
    {
        // Update on main thread for UI safety
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CurrentWind = reading;
        });
    }
    private void OnObservationReceived(ObservationReading reading)
    {
        // Update on main thread for UI safety
        MainThread.BeginInvokeOnMainThread(
            () => {
                CurrentObservation = reading;
            }
        );
    }
    // ========================================
    // Properties
    // ========================================
    public IWindReading? CurrentWind
    {
        get => _currentWind;
        private set
        {
            if (_currentWind != value)
            {
                _currentWind = value;
                OnPropertyChanged();
            }
        }
    }
    public IObservationReading? CurrentObservation
    {
        get => _currentObservation;
        private set
        {
            if (_currentObservation != value)
            {
                _currentObservation = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PrecipitationTypeDisplay));
                OnPropertyChanged(nameof(RelativeHumidityUnit));
            }
        }
    }

    // ========================================
    // Disposal
    // ========================================
    public void Dispose()
    {
        // Stop status timer if still running
        if (_statusCheckTimer is not null)
        {
            try { _statusCheckTimer.Dispose(); } catch { }
            _statusCheckTimer = null;
        }

        // Unregister from event relay
        try { _iEventRelayBasic.Unregister<WindReading>(this); } catch { }
        try { _iEventRelayBasic.Unregister<ObservationReading>(this); } catch { }
        try { _iEventRelayBasic.Unregister<WeatherIngestStatus>(this); } catch { }
        try { _iEventRelayBasic.Unregister<TempestOAuthInteractiveAuthRequest>(this); } catch { }

        // Stop and dispose clock timer
        try
        {
            if (_clockTimer is not null)
            {
                _clockTimer.Elapsed -= OnClockTimerTick;
                _clockTimer.Dispose();
                _clockTimer = null;
            }
        }
        catch { /* swallow */ }

        // Cancel local cancellation so any background tasks stop
        try { _localCancellation?.Cancel(); } catch { }
        try { _linkedCancellation?.Cancel(); } catch { }

        try { _linkedCancellation?.Dispose(); } catch { }
        try { _localCancellation?.Dispose(); } catch { }
    }
    // ========================================
    // INotifyPropertyChanged
    // ========================================
    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
