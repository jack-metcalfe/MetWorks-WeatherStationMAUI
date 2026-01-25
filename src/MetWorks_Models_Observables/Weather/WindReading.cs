namespace MetWorks.Models.Observables.Weather;
/// <summary>
/// Wind reading implementation with speed, direction, and gusts.
/// Immutable record with RedStar.Amounts for type-safe speed measurements.
/// </summary>
public record WindReading : IWindReading
{
    // IWeatherReading properties
    public required Guid Id { get; init; }
    public required Guid SourcePacketId { get; init; }
    public required DateTime Timestamp { get; init; }
    public required DateTime ReceivedUtc { get; init; }
    public PacketEnum PacketType => PacketEnum.Wind;
    public required IReadingProvenance Provenance { get; init; }

    // IWindReading properties
    public required Amount Speed { get; init; }
    public required double DirectionDegrees { get; init; }
    public required string DirectionCardinal { get; init; }
    public Amount? GustSpeed { get; init; }
    public Amount? AverageSpeed { get; init; }
    public Amount? LullSpeed { get; init; }
}
