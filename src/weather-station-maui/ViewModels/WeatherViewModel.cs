namespace MetWorksWeather.ViewModels;

/// <summary>
/// ViewModel for displaying current weather readings.
/// Subscribes to weather reading streams via ISingletonEventRelay.
/// Works with both MockWeatherReadingService and real WeatherDataTransformer.
/// </summary>
public class WeatherViewModel : INotifyPropertyChanged, IDisposable
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
    IWindReading? _currentWind;
    IObservationReading? _currentObservation;
    SystemTimer? _clockTimer;
    ThreadingTimer? _statusCheckTimer;
    DateTime _currentTime = DateTime.Now;
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
    public WeatherViewModel()
    {
        StartServiceStatusMonitoring();
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

                    Debug.WriteLine($"Service Status: {serviceStatus} | Database: {dbStatus}");

                    if (isInitialized) Task.Run(InitializeAsync);

                    // Update UI elements if you have them
                    // Example: StatusLabel.Text = $"{serviceStatus} | DB: {dbStatus}";
                }
                catch (Exception exception)
                {
                    Debug.WriteLine($"Error checking service status: {exception}");
                }
            }
        );
    }
    public async Task<bool> InitializeAsync()
    {
        try
        {
            _iLogger = StartupInitializer.Registry.GetTheLoggerFile();
            _iEventRelayBasic = StartupInitializer.Registry.GetTheEventRelayBasic();

            if (_statusCheckTimer is not null)
            {
                _statusCheckTimer?.Dispose();
                _statusCheckTimer = null;
            }

            // Subscribe to REAL weather readings via IEventRelay
            // This works with BOTH mock and production services

            // Register for wind reading events
            _iEventRelayBasic.Register<IWindReading>(
                this, OnWindReceived
            );
            // Register for observation reading events
            _iEventRelayBasic.Register<IObservationReading>(
                this, OnObservationReceived
            );

            InitializeClockTimer();

            _isInitialized = true;
        }

        catch (Exception exception)
        {
            ILogger.Error("Failed to initialize", exception);
            throw;
        }

        return await Task.FromResult(_isInitialized);
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
        if (_statusCheckTimer is not null)
        {
            _statusCheckTimer?.Dispose();
            _statusCheckTimer = null;
        }

        // Unregister from event relay
        IEventRelayBasic.Unregister<IWindReading>(this);
        IEventRelayBasic.Unregister<IObservationReading>(this);
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
