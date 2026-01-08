using System;
using MetWorksModels.Weather;

namespace MetWorksWeather.Services;

/// <summary>
/// Service interface for receiving real-time weather readings.
/// Provides Observable streams for different weather reading types.
/// </summary>
public interface IWeatherReadingService
{
    /// <summary>
    /// Observable stream of wind readings (speed, direction, gusts).
    /// </summary>
    IObservable<IWindReading> WindReadings { get; }
    
    /// <summary>
    /// Observable stream of observation readings (temperature, pressure, humidity).
    /// </summary>
    IObservable<IObservationReading> ObservationReadings { get; }
    
    /// <summary>
    /// Indicates whether the service is currently running and emitting readings.
    /// </summary>
    bool IsRunning { get; }
    
    /// <summary>
    /// Starts the weather reading service.
    /// Begins emitting readings through the observable streams.
    /// </summary>
    void Start();
    
    /// <summary>
    /// Stops the weather reading service.
    /// Stops emitting readings through the observable streams.
    /// </summary>
    void Stop();
}
