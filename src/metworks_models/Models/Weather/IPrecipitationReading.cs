namespace MetWorksModels.Weather;

/// <summary>
/// Precipitation reading with rain rate and accumulation totals.
/// Uses RedStar.Amounts for type-safe precipitation measurements.
/// </summary>
public interface IPrecipitationReading : IWeatherReading
{
    /// <summary>
    /// Current rain rate (intensity) with unit (e.g., 0.5 in/hr or 12.7 mm/hr).
    /// Unit determined by user preferences at transformation time.
    /// </summary>
    Amount RainRate { get; }

    /// <summary>
    /// Total rainfall since midnight (daily accumulation) with unit.
    /// </summary>
    Amount? DailyAccumulation { get; }

    /// <summary>
    /// Total rainfall in the last hour with unit.
    /// </summary>
    Amount? HourlyAccumulation { get; }

    /// <summary>
    /// Total rainfall in the last minute with unit (for high-resolution monitoring).
    /// </summary>
    Amount? MinuteAccumulation { get; }

    /// <summary>
    /// Precipitation type (rain, snow, sleet, hail) if detected by station.
    /// </summary>
    string? PrecipitationType { get; }
}