namespace MetWorks.Models.Observables.Weather;
/// <summary>
/// Observation reading implementation with temperature, humidity, and pressure.
/// Immutable record with RedStar.Amounts for type-safe unit handling.
/// </summary>
public record ObservationReading : WeatherReading, IObservationReading
{
    /// Derived measurements
    /// <summary>
    /// Atmospheric pressure measurement with unit (e.g., 29.92 inHg or 1013 mbar).
    /// Unit determined by user preferences at transformation time.
    /// Not from device
    /// </summary>
    public Amount? AtmosphericPressure { get; init; }
    /// <summary>
    /// Calculated dew point temperature (optional - may not be provided by all stations).
    /// Not from device
    /// </summary>
    public Amount? DewPoint { get; init; }
    /// Not from device
    public Amount? FeelsLike { get; init; }
    /// <summary>
    /// Calculated heat index temperature (optional - only relevant in hot conditions).
    /// Not from device
    /// </summary>
    public Amount? HeatIndex { get; init; }
    /// <summary>
    /// Calculated wind chill temperature (optional - only relevant in cold/windy conditions).
    /// Not from device
    /// </summary>
    public Amount? WindChill { get; init; }

    /// Direct from device values

    /// <summary>
    /// Temperature measurement with unit (e.g., 72.5°F or 22.5°C).
    /// Unit determined by user preferences at transformation time.
    /// Direct from device in °C
    /// Index 7 in observation array
    /// </summary>
    public required Amount AirTemperature { get; init; }
    /// Direct from device in Volts
    /// Index 16 in observation array
    public required Amount BatteryLevel { get; init; }
    /// <summary>
    /// Epoch time of measurement (UTC)
    /// Direct from device in seconds since Jan 1, 1970.
    /// Index 0 in observation array
    /// </summary>
    public required long EpochTimeOfMeasurement { get; init; }
    /// <summary>
    /// Relative humidity as a percentage (0-100).
    /// Direct from device in Lux
    /// Index 9 in observation array
    /// </summary>
    public required Amount Illuminance { get; init; }
    /// Direct from device
    /// Index 14 in observation array
    /// Direct from device in km
    public required Amount LightningStrikeAverageDistance { get; init; }
    /// Direct from device - count of strikes
    /// Index 15 in observation array
    public required int LightningStrikeCount { get; init; }
    /// Direct from device
    /// 0 = none, 1 = rain, 2 = hail, 3 = rain + hail (experimental)
    /// Index 13 in observation array
    public required int PrecipitationType { get; init; }
    /// Direct from device in mm over previous minute
    /// 0 = none, 1 = rain, 2 = hail, 3 = rain + hail (experimental)
    /// Index 12 in observation array
    public required Amount RainAccumulation { get; init; }
    /// Direct from device in percent
    /// Index 8 in observation array
    public required double RelativeHumidity { get; init; }
    /// Direct from device in minutes
    /// Index 17 in observation array
    public required int ReportingInterval { get; init; }
    /// <summary>
    /// Solar radiation
    /// Direct from device in W/m²
    /// Index 11 in observation array
    /// </summary>
    public required Amount SolarRadiation { get; init; }
    /// <summary>
    /// Station pressure (pressure at station elevation, not sea level adjusted).
    /// Direct from device in MB/millibars
    /// Index 6 in observation array
    /// </summary>
    public required Amount StationPressure { get; init; }
    /// <summary>
    /// UV index (0-11+ scale).
    /// Direct from device in UV index scale
    /// Index 10 in observation array
    /// </summary>
    public required double UvIndex { get; init; }
    /// Direct from device in average over report interval
    /// Index 2 in observation array
    public required Amount WindAverage { get; init; }
    /// Direct from device in degrees
    /// Index 4 in observation array
    public required int WindDirection { get; init; }
    /// Direct from device in highest over report interval, minimum 3 second sample
    /// Index 3 in observation array
    public required Amount WindGust { get; init; }
    /// Direct from device in lowest over report interval, minimum 3 second sample
    /// Index 4 in observation array
    public required Amount WindLull { get; init; }
    /// Direct from device in seconds
    /// Index 5 in observation array
    public required int WindSampleInterval { get; init; }
}