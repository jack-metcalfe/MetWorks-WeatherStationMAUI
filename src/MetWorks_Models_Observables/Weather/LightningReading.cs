namespace MetWorks.Models.Observables.Weather;
/// <summary>
/// Lightning reading implementation with strike distance and count.
/// Immutable record with RedStar.Amounts for type-safe distance measurements.
/// </summary>
public record LightningReading : ILightningReading
{
    // IWeatherReading properties
    public required Guid Id { get; init; }
    public required Guid SourcePacketId { get; init; }
    public required DateTime Timestamp { get; init; }
    public required DateTime ReceivedUtc { get; init; }
    public PacketEnum PacketType => PacketEnum.Lightning;
    public required IReadingProvenance Provenance { get; init; }

    // ILightningReading properties
    public required Amount StrikeDistance { get; init; }
    public required int StrikeCount { get; init; }
    public Amount? AverageDistance { get; init; }
    public double? StrikeEnergy { get; init; }
    public DateTime? LastStrikeTime { get; init; }
}