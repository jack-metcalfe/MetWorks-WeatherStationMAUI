namespace MetWorksModels.Weather;
/// <summary>
/// Base interface for all weather readings.
/// Every reading embeds provenance metadata and links back to its source packet.
/// </summary>
public interface IWeatherReading
{
    /// <summary>
    /// Unique identifier for this transformed reading (COMB GUID).
    /// Each transformation (including retransformations) gets a new ID.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// COMB GUID of the original raw UDP packet (immutable source).
    /// Links this reading back to its source for complete traceability.
    /// </summary>
    Guid SourcePacketId { get; }

    /// <summary>
    /// Timestamp from the weather station (weather event time).
    /// </summary>
    DateTime Timestamp { get; }

    /// <summary>
    /// UTC timestamp when the system received the UDP packet.
    /// </summary>
    DateTime ReceivedUtc { get; }

    /// <summary>
    /// Type of weather packet (Observation, Wind, Precipitation, Lightning).
    /// </summary>
    PacketEnum PacketType { get; }

    /// <summary>
    /// Complete provenance metadata tracking the transformation pipeline.
    /// Includes timing, unit conversions, and transformation version.
    /// </summary>
    ReadingProvenance Provenance { get; }
}   