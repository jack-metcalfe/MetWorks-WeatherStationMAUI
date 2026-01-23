namespace MetWorks.Models.Observables.Weather;
/// <summary>
/// Observation reading implementation with temperature, humidity, and pressure.
/// Immutable record with RedStar.Amounts for type-safe unit handling.
/// </summary>
public record ObservationReading : IObservationReading
{
    // IWeatherReading properties
    public required Guid Id { get; init; }
    public required Guid SourcePacketId { get; init; }
    public required DateTime Timestamp { get; init; }
    public required DateTime ReceivedUtc { get; init; }
    public PacketEnum PacketType => PacketEnum.Observation;
    public required IReadingProvenance Provenance { get; init; }

    // IObservationReading properties
    public required Amount Temperature { get; init; }
    public required double HumidityPercent { get; init; }
    public required Amount Pressure { get; init; }
    public Amount? DewPoint { get; init; }
    public Amount? HeatIndex { get; init; }
    public Amount? WindChill { get; init; }
    public Amount? StationPressure { get; init; }
    public double? UvIndex { get; init; }
    public double? SolarRadiation { get; init; }
}
