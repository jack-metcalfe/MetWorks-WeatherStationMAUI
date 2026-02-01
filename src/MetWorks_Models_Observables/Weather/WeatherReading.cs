namespace MetWorks.Models.Observables.Weather;
public record WeatherReading : Reading, IWeatherReading
{
    // IWeatherReading properties
    public required Guid Id { get; init; }
    public required Guid SourcePacketId { get; init; }
    public required DateTime Timestamp { get; init; }
    public required DateTime ReceivedUtc { get; init; }
    public PacketEnum PacketType => PacketEnum.Precipitation;
    public required IReadingProvenance Provenance { get; init; }
}
