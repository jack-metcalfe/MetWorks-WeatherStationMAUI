namespace MetWorksModels.Provenance;
/// <summary>
/// Tracks the complete transformation pipeline for a weather reading.
/// Embedded in every IWeatherReading to provide self-describing lineage metadata.
/// </summary>
public record ReadingProvenance
{
    /// <summary>
    /// COMB GUID of the original raw UDP packet (immutable source).
    /// Links back to the source data for complete traceability.
    /// </summary>
    public required Guid RawPacketId { get; init; }

    /// <summary>
    /// UTC timestamp when the UDP packet was received by the system.
    /// </summary>
    public required DateTime UdpReceiptTime { get; init; }

    /// <summary>
    /// UTC timestamp when transformation processing started.
    /// </summary>
    public required DateTime TransformStartTime { get; init; }

    /// <summary>
    /// UTC timestamp when transformation processing completed.
    /// </summary>
    public required DateTime TransformEndTime { get; init; }

    /// <summary>
    /// Duration of the transformation operation.
    /// Computed property for performance analysis.
    /// </summary>
    public TimeSpan TransformDuration => TransformEndTime - TransformStartTime;

    /// <summary>
    /// Total time from UDP receipt to transformation completion.
    /// Computed property for end-to-end pipeline analysis.
    /// </summary>
    public TimeSpan TotalPipelineTime => TransformEndTime - UdpReceiptTime;

    /// <summary>
    /// Source unit names before conversion (e.g., "degree fahrenheit").
    /// Null if no unit conversion was performed.
    /// </summary>
    public string? SourceUnits { get; init; }

    /// <summary>
    /// Target unit names after conversion (e.g., "degree celsius").
    /// Null if no unit conversion was performed.
    /// </summary>
    public string? TargetUnits { get; init; }

    /// <summary>
    /// Version of the transformer that processed this reading.
    /// "1.0" = initial transform, "1.0-retransform" = settings-triggered retransform.
    /// </summary>
    public string TransformerVersion { get; init; } = "1.0";

    /// <summary>
    /// Indicates if this reading was created by a retransformation due to settings change.
    /// </summary>
    public bool IsRetransformation => TransformerVersion.Contains("retransform");
}