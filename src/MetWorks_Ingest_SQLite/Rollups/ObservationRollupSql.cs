namespace MetWorks.Ingest.SQLite.Rollups;
internal static class ObservationRollupSql
{
    internal const string SourceTableName = "observation";

    internal static string BuildUpsertRollupSql(string rollupTableName, int bucketWidthSeconds)
    {
        if (string.IsNullOrWhiteSpace(rollupTableName))
            throw new ArgumentException("Rollup table name is required.", nameof(rollupTableName));

        if (bucketWidthSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(bucketWidthSeconds));

        // Bucketing uses integer division on device epoch.
        // Each group yields one row per (installation_id, bucket_start_epoch).
        return $@"
INSERT INTO {rollupTableName}
(
    installation_id,
    bucket_start_epoch,
    sample_count,
    air_temperature_avg, air_temperature_min, air_temperature_max,
    station_pressure_avg, station_pressure_min, station_pressure_max,
    relative_humidity_avg, relative_humidity_min, relative_humidity_max,
    illuminance_avg, illuminance_min, illuminance_max,
    uv_index_avg, uv_index_min, uv_index_max,
    solar_radiation_avg, solar_radiation_min, solar_radiation_max,
    rain_accumulation_sum,
    battery_level_avg, battery_level_min, battery_level_max,
    reporting_interval_mode
)
SELECT
    o.installation_id,
    (o.device_received_utc_timestamp_epoch / {bucketWidthSeconds}) * {bucketWidthSeconds} AS bucket_start_epoch,
    COUNT(1) AS sample_count,

    AVG(o.air_temperature_at_timestamp) AS air_temperature_avg,
    MIN(o.air_temperature_at_timestamp) AS air_temperature_min,
    MAX(o.air_temperature_at_timestamp) AS air_temperature_max,

    AVG(o.station_pressure_at_timestamp) AS station_pressure_avg,
    MIN(o.station_pressure_at_timestamp) AS station_pressure_min,
    MAX(o.station_pressure_at_timestamp) AS station_pressure_max,

    AVG(o.relative_humidity_at_timestamp) AS relative_humidity_avg,
    MIN(o.relative_humidity_at_timestamp) AS relative_humidity_min,
    MAX(o.relative_humidity_at_timestamp) AS relative_humidity_max,

    AVG(o.illuminance_at_timestamp) AS illuminance_avg,
    MIN(o.illuminance_at_timestamp) AS illuminance_min,
    MAX(o.illuminance_at_timestamp) AS illuminance_max,

    AVG(o.uv_index_at_timestamp) AS uv_index_avg,
    MIN(o.uv_index_at_timestamp) AS uv_index_min,
    MAX(o.uv_index_at_timestamp) AS uv_index_max,

    AVG(o.solar_radiation_at_timestamp) AS solar_radiation_avg,
    MIN(o.solar_radiation_at_timestamp) AS solar_radiation_min,
    MAX(o.solar_radiation_at_timestamp) AS solar_radiation_max,

    SUM(o.rain_accumulation_in_reporting_interval) AS rain_accumulation_sum,

    AVG(o.battery_level_at_timestamp) AS battery_level_avg,
    MIN(o.battery_level_at_timestamp) AS battery_level_min,
    MAX(o.battery_level_at_timestamp) AS battery_level_max,

    (
        SELECT reporting_interval
        FROM observation o2
        WHERE o2.installation_id = o.installation_id
          AND (o2.device_received_utc_timestamp_epoch / {bucketWidthSeconds}) * {bucketWidthSeconds} = (o.device_received_utc_timestamp_epoch / {bucketWidthSeconds}) * {bucketWidthSeconds}
        GROUP BY reporting_interval
        ORDER BY COUNT(1) DESC
        LIMIT 1
    ) AS reporting_interval_mode
FROM observation o
WHERE o.installation_id = $installation_id
  AND o.device_received_utc_timestamp_epoch >= $range_start_epoch
  AND o.device_received_utc_timestamp_epoch < $range_end_epoch
GROUP BY
    o.installation_id,
    bucket_start_epoch
ON CONFLICT(installation_id, bucket_start_epoch)
DO UPDATE SET
    sample_count = excluded.sample_count,
    air_temperature_avg = excluded.air_temperature_avg,
    air_temperature_min = excluded.air_temperature_min,
    air_temperature_max = excluded.air_temperature_max,
    station_pressure_avg = excluded.station_pressure_avg,
    station_pressure_min = excluded.station_pressure_min,
    station_pressure_max = excluded.station_pressure_max,
    relative_humidity_avg = excluded.relative_humidity_avg,
    relative_humidity_min = excluded.relative_humidity_min,
    relative_humidity_max = excluded.relative_humidity_max,
    illuminance_avg = excluded.illuminance_avg,
    illuminance_min = excluded.illuminance_min,
    illuminance_max = excluded.illuminance_max,
    uv_index_avg = excluded.uv_index_avg,
    uv_index_min = excluded.uv_index_min,
    uv_index_max = excluded.uv_index_max,
    solar_radiation_avg = excluded.solar_radiation_avg,
    solar_radiation_min = excluded.solar_radiation_min,
    solar_radiation_max = excluded.solar_radiation_max,
    rain_accumulation_sum = excluded.rain_accumulation_sum,
    battery_level_avg = excluded.battery_level_avg,
    battery_level_min = excluded.battery_level_min,
    battery_level_max = excluded.battery_level_max,
    reporting_interval_mode = excluded.reporting_interval_mode;";
    }
}
