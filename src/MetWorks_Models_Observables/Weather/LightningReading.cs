namespace MetWorks.Models.Observables.Weather;
/// <summary>
/// Lightning reading implementation with strike distance and count.
/// Immutable record with RedStar.Amounts for type-safe distance measurements.
/// </summary>
public record LightningReading : WeatherReading, ILightningReading
{
    // ILightningReading properties
    public required Amount StrikeDistance { get; init; }
    public required int StrikeCount { get; init; }
    public Amount? AverageDistance { get; init; }
    public double? StrikeEnergy { get; init; }
    public DateTime? LastStrikeTime { get; init; }
}