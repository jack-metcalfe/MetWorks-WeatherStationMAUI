namespace MetWorks.Apps.MAUI.WeatherStationMaui.ViewModels;
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
    readonly ILoggerResilient _iLoggerResilient;
    readonly ISettingRepository _iSettingRepository;
    readonly IEventRelayBasic _iEventRelayBasic;
    IWindReading? _currentWind;
    IObservationReading? _currentObservation;
    SystemTimer? _clockTimer;
    ThreadingTimer? _statusCheckTimer;
    string? _lastServiceStatusLine;
    DateTime _currentTime = DateTime.Now;

    // Lightweight init guard: 0 = not started, 1 = initializing, 2 = initialized
    int _initializeState = (int)InitializeStateEnum.Uninitialized;

    // Cancellation pattern for cooperative shutdown (optional)
    CancellationTokenSource? _localCancellation;
    CancellationTokenSource? _linkedCancellation;
    CancellationToken LinkedCancellationToken => _linkedCancellation?.Token ?? CancellationToken.None;
    // ========================================
    // Wind Display Properties - 3 second readings
    // ========================================
    public string WindDirectionCardinalInstantValue =>
        CurrentWind is not null
            ? CurrentWind.DirectionCardinal
            : "--";
    public string WindDirectionDegreesInstantValue =>
        CurrentWind is not null
            ? $"{CurrentWind.DirectionDegrees:F0}"
            : "";
    public string WindSpeedInstantUnit =>
        CurrentWind is not null
            ? CurrentWind.Speed.Unit.Symbol
            : "";
    public double WindSpeedInstantValue =>
        CurrentWind is not null
            ? CurrentWind.Speed.Value
            : double.NaN;
    public string WindSpeedInstantDisplay =>
        CurrentWind is not null
            ? $"{CurrentWind.Speed.Value:F1} {CurrentWind.Speed.Unit.Symbol}"
            : "-- (waiting for data)";
    public string WindDeviceReceivedUtcTimestampEpochInstant =>
        CurrentWind is not null
            ? $"{new DateTime(1970, 1, 1).AddSeconds(CurrentWind.DeviceReceivedUtcTimestampEpoch).ToLocalTime():yyyy-MM-dd HH:mm:ss}"
            : "--";
    public string WindDirectionInstantDisplay =>
        CurrentWind is not null
            ? $"{CurrentWind.DirectionCardinal} ({CurrentWind.DirectionDegrees:F0}°)"
            : "--";
    public string WindHubSerialNumberInstant =>
        CurrentWind is not null
            ? $"{CurrentWind.HubSerialNumber}"
            : "--";
    public string WindSerialNumberInstant =>
        CurrentWind is not null
            ? $"{CurrentWind.SerialNumber}"
            : "--";
    public string WindTypeInstant =>
        CurrentWind is not null
            ? $"{CurrentWind.Type}"
            : "--";
    // ========================================
    // Observation Reading Display Properties
    // ========================================
    public string AirTemperatureUnit =>
        CurrentObservation is not null
            ? $"{CurrentObservation.AirTemperature.Unit.Symbol}"
            : "--";
    public double AirTemperatureValue =>
        CurrentObservation is not null
            ? CurrentObservation.AirTemperature.Value
            : double.NaN;
    public string BatteryLevelDisplay =>
        CurrentObservation is not null
            ? $"{CurrentObservation.BatteryLevel.Value:F2} {CurrentObservation.BatteryLevel.Unit.Symbol}"
            : "--";
    public string BatteryLevelUnit =>
        CurrentObservation is not null
            ? $"{CurrentObservation.BatteryLevel.Unit.Symbol}"
            : "--";
    public string EpochTimestampUtcDisplay =>
        CurrentObservation is not null
            ? $"{new DateTime(1970, 1, 1).AddSeconds(CurrentObservation.EpochTimeOfMeasurement).ToLocalTime():yyyy-MM-dd HH:mm:ss}"
            : "--";
    public string IlluminanceDisplay =>
        CurrentObservation is not null
            ? $"{CurrentObservation.Illuminance.Value:F0} {CurrentObservation.Illuminance.Unit.Symbol}"
            : "--";
    public string LightningStrikeAverageDistanceDisplay =>
        CurrentObservation is not null
            ? $"{CurrentObservation.LightningStrikeAverageDistance.Value:F0} {CurrentObservation.LightningStrikeAverageDistance.Unit.Symbol}"
            : "--";
    public string LightningStrikeCount =>
        CurrentObservation is not null
            ? $"{CurrentObservation.LightningStrikeCount}"
            : "--";
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
    public string RainAccumulationDisplay =>
        CurrentObservation is not null
            ? $"{CurrentObservation.RainAccumulation.Value:F2} {CurrentObservation.RainAccumulation.Unit.Symbol}"
            : "--";
    public string RelativeHumidityUnit => "%";
    public double RelativeHumidityValue =>
        CurrentObservation is not null
            ? CurrentObservation.RelativeHumidity
            : double.NaN;
    public string ReportingIntervalValue =>
        CurrentObservation is not null
            ? $"{CurrentObservation.ReportingInterval:F0}"
            : "--";
    public string SolarRadiationDisplay =>
        CurrentObservation is not null
            ? $"{CurrentObservation.SolarRadiation.Value:F0} {CurrentObservation.SolarRadiation.Unit.Symbol}"
            : "--";
    public string StationPressureDisplay =>
        CurrentObservation is not null
            ? $"{CurrentObservation.StationPressure.Value:F2} {CurrentObservation.StationPressure.Unit.Symbol}"
            : "--";
    // ========================================
    // UV Index Display Properties
    // ========================================
    public double UvIndexValue =>
        CurrentObservation is not null
            ? CurrentObservation.UvIndex
            : double.NaN;
    public string UvIndexUnit => ""; // UV Index has no unit
    public string WindAverageDisplay =>
        CurrentObservation is not null
            ? $"{CurrentObservation.WindAverage.Value:F0} {CurrentObservation.WindAverage.Unit.Symbol}"
            : "--";
    public string WindGustDisplay =>
        CurrentObservation is not null
            ? $"{CurrentObservation.WindGust.Value:F0} {CurrentObservation.WindGust.Unit.Symbol}"
            : "--";
    public string WindLullDisplay =>
        CurrentObservation is not null
            ? $"{CurrentObservation.WindLull.Value:F0} {CurrentObservation.WindLull.Unit.Symbol}"
            : "--";
    public string WindSampleInterval =>
        CurrentObservation is not null
            ? $"{CurrentObservation.WindSampleInterval:F0}"
            : "--";
    // ========================================
    // Time Display Properties
    // ========================================
    public string TimeDayOfWeekDisplay => _currentTime.ToString("ddd");
    public string TimeDateDisplay => _currentTime.ToString("MMM dd");
    public string TimeDisplay => _currentTime.ToString("HH:mm");
    public WeatherViewModel(
        ILoggerResilient iLoggerResilient,
        ISettingRepository iSettingRepository,
        IEventRelayBasic iEventRelayBasic
    )
    {
        ArgumentNullException.ThrowIfNull(iLoggerResilient);
        ArgumentNullException.ThrowIfNull(iSettingRepository);
        ArgumentNullException.ThrowIfNull(iEventRelayBasic);

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
            var iExternalCancellationToken = StartupInitializer.Registry.GetRootCancellationTokenSource().Token;

            // Create local and linked cancellation sources so we can honor external cancellation if provided.
            _localCancellation = new CancellationTokenSource();
            _linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(iExternalCancellationToken, _localCancellation.Token);

            Interlocked.Exchange(ref _initializeState, (int)InitializeStateEnum.Initialized);

            // Stop status checks once we begin real initialization
            if (_statusCheckTimer is not null)
            {
                try { _statusCheckTimer.Dispose(); } catch { /* swallow */ }
                _statusCheckTimer = null;
            }

            // Register for events
            _iEventRelayBasic.Register<IWindReading>(this, OnWindReceived);
            _iEventRelayBasic.Register<IObservationReading>(this, OnObservationReceived);

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

            _iLoggerResilient.Error("Failed to initialize", exception);
            throw;
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
        _clockTimer.Elapsed += OnClockTimerFirstTick;
        _clockTimer.AutoReset = false; // Fire once, then reconfigure
        _clockTimer.Start();
    }
    private void OnClockTimerFirstTick(object? sender, ElapsedEventArgs e)
    {
        // First tick - now synchronized to top of minute
        UpdateTimeDisplay();

        // Reconfigure timer for every minute from now on
        if (_clockTimer is not null)
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
            OnPropertyChanged(nameof(TimeDayOfWeekDisplay));
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

                OnPropertyChanged(nameof(WindDirectionCardinalInstantValue));
                OnPropertyChanged(nameof(WindDirectionDegreesInstantValue));
                OnPropertyChanged(nameof(WindSpeedInstantUnit));
                OnPropertyChanged(nameof(WindSpeedInstantValue));
                OnPropertyChanged(nameof(WindSpeedInstantDisplay));
                OnPropertyChanged(nameof(WindDirectionInstantDisplay));
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

                OnPropertyChanged(nameof(AirTemperatureUnit));
                OnPropertyChanged(nameof(AirTemperatureValue));
                OnPropertyChanged(nameof(BatteryLevelDisplay));
                OnPropertyChanged(nameof(BatteryLevelUnit));
                OnPropertyChanged(nameof(EpochTimestampUtcDisplay));
                OnPropertyChanged(nameof(IlluminanceDisplay));
                OnPropertyChanged(nameof(LightningStrikeAverageDistanceDisplay));
                OnPropertyChanged(nameof(LightningStrikeCount));
                OnPropertyChanged(nameof(PrecipitationTypeDisplay));
                OnPropertyChanged(nameof(RainAccumulationDisplay));
                OnPropertyChanged(nameof(RelativeHumidityUnit));
                OnPropertyChanged(nameof(RelativeHumidityValue));
                OnPropertyChanged(nameof(ReportingIntervalValue));
                OnPropertyChanged(nameof(SolarRadiationDisplay));
                OnPropertyChanged(nameof(StationPressureDisplay));
                OnPropertyChanged(nameof(UvIndexValue));
                OnPropertyChanged(nameof(WindAverageDisplay));
                OnPropertyChanged(nameof(WindGustDisplay));
                OnPropertyChanged(nameof(WindLullDisplay));
                OnPropertyChanged(nameof(WindSampleInterval));
            }
        }
    }

    // ========================================
    // Display Properties for UI Binding
    // ========================================
    public string PressureDisplay =>
        CurrentObservation is not null
            ? $"{CurrentObservation.StationPressure.Value:F2} {CurrentObservation.StationPressure.Unit.Symbol}"
            : "--";
    public string HumidityDisplay =>
        CurrentObservation is not null
            ? $"{CurrentObservation.RelativeHumidity:F0}%"
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

        // Unregister from event relay
        try { _iEventRelayBasic.Unregister<IWindReading>(this); } catch { }
        try { _iEventRelayBasic.Unregister<IObservationReading>(this); } catch { }

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
