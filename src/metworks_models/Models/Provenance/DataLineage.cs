namespace MetWorksModels.Provenance;

/// <summary>
/// Complete lineage record for a weather data packet.
/// Tracks the entire journey from UDP receipt through transformation to persistence and display.
/// Used by ProvenanceTracker for diagnostics, performance analysis, and debugging.
/// </summary>
public record DataLineage
{
    /// <summary>
    /// COMB GUID of the original raw UDP packet (primary key).
    /// </summary>
    public required Guid RawPacketId { get; init; }

    /// <summary>
    /// COMB GUID of the transformed weather reading (if transformation succeeded).
    /// </summary>
    public Guid? TransformedReadingId { get; init; }

    /// <summary>
    /// Database record ID (if successfully persisted to PostgreSQL).
    /// </summary>
    public Guid? DatabaseRecordId { get; init; }

    /// <summary>
    /// Type of weather packet (Observation, Wind, Precipitation, Lightning).
    /// </summary>
    public required PacketEnum PacketType { get; init; }

    /// <summary>
    /// Current status of the packet in the processing pipeline.
    /// </summary>
    public required DataStatus Status { get; init; }

    /// <summary>
    /// UTC timestamp when the packet was first received.
    /// </summary>
    public required DateTime ReceivedUtc { get; init; }

    /// <summary>
    /// UTC timestamp of the last status update.
    /// </summary>
    public DateTime LastUpdated { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Complete ordered list of processing steps.
    /// Chronologically sorted by timestamp for pipeline visualization.
    /// </summary>
    public required List<ProvenanceStep> ProcessingSteps { get; init; } = new();

    /// <summary>
    /// List of errors encountered during processing (if any).
    /// </summary>
    public List<ProcessingError>? Errors { get; init; }

    /// <summary>
    /// Original JSON content of the UDP packet (for debugging).
    /// </summary>
    public string? OriginalJson { get; init; }

    /// <summary>
    /// Total time from receipt to current status.
    /// </summary>
    public TimeSpan TotalProcessingTime => LastUpdated - ReceivedUtc;

    /// <summary>
    /// Indicates if processing failed at any step.
    /// </summary>
    public bool HasErrors => Errors != null && Errors.Count > 0;

    /// <summary>
    /// Gets all processing step names in chronological order.
    /// </summary>
    public IEnumerable<string> GetStepNames() =>
        ProcessingSteps.OrderBy(s => s.Timestamp).Select(s => s.StepName);

    /// <summary>
    /// Gets the most recent processing step.
    /// </summary>
    public ProvenanceStep? GetLastStep() =>
        ProcessingSteps.OrderByDescending(s => s.Timestamp).FirstOrDefault();

    /// <summary>
    /// Gets the total duration of all measured steps.
    /// </summary>
    public TimeSpan GetTotalStepDuration() =>
        TimeSpan.FromMilliseconds(
            ProcessingSteps
                .Where(s => s.Duration.HasValue)
                .Sum(s => s.Duration!.Value.TotalMilliseconds));
}