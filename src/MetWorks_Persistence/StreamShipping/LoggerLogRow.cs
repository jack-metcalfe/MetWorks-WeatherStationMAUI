namespace MetWorks.Persistence.StreamShipping;

public sealed record LoggerLogRow(
    long RowId,
    string Id,
    string TimestampUtc,
    string Level,
    string Message,
    string? Exception,
    string? PropertiesJson,
    string? InstallationId);
