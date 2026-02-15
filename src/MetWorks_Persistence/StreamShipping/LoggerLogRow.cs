namespace MetWorks.Persistence.StreamShipping;

public sealed record LoggerLogRow(
    long Id,
    string TimestampUtc,
    string Level,
    string Message,
    string? Exception,
    string? PropertiesJson,
    string? InstallationId);
