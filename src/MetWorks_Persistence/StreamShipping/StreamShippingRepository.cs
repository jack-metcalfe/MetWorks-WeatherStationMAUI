using System.Globalization;
using MetWorks.Interfaces;
using MetWorks.Common.Utility;
namespace MetWorks.Persistence.StreamShipping;
public sealed class StreamShippingRepository : IStreamShippingRepository
{
    ISqliteDatabase? _sqliteDatabase;
    IInstanceIdentifier? _instanceIdentifier;
    IStreamShippingDatabaseReadiness? _databaseReadiness;
    readonly SemaphoreSlim _readinessGate = new(1, 1);
    bool _schemaEnsured = false;
    bool _isInitialized = false;
    ILogger? _iLogger = null;
    ILogger ILogger => NullPropertyGuard.Get(_isInitialized, _iLogger, nameof(ILogger));
    public StreamShippingRepository()
    {
    }

    public Task<bool> InitializeAsync(
        ILogger iLogger,
        ISqliteDatabase sqliteDatabase,
        IInstanceIdentifier instanceIdentifier,
        IStreamShippingDatabaseReadiness streamShippingDatabaseReadiness,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(iLogger);
        ArgumentNullException.ThrowIfNull(sqliteDatabase);
        ArgumentNullException.ThrowIfNull(instanceIdentifier);
        ArgumentNullException.ThrowIfNull(streamShippingDatabaseReadiness);

        _sqliteDatabase = sqliteDatabase;
        _instanceIdentifier = instanceIdentifier;
        _databaseReadiness = streamShippingDatabaseReadiness;
        _iLogger = iLogger;
        _isInitialized = true;
        return Task.FromResult(true);
    }

    async Task EnsureSchemaAsync(CancellationToken cancellationToken)
    {
        if (_schemaEnsured)
            return;

        await _readinessGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_schemaEnsured)
                return;

            var readiness = _databaseReadiness
                ?? throw new InvalidOperationException("StreamShippingRepository is not initialized (databaseReadiness).");

