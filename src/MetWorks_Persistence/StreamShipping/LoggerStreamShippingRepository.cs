using System.Globalization;
using MetWorks.Data.Sqlite;

namespace MetWorks.Persistence.StreamShipping;

public sealed class LoggerStreamShippingRepository : ILoggerStreamShippingRepository
{
    ISqliteDatabase? _sqliteDatabase;
    IStreamShippingRepository? _streamShippingRepository;

    public LoggerStreamShippingRepository()
    {
    }

    public Task<bool> InitializeAsync(
        ISqliteDatabase sqliteDatabase,
        IStreamShippingRepository streamShippingRepository,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sqliteDatabase);
        ArgumentNullException.ThrowIfNull(streamShippingRepository);

        _sqliteDatabase = sqliteDatabase;
        _streamShippingRepository = streamShippingRepository;
        return Task.FromResult(true);
    }

    ISqliteDatabase GetInitializedSqliteDatabase() =>
        _sqliteDatabase ?? throw new InvalidOperationException($"{nameof(LoggerStreamShippingRepository)} has not been initialized.");

    IStreamShippingRepository GetInitializedStreamShippingRepository() =>
        _streamShippingRepository ?? throw new InvalidOperationException($"{nameof(LoggerStreamShippingRepository)} has not been initialized.");

    public async Task<IReadOnlyList<LoggerLogRow>> ReadLoggerBatchAsync(
        string table,
        long lastAckedRowId,
        int maxRows,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(table))
            throw new ArgumentException("Table is required.", nameof(table));

        if (lastAckedRowId < 0)
            throw new ArgumentOutOfRangeException(nameof(lastAckedRowId));

        if (maxRows <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxRows));

        var sql = $"""
SELECT rowid, id, timestamp_utc, level, message, exception, properties, installation_id
FROM "{table}"
WHERE rowid > $last_acked_rowid
ORDER BY rowid
LIMIT $limit;
""";

        await using var session = await GetInitializedSqliteDatabase().OpenSessionAsync(cancellationToken).ConfigureAwait(false);

        var rows = await session.QueryAsync(
            sql,
            [
                new DbParam("$last_acked_rowid", lastAckedRowId),
                new DbParam("$limit", maxRows),
            ],
            row =>
            {
                _ = row.TryGetInt64("rowid", out var rowId);
                _ = row.TryGetString("id", out var id);
                _ = row.TryGetString("timestamp_utc", out var ts);
                _ = row.TryGetString("level", out var level);
                _ = row.TryGetString("message", out var message);
                _ = row.TryGetString("exception", out var exception);
                _ = row.TryGetString("properties", out var properties);
                _ = row.TryGetString("installation_id", out var installationId);

                return new LoggerLogRow(
                    RowId: rowId,
                    Id: id ?? string.Empty,
                    TimestampUtc: ts ?? string.Empty,
                    Level: level ?? string.Empty,
                    Message: message ?? string.Empty,
                    Exception: exception,
                    PropertiesJson: properties,
                    InstallationId: installationId);
            },
            cancellationToken).ConfigureAwait(false);

        return rows;
    }

    public async Task<int> PurgeAckedOlderThanAsync(
        string table,
        long ackedUpToRowId,
        DateTime cutoffUtc,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(table))
            throw new ArgumentException("Table is required.", nameof(table));

        if (ackedUpToRowId <= 0)
            return 0;

        var cutoffRaw = cutoffUtc.ToString("O", CultureInfo.InvariantCulture);

        var sql = $"""
DELETE FROM "{table}"
WHERE rowid <= $acked_rowid AND timestamp_utc < $cutoff_ts;
""";

        await using var session = await GetInitializedSqliteDatabase().OpenSessionAsync(cancellationToken).ConfigureAwait(false);

        return await session.ExecuteAsync(
            sql,
            [
                new DbParam("$acked_rowid", ackedUpToRowId),
                new DbParam("$cutoff_ts", cutoffRaw),
            ],
            cancellationToken).ConfigureAwait(false);
    }

    public Task RecordLossyDeletionAsync(
        string table,
        long deletedThroughRowId,
        int deletedRowCount,
        DateTime deletionUtc,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(table))
            throw new ArgumentException("Table is required.", nameof(table));

        return GetInitializedStreamShippingRepository().UpsertShippingProgressAsync(
            table,
            lastShippedRowId: deletedThroughRowId,
            lastAckedRowId: deletedThroughRowId,
            cancellationToken: cancellationToken);
    }
}
