namespace MetWorks.Interfaces;
/// <summary>
/// Wind reading with speed, direction, and gusts.
/// Uses RedStar.Amounts for type-safe speed measurements.
/// </summary>
public interface IWindReading : IWeatherReading
{
    long DeviceReceivedUtcTimestampEpoch { get; }
    /// <summary>
    /// Wind direction as cardinal/intercardinal direction (N, NE, E, SE, S, SW, W, NW).
    /// Computed from DirectionDegrees for display purposes.
    /// </summary>
    string DirectionCardinal { get; }
    /// <summary>
    /// Wind direction in degrees (0-360, where 0/360 = North, 90 = East, 180 = South, 270 = West).
    /// </summary>
    double DirectionDegrees { get; }
    /// <summary>
    /// Current wind speed with unit (e.g., 15 mph or 24 km/h).
    /// Unit determined by user preferences at transformation time.
    /// </summary>
    Amount Speed { get; }
}