            await readiness.EnsureReadyAsync(cancellationToken).ConfigureAwait(false);
            _schemaEnsured = true;
        }
        finally
        {
            _readinessGate.Release();
        }
    }
    (ISqliteDatabase SqliteDatabase, IInstanceIdentifier InstanceIdentifier) GetInitialized()
    {
        var sqliteDatabase = _sqliteDatabase;
        if (sqliteDatabase is null)
            throw new InvalidOperationException("StreamShippingRepository is not initialized (sqliteDatabase).");

        var instanceIdentifier = _instanceIdentifier;
        if (instanceIdentifier is null)
            throw new InvalidOperationException("StreamShippingRepository is not initialized (instanceIdentifier).");

        return (sqliteDatabase, instanceIdentifier);
    }

    public async Task<ShipperStateSnapshot?> TryGetStateAsync(string table, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(table))
            throw new ArgumentException("Table is required.", nameof(table));

        await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);

        var (sqliteDatabase, instanceIdentifier) = GetInitialized();

        var installationId = instanceIdentifier.GetOrCreateInstallationId();
        if (string.IsNullOrWhiteSpace(installationId))
            throw new InvalidOperationException("Installation id is required.");

        const string sql = """
SELECT
    last_shipped_rowid,
    last_acked_rowid,
    last_lossy_deleted_rowid,
    lossy_deleted_row_count,
    last_lossy_delete_utc
FROM shipper_state
WHERE installation_id = $installation_id AND [table] = $table;
""";
        try
        {
            await using var session = await sqliteDatabase.OpenSessionAsync(cancellationToken).ConfigureAwait(false);

            var rows = await session.QueryAsync(
                sql,
                [
                    new DbParam("$installation_id", installationId),
                    new DbParam("$table", table),
                ],
                row =>
                {
                    _ = row.TryGetInt64("last_shipped_rowid", out var lastShipped);
                    _ = row.TryGetInt64("last_acked_rowid", out var lastAcked);
                    _ = row.TryGetInt64("last_lossy_deleted_rowid", out var lastLossyDeleted);
                    _ = row.TryGetInt64("lossy_deleted_row_count", out var lossyDeletedRowCount);
                    _ = row.TryGetString("last_lossy_delete_utc", out var lastLossyDeleteUtcRaw);

                    DateTime? lastLossyDeleteUtc = null;
                    if (!string.IsNullOrWhiteSpace(lastLossyDeleteUtcRaw) &&
                        DateTime.TryParse(
                            lastLossyDeleteUtcRaw,
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                            out var parsed))
                    {
                        lastLossyDeleteUtc = parsed;
                    }

                    return new ShipperStateSnapshot(
                        InstallationId: installationId,
                        Table: table,
                        LastShippedRowId: lastShipped,
                        LastAckedRowId: lastAcked,
                        LastLossyDeletedRowId: lastLossyDeleted,
                        LossyDeletedRowCount: lossyDeletedRowCount,
                        LastLossyDeleteUtc: lastLossyDeleteUtc);
                },
                cancellationToken).ConfigureAwait(false);

            return rows.Count == 0 ? null : rows[0];
        }
        catch (Exception exception)
        {
            var message = $"Error reading shipper state for table '{table}' and installation '{installationId}' exception[{exception}].";
            ILogger.Error($"{message} Exception: {exception}");
            throw new InvalidOperationException(message, exception);
        }
    }

    public async Task UpsertShippingProgressAsync(
        string table,
        long? lastShippedRowId,
        long? lastAckedRowId,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(table))
            throw new ArgumentException("Table is required.", nameof(table));

        await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);

        var (sqliteDatabase, instanceIdentifier) = GetInitialized();

        var installationId = instanceIdentifier.GetOrCreateInstallationId();
        if (string.IsNullOrWhiteSpace(installationId))
            throw new InvalidOperationException("Installation id is required.");

        const string sql = """
INSERT INTO shipper_state(installation_id, [table], last_shipped_rowid, last_acked_rowid)
VALUES ($installation_id, $table, $last_shipped_rowid, $last_acked_rowid)
ON CONFLICT(installation_id, [table])
DO UPDATE SET
    last_shipped_rowid = excluded.last_shipped_rowid,
    last_acked_rowid = excluded.last_acked_rowid,
    updated_utc_timestampz = strftime('%Y-%m-%dT%H:%M:%fZ','now');
""";

        try
        {
            await using var session = await sqliteDatabase.OpenSessionAsync(cancellationToken).ConfigureAwait(false);

            _ = await session.ExecuteAsync(
                sql,
                [
                    new DbParam("$installation_id", installationId),
                new DbParam("$table", table),
                new DbParam("$last_shipped_rowid", lastShippedRowId is null ? DBNull.Value : lastShippedRowId.Value),
                new DbParam("$last_acked_rowid", lastAckedRowId is null ? DBNull.Value : lastAckedRowId.Value),
                ],
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            var message = $"Error upserting shipping progress for table '{table}' and installation '{installationId}' exception[{exception}].";
            ILogger.Error($"{message} Exception: {exception}");
            throw new InvalidOperationException(message, exception);
        }
    }

    public async Task<IReadOnlyList<StandardReadingRow>> ReadStandardReadingsBatchAsync(
        string table,
        string installationId,
        long lastAckedRowId,
        int maxRows,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(table))
            throw new ArgumentException("Table is required.", nameof(table));

        if (string.IsNullOrWhiteSpace(installationId))
            throw new ArgumentException("Installation id is required.", nameof(installationId));

        if (lastAckedRowId < 0)
            throw new ArgumentOutOfRangeException(nameof(lastAckedRowId));

        if (maxRows <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxRows));

        await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var (sqliteDatabase, _) = GetInitialized();

            var sql = $"""
SELECT rowid, id, application_received_utc_timestampz, json_document_original
FROM {table}
WHERE installation_id = $installation_id AND rowid > $last_acked_rowid
ORDER BY rowid
LIMIT $limit;
""";

            await using var session = await sqliteDatabase.OpenSessionAsync(cancellationToken).ConfigureAwait(false);

            IReadOnlyList<StandardReadingRow> rows = Array.Empty<StandardReadingRow>();
            rows = await session.QueryAsync(
                sql,
                [
                    new DbParam("$installation_id", installationId),
                    new DbParam("$last_acked_rowid", lastAckedRowId),
                    new DbParam("$limit", maxRows),
                ],
                row =>
                {
                    _ = row.TryGetInt64("rowid", out var rowId);
                    _ = row.TryGetString("id", out var id);
                    var applicationReceivedUtc = 0L;
                    if (row.TryGetString("application_received_utc_timestampz", out var applicationReceivedText)
                        && !string.IsNullOrWhiteSpace(applicationReceivedText)
                        && DateTimeOffset.TryParse(applicationReceivedText, out var applicationReceivedDto))
                    {
                        applicationReceivedUtc = applicationReceivedDto.ToUnixTimeSeconds();
                    }
                    else
                    {
                        try
                        {
                            _ = row.TryGetInt64("application_received_utc_timestampz", out applicationReceivedUtc);
                        }
                        catch (FormatException)
                        {
                            applicationReceivedUtc = 0;
                        }
                    }
                    _ = row.TryGetString("json_document_original", out var json);

                    return new StandardReadingRow(
                        RowId: rowId,
                        Id: id ?? string.Empty,
                        ApplicationReceivedUtcEpoch: applicationReceivedUtc,
                        JsonDocumentOriginal: json ?? string.Empty);
                },
                cancellationToken).ConfigureAwait(false);

            return rows;
        }
        catch (Exception exception)
        {
            var message = $"Error reading standard readings batch from table '{table}' for installation '{installationId}' exception[{exception}].";
            ILogger.Error($"{message} Exception: {exception}");
            throw new InvalidOperationException(message, exception);
        }
    }
}
