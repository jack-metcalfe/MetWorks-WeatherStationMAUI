namespace MetWorks.Persistence.Logging;

public sealed record LoggerSqliteLogEvent(
    string Id,
    DateTime TimestampUtc,
    string Level,
    string Message,
    string? Exception,
    string PropertiesJson,
    string? InstallationId);
