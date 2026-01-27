namespace MetWorks.Apps.MAUI.WeatherStationMaui.ViewModels;

/// <summary>
/// ViewModel for displaying current weather readings.
/// Subscribes to weather reading streams via ISingletonEventRelay.
/// Works with both MockWeatherReadingService and real WeatherDataTransformer.
/// </summary>
public class WeatherViewModel : INotifyPropertyChanged, IDisposable
{
    bool _isInitialized = false;
    ILoggerResilient? _iLoggerResilient = null;
    ILoggerResilient ILoggerResilient => NullPropertyGuard.Get(_isInitialized, _iLoggerResilient, nameof(ILoggerResilient));
    ISettingRepository? _iSettingRepository = null;
    ISettingRepository ISettingRepository => NullPropertyGuard.Get(_isInitialized, _iSettingRepository, nameof(ISettingRepository));
    IEventRelayBasic? _iEventRelayBasic = null;
    IEventRelayBasic IEventRelayBasic => NullPropertyGuard.Get(_isInitialized, _iEventRelayBasic, nameof(IEventRelayBasic));
    IWindReading? _currentWind;
    IObservationReading? _currentObservation;
    SystemTimer? _clockTimer;
    ThreadingTimer? _statusCheckTimer;
    string? _lastServiceStatusLine;
    DateTime _currentTime = DateTime.Now;

    // Lightweight init guard: 0 = not started, 1 = initializing, 2 = initialized
    int _initializeState = 0;

    // Cancellation pattern for cooperative shutdown (optional)
    CancellationTokenSource? _localCancellation;
    CancellationTokenSource? _linkedCancellation;
    CancellationToken LinkedCancellationToken => _linkedCancellation?.Token ?? CancellationToken.None;

    // ========================================
    // Temperature Display Properties
    // ========================================
    public string TemperatureValue =>
        CurrentObservation != null
            ? $"{CurrentObservation.Temperature.Value:F0}"
            : "--";
    public string TemperatureUnit =>
        CurrentObservation != null
            ? $"{CurrentObservation.Temperature.Unit.Symbol}"
            : "";
    // ========================================
    // Wind Display Properties
    // ========================================
    public string WindSpeedValue =>
        CurrentWind != null
            ? $"{CurrentWind.Speed.Value:F0}"
            : "--";
    public string WindSpeedUnit =>
        CurrentWind != null
            ? CurrentWind.Speed.Unit.Symbol
            : "";
    public string WindDirectionCardinal =>
        CurrentWind != null
            ? CurrentWind.DirectionCardinal
            : "--";
    public string WindDirectionDegrees =>
        CurrentWind != null
            ? $"{CurrentWind.DirectionDegrees:F0}°"
            : "";
    // ========================================
    // Humidity Display Properties
    // ========================================
    public string HumidityValue =>
        CurrentObservation != null
            ? $"{CurrentObservation.HumidityPercent:F0}"
            : "--";
    public string HumidityUnit => "%";
    // ========================================
    // UV Index Display Properties
    // ========================================
    public string UvIndexValue =>
        CurrentObservation?.UvIndex != null
            ? $"{CurrentObservation.UvIndex:F0}"
            : "--";
    public string UvIndexUnit => ""; // UV Index has no unit
    // ========================================
    // Time Display Properties
    // ========================================
    public string TimeDayOfWeek => _currentTime.ToString("ddd");
    public string TimeDateDisplay => _currentTime.ToString("MMM dd");
    public string TimeDisplay => _currentTime.ToString("HH:mm");
    public WeatherViewModel(
        ILoggerResilient iLoggerResilient,
        ISettingRepository iSettingRepository,
        IEventRelayBasic iEventRelayBasic
    )
    {
        _iLoggerResilient = iLoggerResilient;
        _iSettingRepository = iSettingRepository;
        _iEventRelayBasic = iEventRelayBasic;
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
            TimeSpan.FromSeconds(5));
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

