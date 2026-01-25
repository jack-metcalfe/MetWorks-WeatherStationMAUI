namespace MetWorks.Interfaces;
/// <summary>
/// Read-only contract describing provenance metadata embedded in weather readings.
/// Mirrors the shape of <see cref="ReadingProvenance"/>.
/// </summary>
public interface IReadingProvenance
{
    /// <summary>
    /// COMB GUID of the original raw UDP packet (immutable source).
    /// </summary>
    Guid RawPacketId { get; init; }

    /// <summary>
    /// UTC timestamp when the UDP packet was received by the system.
    /// </summary>
    DateTime UdpReceiptTime { get; init; }

    /// <summary>
    /// UTC timestamp when transformation processing started.
    /// </summary>
    DateTime TransformStartTime { get; init; }

    /// <summary>
    /// UTC timestamp when transformation processing completed.
    /// </summary>
    DateTime TransformEndTime { get; init; }

    /// <summary>
    /// Duration of the transformation operation (computed).
    /// </summary>
    TimeSpan TransformDuration { get; }

    /// <summary>
    /// Total time from UDP receipt to transformation completion (computed).
    /// </summary>
    TimeSpan TotalPipelineTime { get; }

    /// <summary>
    /// Source unit names before conversion (nullable).
    /// </summary>
    string? SourceUnits { get; init; }

    /// <summary>
    /// Target unit names after conversion (nullable).
    /// </summary>
    string? TargetUnits { get; init; }

    /// <summary>
    /// Version of the transformer that processed this reading.
    /// </summary>
    string TransformerVersion { get; init; }

    /// <summary>
    /// Indicates if this reading was created by a retransformation due to settings change.
    /// </summary>
    bool IsRetransformation { get; }
}