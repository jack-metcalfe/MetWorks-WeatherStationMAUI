namespace MetWorks.Persistence.Rollups;

internal static class LightningRollupSql
{
    internal const string SourceTableName = "lightning";

    internal static string BuildUpsertRollupSql(string rollupTableName, int bucketWidthSeconds)
    {
        if (string.IsNullOrWhiteSpace(rollupTableName))
            throw new ArgumentException("Rollup table name is required.", nameof(rollupTableName));

        if (bucketWidthSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(bucketWidthSeconds));

        return $@"
INSERT INTO {rollupTableName}
(
    installation_id,
    bucket_start_epoch,
    strike_count,
    strike_distance_min,
    strike_distance_avg,
    relative_energy_max,
    relative_energy_avg
)
SELECT
    l.installation_id,
    (l.device_received_utc_timestamp_epoch / {bucketWidthSeconds}) * {bucketWidthSeconds} AS bucket_start_epoch,
    COUNT(1) AS strike_count,

    MIN(l.lightning_strike_distance_at_timestamp) AS strike_distance_min,
    AVG(l.lightning_strike_distance_at_timestamp) AS strike_distance_avg,

    MAX(l.relative_energy_content_at_timestamp) AS relative_energy_max,
    AVG(l.relative_energy_content_at_timestamp) AS relative_energy_avg
FROM lightning l
WHERE l.installation_id = $installation_id
  AND l.device_received_utc_timestamp_epoch >= $range_start_epoch
  AND l.device_received_utc_timestamp_epoch < $range_end_epoch
GROUP BY
    l.installation_id,
    bucket_start_epoch
ON CONFLICT(installation_id, bucket_start_epoch)
DO UPDATE SET
    strike_count = excluded.strike_count,
    strike_distance_min = excluded.strike_distance_min,
    strike_distance_avg = excluded.strike_distance_avg,
    relative_energy_max = excluded.relative_energy_max,
    relative_energy_avg = excluded.relative_energy_avg;";
    }
}