    // Accept optional external CancellationToken so this viewmodel can be cooperatively cancelled.
    public async Task<bool> InitializeAsync()
    {
        // Quick check: if already marked initialized return true
        if (Interlocked.CompareExchange(ref _initializeState, 2, 2) == 2)
            return await Task.FromResult(true);

        // Try to transition from 0 -> 1 (not started -> initializing)
        var prior = Interlocked.CompareExchange(ref _initializeState, 1, 0);
        if (prior == 1)
        {
            // someone else is initializing
            return await Task.FromResult(false);
        }
        if (prior == 2)
        {
            // already initialized
            return await Task.FromResult(true);
        }

        try
        {
            // Acquire dependencies using registry (existing pattern)
            var iExternalCancellationToken = StartupInitializer.Registry.GetRootCancellationTokenSource().Token;

            // Create local and linked cancellation sources so we can honor external cancellation if provided.
            _localCancellation = new CancellationTokenSource();
            _linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(iExternalCancellationToken, _localCancellation.Token);

            // Mark initialized so NullPropertyGuard does not throw when used by callbacks.
            _isInitialized = true;
            Interlocked.Exchange(ref _initializeState, 2);

            // Stop status checks once we begin real initialization
            if (_statusCheckTimer is not null)
            {
                try { _statusCheckTimer.Dispose(); } catch { /* swallow */ }
                _statusCheckTimer = null;
            }

            // Register for events using backing field (avoid guarded property in setup)
            _iEventRelayBasic?.Register<IWindReading>(this, OnWindReceived);
            _iEventRelayBasic?.Register<IObservationReading>(this, OnObservationReceived);

            InitializeClockTimer();

            return await Task.FromResult(true);
        }
        catch (Exception exception)
        {
            // Reset init state so caller can retry later
            Interlocked.Exchange(ref _initializeState, 0);
            _isInitialized = false;

            // Use backing logger field — property might throw if initialization failed earlier
            _iLoggerResilient?.Error("Failed to initialize", exception);
            throw;
        }
    }
    // ========================================
    // Clock Timer Logic
    // ========================================
    private void InitializeClockTimer()
    {
        _currentTime = DateTime.Now;
        OnPropertyChanged(nameof(TimeDayOfWeek));
        OnPropertyChanged(nameof(TimeDateDisplay));
        OnPropertyChanged(nameof(TimeDisplay));

        // Calculate milliseconds until next minute
        var now = DateTime.Now;
        var nextMinute = now.Date.AddHours(now.Hour).AddMinutes(now.Minute + 1);
        var delayUntilNextMinute = (nextMinute - now).TotalMilliseconds;

        // Start timer that fires at the top of the next minute
        _clockTimer = new SystemTimer(delayUntilNextMinute);
        _clockTimer.Elapsed += OnClockTimerFirstTick;
        _clockTimer.AutoReset = false; // Fire once, then reconfigure
        _clockTimer.Start();
    }
    private void OnClockTimerFirstTick(object? sender, ElapsedEventArgs e)
    {
        // First tick - now synchronized to top of minute
        UpdateTimeDisplay();

        // Reconfigure timer for every minute from now on
        if (_clockTimer != null)
        {
            _clockTimer.Elapsed -= OnClockTimerFirstTick;
            _clockTimer.Elapsed += OnClockTimerTick;
            _clockTimer.Interval = 60000; // 60 seconds
            _clockTimer.AutoReset = true;
            _clockTimer.Start();
        }
    }
    private void OnClockTimerTick(object? sender, ElapsedEventArgs e)
    {
        UpdateTimeDisplay();
    }
    private void UpdateTimeDisplay()
    {
        Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
        {
            _currentTime = DateTime.Now;
            OnPropertyChanged(nameof(TimeDayOfWeek));
            OnPropertyChanged(nameof(TimeDateDisplay));
            OnPropertyChanged(nameof(TimeDisplay));
        });
    }
    // ========================================
    // Event Handlers
    // ========================================
    private void OnWindReceived(IWindReading reading)
    {
        // Update on main thread for UI safety
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CurrentWind = reading;
        });
    }
    private void OnObservationReceived(IObservationReading reading)
    {
        // Update on main thread for UI safety
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CurrentObservation = reading;
        });
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

                OnPropertyChanged(nameof(WindSpeedUnit));
                OnPropertyChanged(nameof(WindDirectionCardinal));
                OnPropertyChanged(nameof(WindDirectionDegrees));
                OnPropertyChanged(nameof(WindSpeedValue));
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
                OnPropertyChanged(nameof(HumidityUnit));
                OnPropertyChanged(nameof(HumidityValue));

                OnPropertyChanged(nameof(TemperatureUnit));
                OnPropertyChanged(nameof(TemperatureValue));

                OnPropertyChanged(nameof(UvIndexValue));
            }
        }
    }

    // ========================================
    // Display Properties for UI Binding
    // ========================================

    public string WindSpeedDisplay =>
        CurrentWind != null
            ? $"{CurrentWind.Speed.Value:F1} {CurrentWind.Speed.Unit.Symbol}"
            : "-- (waiting for data)";
    public string WindDirectionDisplay =>
        CurrentWind != null
            ? $"{CurrentWind.DirectionCardinal} ({CurrentWind.DirectionDegrees:F0}°)"
            : "--";
    public string WindGustDisplay =>
        CurrentWind?.GustSpeed is not null
            ? $"{CurrentWind.GustSpeed.Value:F1} {CurrentWind.GustSpeed.Unit.Symbol}"
            : "--";
    public string TemperatureDisplay =>
        CurrentObservation != null
            ? $"{CurrentObservation.Temperature.Value:F1}°{CurrentObservation.Temperature.Unit.Symbol}"
            : "-- (waiting for data)";

    public string PressureDisplay =>
        CurrentObservation != null
            ? $"{CurrentObservation.Pressure.Value:F2} {CurrentObservation.Pressure.Unit.Symbol}"
            : "--";

    public string HumidityDisplay =>
        CurrentObservation != null
            ? $"{CurrentObservation.HumidityPercent:F0}%"
            : "--";

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

        // Unregister from event relay using backing field to avoid NullPropertyGuard throws
        try { _iEventRelayBasic?.Unregister<IWindReading>(this); } catch { }
        try { _iEventRelayBasic?.Unregister<IObservationReading>(this); } catch { }

        // Stop and dispose clock timer
        try
        {
            if (_clockTimer is not null)
            {
                _clockTimer.Elapsed -= OnClockTimerFirstTick;
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

        // Do not reference guarded properties after disposal
        _isInitialized = false;
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
