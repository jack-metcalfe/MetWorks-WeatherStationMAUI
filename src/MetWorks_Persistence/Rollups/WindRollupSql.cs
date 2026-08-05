namespace MetWorks.Persistence.Rollups;

internal static class WindRollupSql
{
    internal const string SourceTableName = "wind";

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
    sample_count,
    wind_speed_avg,
    wind_speed_min,
    wind_speed_max,
    wind_direction_prevailing_deg
)
SELECT
    w.installation_id,
    (w.device_received_utc_timestamp_epoch / {bucketWidthSeconds}) * {bucketWidthSeconds} AS bucket_start_epoch,
    COUNT(1) AS sample_count,

    AVG(w.wind_speed) AS wind_speed_avg,
    MIN(w.wind_speed) AS wind_speed_min,
    MAX(w.wind_speed) AS wind_speed_max,

    -- Circular mean of angles (vector average). Uses degrees -> radians.
    -- Note: SQLite trig functions are available in modern SQLite builds; if absent, this will fail at runtime.
    (
        (atan2(
            AVG(sin(w.wind_direction * (pi() / 180.0))),
            AVG(cos(w.wind_direction * (pi() / 180.0)))
        ) * 180.0 / pi() + 360.0) % 360.0
    ) AS wind_direction_prevailing_deg
FROM wind w
WHERE w.installation_id = $installation_id
  AND w.device_received_utc_timestamp_epoch >= $range_start_epoch
  AND w.device_received_utc_timestamp_epoch < $range_end_epoch
GROUP BY
    w.installation_id,
    bucket_start_epoch
ON CONFLICT(installation_id, bucket_start_epoch)
DO UPDATE SET
    sample_count = excluded.sample_count,
    wind_speed_avg = excluded.wind_speed_avg,
    wind_speed_min = excluded.wind_speed_min,
    wind_speed_max = excluded.wind_speed_max,
    wind_direction_prevailing_deg = excluded.wind_direction_prevailing_deg;";
    }
}
