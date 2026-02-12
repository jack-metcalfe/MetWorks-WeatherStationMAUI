namespace MetWorks.Ingest.SQLite.Shipping;
internal sealed record ShipperStateSnapshot(
    string InstallationId,
    string Source,
    long? LastShippedRowId,
    long? LastAckedRowId,
    long? LastLossyDeletedRowId,
    long LossyDeletedRowCount,
    DateTime? LastLossyDeleteUtc
);

internal sealed class ShipperStateStore
{
    const string TableName = "shipper_state";

    readonly string _installationId;

    internal ShipperStateStore(string installationId)
    {
        if (string.IsNullOrWhiteSpace(installationId))
            throw new ArgumentException("Installation id is required.", nameof(installationId));

        _installationId = installationId;
    }

    internal async Task EnsureTableAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $@"
CREATE TABLE IF NOT EXISTS {TableName}
(
    installation_id TEXT NOT NULL,
    source TEXT NOT NULL,
    last_shipped_rowid INTEGER NULL,
    last_acked_rowid INTEGER NULL,
    last_lossy_deleted_rowid INTEGER NULL,
    lossy_deleted_row_count INTEGER NOT NULL DEFAULT 0,
    last_lossy_delete_utc TEXT NULL,
    updated_utc_timestampz TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now')),
    PRIMARY KEY (installation_id, source)
);

CREATE INDEX IF NOT EXISTS idx_{TableName}_source ON {TableName}(source);
";
        _ = await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    internal async Task<ShipperStateSnapshot?> TryGetAsync(SqliteConnection connection, string source, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);

        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Source is required.", nameof(source));

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $@"
SELECT
    last_shipped_rowid,
    last_acked_rowid,
    last_lossy_deleted_rowid,
    lossy_deleted_row_count,
    last_lossy_delete_utc
FROM {TableName}
WHERE installation_id = $installation_id AND source = $source;";

        cmd.Parameters.AddWithValue("$installation_id", _installationId);
        cmd.Parameters.AddWithValue("$source", source);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            return null;

        long? lastShipped = reader.IsDBNull(0) ? null : reader.GetInt64(0);
        long? lastAcked = reader.IsDBNull(1) ? null : reader.GetInt64(1);
        long? lastLossyDeleted = reader.IsDBNull(2) ? null : reader.GetInt64(2);
        var lossyCount = reader.IsDBNull(3) ? 0L : reader.GetInt64(3);
        var lastLossyDeleteUtcRaw = reader.IsDBNull(4) ? null : reader.GetString(4);

        DateTime? lastLossyDeleteUtc = null;
        if (!string.IsNullOrWhiteSpace(lastLossyDeleteUtcRaw) &&
            DateTime.TryParse(lastLossyDeleteUtcRaw, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed))
        {
            lastLossyDeleteUtc = parsed;
        }

        return new ShipperStateSnapshot(
            InstallationId: _installationId,
            Source: source,
            LastShippedRowId: lastShipped,
            LastAckedRowId: lastAcked,
            LastLossyDeletedRowId: lastLossyDeleted,
            LossyDeletedRowCount: lossyCount,
            LastLossyDeleteUtc: lastLossyDeleteUtc
        );
    }

    internal async Task UpsertShippingProgressAsync(
        SqliteConnection connection,
        string source,
        long? lastShippedRowId,
        long? lastAckedRowId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);

        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Source is required.", nameof(source));

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $@"
INSERT INTO {TableName}(installation_id, source, last_shipped_rowid, last_acked_rowid)
VALUES ($installation_id, $source, $last_shipped_rowid, $last_acked_rowid)
ON CONFLICT(installation_id, source)
DO UPDATE SET
    last_shipped_rowid = excluded.last_shipped_rowid,
    last_acked_rowid = excluded.last_acked_rowid,
    updated_utc_timestampz = strftime('%Y-%m-%dT%H:%M:%fZ','now');";

        cmd.Parameters.AddWithValue("$installation_id", _installationId);
        cmd.Parameters.AddWithValue("$source", source);
        cmd.Parameters.AddWithValue("$last_shipped_rowid", lastShippedRowId is null ? DBNull.Value : lastShippedRowId);
        cmd.Parameters.AddWithValue("$last_acked_rowid", lastAckedRowId is null ? DBNull.Value : lastAckedRowId);

        _ = await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    internal async Task RecordLossyDeletionAsync(
        SqliteConnection connection,
        string source,
        long deletedThroughRowId,
        long deletedRowCount,
        DateTime deletionUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);

        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Source is required.", nameof(source));

        if (deletedThroughRowId <= 0)
            throw new ArgumentOutOfRangeException(nameof(deletedThroughRowId));

        if (deletedRowCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(deletedRowCount));

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $@"
INSERT INTO {TableName}(
    installation_id,
    source,
    last_lossy_deleted_rowid,
    lossy_deleted_row_count,
    last_lossy_delete_utc
)
VALUES (
    $installation_id,
    $source,
    $deleted_through_rowid,
    $deleted_row_count,
    $last_lossy_delete_utc
)
ON CONFLICT(installation_id, source)
DO UPDATE SET
    last_lossy_deleted_rowid = max(excluded.last_lossy_deleted_rowid, {TableName}.last_lossy_deleted_rowid),
    lossy_deleted_row_count = {TableName}.lossy_deleted_row_count + excluded.lossy_deleted_row_count,
    last_lossy_delete_utc = excluded.last_lossy_delete_utc,
    updated_utc_timestampz = strftime('%Y-%m-%dT%H:%M:%fZ','now');";

        cmd.Parameters.AddWithValue("$installation_id", _installationId);
        cmd.Parameters.AddWithValue("$source", source);
        cmd.Parameters.AddWithValue("$deleted_through_rowid", deletedThroughRowId);
        cmd.Parameters.AddWithValue("$deleted_row_count", deletedRowCount);
        cmd.Parameters.AddWithValue("$last_lossy_delete_utc", deletionUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));

        _ = await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
