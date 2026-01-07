namespace MetWorksModels.Provenance;
/// <summary>
/// Tracks errors that occurred during packet processing.
/// Used by ProvenanceTracker for diagnostics and error analysis.
/// </summary>
public record ProcessingError
{
    /// <summary>
    /// Unique identifier for this error (COMB GUID).
    /// </summary>
    public Guid ErrorId { get; init; } = IdGenerator.CreateCombGuid();

    /// <summary>
    /// UTC timestamp when the error occurred.
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Component where the error occurred (e.g., "WeatherDataTransformer").
    /// </summary>
    public required string Component { get; init; }

    /// <summary>
    /// Name of the processing step that failed (e.g., "JSON Parse", "Unit Conversion").
    /// </summary>
    public required string StepName { get; init; }

    /// <summary>
    /// Error message from the exception.
    /// </summary>
    public required string ErrorMessage { get; init; }

    /// <summary>
    /// Exception type name (e.g., "JsonException", "UnitConversionException").
    /// </summary>
    public required string ExceptionType { get; init; }

    /// <summary>
    /// Full stack trace for debugging.
    /// </summary>
    public string? StackTrace { get; init; }

    /// <summary>
    /// Inner exception message, if present.
    /// </summary>
    public string? InnerExceptionMessage { get; init; }

    /// <summary>
    /// COMB GUID of the packet that caused this error.
    /// </summary>
    public required Guid PacketId { get; init; }

    /// <summary>
    /// Creates a ProcessingError from an Exception.
    /// </summary>
    public static ProcessingError FromException(
        Guid packetId,
        string component,
        string stepName,
        Exception exception)
    {
        return new ProcessingError
        {
            Timestamp = DateTime.UtcNow,
            Component = component,
            StepName = stepName,
            ErrorMessage = exception.Message,
            ExceptionType = exception.GetType().Name,
            StackTrace = exception.StackTrace,
            InnerExceptionMessage = exception.InnerException?.Message,
            PacketId = packetId
        };
    }
}