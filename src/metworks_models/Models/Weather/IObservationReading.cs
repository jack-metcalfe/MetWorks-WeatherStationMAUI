namespace MetWorksModels.Weather;

/// <summary>
/// Weather observation reading with temperature, humidity, and pressure.
/// Uses RedStar.Amounts for type-safe unit handling with automatic conversions.
/// </summary>
public interface IObservationReading : IWeatherReading
{
    /// <summary>
    /// Temperature measurement with unit (e.g., 72.5°F or 22.5°C).
    /// Unit determined by user preferences at transformation time.
    /// </summary>
    Amount Temperature { get; }

    /// <summary>
    /// Relative humidity as a percentage (0-100).
    /// </summary>
    double HumidityPercent { get; }

    /// <summary>
    /// Atmospheric pressure measurement with unit (e.g., 29.92 inHg or 1013 mbar).
    /// Unit determined by user preferences at transformation time.
    /// </summary>
    Amount Pressure { get; }

    /// <summary>
    /// Calculated dew point temperature (optional - may not be provided by all stations).
    /// </summary>
    Amount? DewPoint { get; }

    /// <summary>
    /// Calculated heat index temperature (optional - only relevant in hot conditions).
    /// </summary>
    Amount? HeatIndex { get; }

    /// <summary>
    /// Calculated wind chill temperature (optional - only relevant in cold/windy conditions).
    /// </summary>
    Amount? WindChill { get; }

    /// <summary>
    /// Station pressure (pressure at station elevation, not sea level adjusted).
    /// </summary>
    Amount? StationPressure { get; }

    /// <summary>
    /// UV index (0-11+ scale).
    /// </summary>
    double? UvIndex { get; }

    /// <summary>
    /// Solar radiation in W/m².
    /// </summary>
    double? SolarRadiation { get; }
}