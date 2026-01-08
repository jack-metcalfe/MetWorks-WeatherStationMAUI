using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Timers;
using MetWorksModels.Weather;
using BasicEventRelay;
using Timer = System.Timers.Timer;

namespace MetWorksWeather.ViewModels;

/// <summary>
/// ViewModel for large-format weather display (viewable from ~20 feet).
/// Extends WeatherViewModel with separated value/unit properties and time display.
/// </summary>
public class LargeFormatWeatherViewModel : INotifyPropertyChanged, IDisposable
{
    private IWindReading? _currentWind;
    private IObservationReading? _currentObservation;
    private Timer? _clockTimer;
    private DateTime _currentTime = DateTime.Now;

    public LargeFormatWeatherViewModel()
    {
        // Subscribe to weather readings
        ISingletonEventRelay.Register<IWindReading>(this, OnWindReceived);
        ISingletonEventRelay.Register<IObservationReading>(this, OnObservationReceived);
        
        // Initialize time display and start clock timer
        InitializeClockTimer();
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
        _clockTimer = new Timer(delayUntilNextMinute);
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
        Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
        {
            CurrentWind = reading;
        });
    }

    private void OnObservationReceived(IObservationReading reading)
    {
        Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
        {
            CurrentObservation = reading;
        });
    }

    // ========================================
    // Current Data Properties
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
                
                // Notify all wind-related display properties
                OnPropertyChanged(nameof(WindSpeedValue));
                OnPropertyChanged(nameof(WindSpeedUnit));
                OnPropertyChanged(nameof(WindDirectionCardinal));
                OnPropertyChanged(nameof(WindDirectionDegrees));
                OnPropertyChanged(nameof(HasWindData));
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
                
                // Notify all observation-related display properties
                OnPropertyChanged(nameof(TemperatureValue));
                OnPropertyChanged(nameof(TemperatureUnit));
                OnPropertyChanged(nameof(HumidityValue));
                OnPropertyChanged(nameof(UvIndexValue));
                OnPropertyChanged(nameof(HasObservationData));
            }
        }
    }

    // ========================================
    // Temperature Display Properties
    // ========================================

    public bool HasObservationData => CurrentObservation != null;

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

    public bool HasWindData => CurrentWind != null;

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
            ? $"({CurrentWind.DirectionDegrees:F0}°)" 
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

    // ========================================
    // Disposal
    // ========================================

    public void Dispose()
    {
        _clockTimer?.Stop();
        _clockTimer?.Dispose();
        ISingletonEventRelay.Unregister<IWindReading>(this);
        ISingletonEventRelay.Unregister<IObservationReading>(this);
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