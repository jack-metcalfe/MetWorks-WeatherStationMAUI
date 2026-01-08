using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MetWorksModels.Weather;
using BasicEventRelay;

namespace MetWorksWeather.ViewModels;

/// <summary>
/// ViewModel for displaying current weather readings.
/// Subscribes to weather reading streams via ISingletonEventRelay.
/// Works with both MockWeatherReadingService and real WeatherDataTransformer.
/// </summary>
public class WeatherViewModel : INotifyPropertyChanged, IDisposable
{
    private IWindReading? _currentWind;
    private IObservationReading? _currentObservation;

    public WeatherViewModel()
    {
        // Subscribe to REAL weather readings via ISingletonEventRelay
        // This works with BOTH mock and production services
        ISingletonEventRelay.Register<IWindReading>(this, OnWindReceived);
        ISingletonEventRelay.Register<IObservationReading>(this, OnObservationReceived);
    }

    // ========================================
    // Event Handlers
    // ========================================

    private void OnWindReceived(IWindReading reading)
    {
        // Update on main thread for UI safety
        Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
        {
            CurrentWind = reading;
        });
    }

    private void OnObservationReceived(IObservationReading reading)
    {
        // Update on main thread for UI safety
        Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
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
                OnPropertyChanged(nameof(WindSpeedDisplay));
                OnPropertyChanged(nameof(WindDirectionDisplay));
                OnPropertyChanged(nameof(WindGustDisplay));
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
                OnPropertyChanged(nameof(TemperatureDisplay));
                OnPropertyChanged(nameof(PressureDisplay));
                OnPropertyChanged(nameof(HumidityDisplay));
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
        CurrentWind?.GustSpeed != null 
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
        // Unregister from event relay
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
