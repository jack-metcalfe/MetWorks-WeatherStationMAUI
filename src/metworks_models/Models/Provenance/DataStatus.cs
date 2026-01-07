namespace MetWorksModels.Provenance;

/// <summary>
/// Represents the lifecycle state of a weather data packet as it flows through the system.
/// Matches ARCHITECTURE.md appendix specification.
/// </summary>
public enum DataStatus
{
    /// <summary>
    /// UDP packet received from weather station, COMB GUID assigned.
    /// </summary>
    Received,

    /// <summary>
    /// JSON parsed successfully, packet type identified.
    /// </summary>
    Parsed,

    /// <summary>
    /// Converted to typed reading with RedStar.Amounts and user preferences applied.
    /// </summary>
    Transformed,

    /// <summary>
    /// Transformed again due to settings change (unit preference update).
    /// </summary>
    Retransformed,

    /// <summary>
    /// Successfully saved to PostgreSQL database.
    /// </summary>
    Persisted,

    /// <summary>
    /// Processing failed at some stage in the pipeline.
    /// </summary>
    Failed,

    /// <summary>
    /// Waiting in buffer due to database unavailability.
    /// </summary>
    Buffered,

    /// <summary>
    /// Rendered in UI to user.
    /// </summary>
    Displayed
}