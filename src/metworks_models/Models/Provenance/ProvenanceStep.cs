namespace MetWorksModels.Provenance;

/// <summary>
/// Represents a single step in the data processing pipeline.
/// Used by ProvenanceTracker to build complete packet lineage.
/// </summary>
public record ProvenanceStep
{
    /// <summary>
    /// Unique identifier for this processing step (COMB GUID for chronological sorting).
    /// </summary>
    public Guid StepId { get; init; } = IdGenerator.CreateCombGuid();

    /// <summary>
    /// Name of the processing step (e.g., "UDP Receipt", "JSON Parse", "Unit Conversion").
    /// </summary>
    public required string StepName { get; init; }

    /// <summary>
    /// UTC timestamp when this step occurred.
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Component that performed this step (e.g., "UdpTransformer", "WeatherDataTransformer").
    /// </summary>
    public required string Component { get; init; }

    /// <summary>
    /// Optional details about this step (e.g., unit conversion details, error messages).
    /// </summary>
    public string? Details { get; init; }

    /// <summary>
    /// Duration of this step, if measured.
    /// </summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>
    /// Status after this step completed.
    /// </summary>
    public DataStatus? ResultingStatus { get; init; }
}