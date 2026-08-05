using System.Globalization;
using MetWorks.Data.Sqlite;

namespace MetWorks.Persistence.Rollups;

internal sealed class RollupWatermarkStore(string installationId)
{
    const string TableName = "rollup_state";

    readonly string _installationId = !string.IsNullOrWhiteSpace(installationId)
        ? installationId
        : throw new ArgumentException("Installation id is required.", nameof(installationId));

    internal async Task<long?> TryGetWatermarkAsync(
        ISqliteSession session,
        string sourceTable,
        int bucketWidthSeconds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (string.IsNullOrWhiteSpace(sourceTable))
            throw new ArgumentException("Source table is required.", nameof(sourceTable));

        if (bucketWidthSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(bucketWidthSeconds));

        var watermark = await session.ScalarAsync<long?>(
            $"SELECT watermark_device_epoch FROM {TableName} WHERE installation_id = $installation_id AND source_table = $source_table AND bucket_width_seconds = $bucket_width_seconds;",
            [
                new DbParam("$installation_id", _installationId),
                new DbParam("$source_table", sourceTable),
                new DbParam("$bucket_width_seconds", bucketWidthSeconds)
            ],
            cancellationToken).ConfigureAwait(false);

        return watermark;
    }

    internal Task UpsertWatermarkAsync(
        ISqliteSession session,
        string sourceTable,
        int bucketWidthSeconds,
        long watermarkDeviceEpoch,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (string.IsNullOrWhiteSpace(sourceTable))
            throw new ArgumentException("Source table is required.", nameof(sourceTable));

        if (bucketWidthSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(bucketWidthSeconds));

        return session.ExecuteAsync(
            $"""
            INSERT INTO {TableName}(installation_id, source_table, bucket_width_seconds, watermark_device_epoch)
            VALUES ($installation_id, $source_table, $bucket_width_seconds, $watermark_device_epoch)
            ON CONFLICT(installation_id, source_table, bucket_width_seconds)
            DO UPDATE SET
                watermark_device_epoch = excluded.watermark_device_epoch,
                updated_utc_timestampz = strftime('%Y-%m-%dT%H:%M:%fZ','now');
            """,
            [
                new DbParam("$installation_id", _installationId),
                new DbParam("$source_table", sourceTable),
                new DbParam("$bucket_width_seconds", bucketWidthSeconds),
                new DbParam("$watermark_device_epoch", watermarkDeviceEpoch)
            ],
            cancellationToken);
    }
}
