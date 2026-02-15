using System.Text.RegularExpressions;
using MetWorks.Data.Sqlite;

namespace MetWorks.Persistence.Logging;

public sealed class LoggerSqliteRepository : ILoggerSqliteRepository
{
    static readonly Regex ValidIdentifier = new(@"^[A-Za-z0-9_]+$", RegexOptions.Compiled);

    ISqliteDatabase? _sqliteDatabase;
    int _isInitialized;

    public LoggerSqliteRepository() { }

    public Task<bool> InitializeAsync(ISqliteDatabase sqliteDatabase, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sqliteDatabase);

        if (Interlocked.Exchange(ref _isInitialized, 1) == 1)
            throw new InvalidOperationException($"{nameof(LoggerSqliteRepository)} is already initialized.");

        if (cancellationToken.IsCancellationRequested) return Task.FromResult(false);

        _sqliteDatabase = sqliteDatabase;
        return Task.FromResult(true);
    }

    public async Task InsertAsync(string table, LoggerSqliteLogEvent logEvent, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(table))
            throw new ArgumentException("Table is required.", nameof(table));

        if (!ValidIdentifier.IsMatch(table))
            throw new ArgumentException(
                "Table name contains invalid characters. Only letters, digits and underscore are allowed.",
                nameof(table));

        ArgumentNullException.ThrowIfNull(logEvent);

        var sql = $"""
 INSERT INTO "{table}" (timestamp_utc, level, message, exception, properties, installation_id)
 VALUES ($ts, $level, $message, $exception, json($properties), $installation_id);
 """;

        if (_isInitialized != 1)
            throw new InvalidOperationException($"{nameof(LoggerSqliteRepository)} is not initialized.");

        var sqliteDatabase = _sqliteDatabase ?? throw new InvalidOperationException($"{nameof(LoggerSqliteRepository)} is not initialized.");

        await using var session = await sqliteDatabase.OpenSessionAsync(cancellationToken).ConfigureAwait(false);

        _ = await session.ExecuteAsync(
            sql,
            [
                new DbParam("$ts", logEvent.TimestampUtc.ToString("O")),
                new DbParam("$level", logEvent.Level),
                new DbParam("$message", logEvent.Message),
                new DbParam("$exception", logEvent.Exception is null ? DBNull.Value : logEvent.Exception),
                new DbParam("$properties", logEvent.PropertiesJson),
                new DbParam("$installation_id", logEvent.InstallationId is null ? DBNull.Value : logEvent.InstallationId),
            ],
            cancellationToken).ConfigureAwait(false);
    }
}
