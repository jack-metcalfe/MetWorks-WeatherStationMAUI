namespace MetWorks.Models.Observables.Weather;
/// <summary>
/// Precipitation reading implementation with rain rate and accumulation totals.
/// Immutable record with RedStar.Amounts for type-safe precipitation measurements.
/// </summary>
public record PrecipitationReading : WeatherReading, IPrecipitationReading
{
    // IPrecipitationReading properties
    public required Amount RainRate { get; init; }
    public Amount? DailyAccumulation { get; init; }
    public Amount? HourlyAccumulation { get; init; }
    public Amount? MinuteAccumulation { get; init; }
    public string? PrecipitationType { get; init; }
}
