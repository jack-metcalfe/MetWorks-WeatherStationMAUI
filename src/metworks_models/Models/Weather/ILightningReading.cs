namespace MetWorksModels.Weather;

/// <summary>
/// Lightning detection reading with strike distance and count.
/// Uses RedStar.Amounts for type-safe distance measurements.
/// </summary>
public interface ILightningReading : IWeatherReading
{
    /// <summary>
    /// Distance to the last detected lightning strike with unit (e.g., 5 mi or 8 km).
    /// Unit determined by user preferences at transformation time.
    /// </summary>
    Amount StrikeDistance { get; }

    /// <summary>
    /// Number of lightning strikes detected in the last minute.
    /// </summary>
    int StrikeCount { get; }

    /// <summary>
    /// Average distance of all strikes in the last minute (optional).
    /// </summary>
    Amount? AverageDistance { get; }

    /// <summary>
    /// Energy level of the last strike (station-specific units, if available).
    /// </summary>
    double? StrikeEnergy { get; }

    /// <summary>
    /// Timestamp of the most recent lightning strike.
    /// </summary>
    DateTime? LastStrikeTime { get; }
}