namespace MetWorks.Persistence.Logging;

public sealed record LoggerSqliteLogEvent(
    DateTime TimestampUtc,
    string Level,
    string Message,
    string? Exception,
    string PropertiesJson,
    string? InstallationId);
