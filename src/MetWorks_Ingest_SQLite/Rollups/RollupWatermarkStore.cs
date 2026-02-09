namespace MetWorks.Ingest.SQLite.Rollups;
internal sealed class RollupWatermarkStore
{
    const string TableName = "rollup_state";

    readonly string _installationId;

    internal RollupWatermarkStore(string installationId)
    {
        if (string.IsNullOrWhiteSpace(installationId))
            throw new ArgumentException("Installation id is required.", nameof(installationId));

        _installationId = installationId;
    }

    internal async Task<long?> TryGetWatermarkAsync(
        SqliteConnection connection,
        string sourceTable,
        int bucketWidthSeconds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);

        if (string.IsNullOrWhiteSpace(sourceTable))
            throw new ArgumentException("Source table is required.", nameof(sourceTable));

        if (bucketWidthSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(bucketWidthSeconds));

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT watermark_device_epoch FROM {TableName} WHERE installation_id = $installation_id AND source_table = $source_table AND bucket_width_seconds = $bucket_width_seconds;";
        cmd.Parameters.AddWithValue("$installation_id", _installationId);
        cmd.Parameters.AddWithValue("$source_table", sourceTable);
        cmd.Parameters.AddWithValue("$bucket_width_seconds", bucketWidthSeconds);

        var scalar = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        if (scalar is null || scalar is DBNull)
            return null;

        return Convert.ToInt64(scalar, CultureInfo.InvariantCulture);
    }

    internal async Task UpsertWatermarkAsync(
        SqliteConnection connection,
        string sourceTable,
        int bucketWidthSeconds,
        long watermarkDeviceEpoch,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);

        if (string.IsNullOrWhiteSpace(sourceTable))
            throw new ArgumentException("Source table is required.", nameof(sourceTable));

        if (bucketWidthSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(bucketWidthSeconds));

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $@"
INSERT INTO {TableName}(installation_id, source_table, bucket_width_seconds, watermark_device_epoch)
VALUES ($installation_id, $source_table, $bucket_width_seconds, $watermark_device_epoch)
ON CONFLICT(installation_id, source_table, bucket_width_seconds)
DO UPDATE SET
    watermark_device_epoch = excluded.watermark_device_epoch,
    updated_utc_timestampz = strftime('%Y-%m-%dT%H:%M:%fZ','now');";

        cmd.Parameters.AddWithValue("$installation_id", _installationId);
        cmd.Parameters.AddWithValue("$source_table", sourceTable);
        cmd.Parameters.AddWithValue("$bucket_width_seconds", bucketWidthSeconds);
        cmd.Parameters.AddWithValue("$watermark_device_epoch", watermarkDeviceEpoch);

        _ = await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
