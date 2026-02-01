namespace MetWorks.Models.Observables.Weather;
/// <summary>
/// Wind reading implementation with speed, direction, and gusts.
/// Immutable record with RedStar.Amounts for type-safe speed measurements.
/// </summary>
public record WindReading : WeatherReading, IWindReading
{
    // IWindReading properties
    public required long DeviceReceivedUtcTimestampEpoch { get; init; }
    public required Amount Speed { get; init; }
    public required double DirectionDegrees { get; init; }
    public required string DirectionCardinal { get; init; }
}
