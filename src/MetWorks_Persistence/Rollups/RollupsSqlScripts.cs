using MetWorks.Data.Sqlite;

namespace MetWorks.Persistence.Rollups;

public static class RollupsSqlScripts
{
    public static IReadOnlyList<SqlScript> GetAll()
    {
        return new[]
        {
            new SqlScript("rollup_state",
                """
                CREATE TABLE IF NOT EXISTS rollup_state
                (
                    installation_id TEXT NOT NULL,
                    source_table TEXT NOT NULL,
                    bucket_width_seconds INTEGER NOT NULL,
                    watermark_device_epoch INTEGER NOT NULL,
                    updated_utc_timestampz TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now')),
                    PRIMARY KEY (installation_id, source_table, bucket_width_seconds)
                );

                CREATE INDEX IF NOT EXISTS idx_rollup_state_source_table ON rollup_state
                (
                    source_table
                );
                """),
            new SqlScript("observation_rollup_1h",
                """
                CREATE TABLE IF NOT EXISTS observation_rollup_1h
                (
                    installation_id TEXT NOT NULL,
                    bucket_start_epoch INTEGER NOT NULL,

                    -- Note: aggregates are over raw observation rows whose device_received_utc_timestamp_epoch falls within
                    -- [bucket_start_epoch, bucket_start_epoch + 3600).

                    sample_count INTEGER NOT NULL,

                    air_temperature_avg REAL NULL,
                    air_temperature_min REAL NULL,
                    air_temperature_max REAL NULL,

                    station_pressure_avg REAL NULL,
                    station_pressure_min REAL NULL,
                    station_pressure_max REAL NULL,

                    relative_humidity_avg REAL NULL,
                    relative_humidity_min REAL NULL,
                    relative_humidity_max REAL NULL,

                    illuminance_avg REAL NULL,
                    illuminance_min REAL NULL,
                    illuminance_max REAL NULL,

                    uv_index_avg REAL NULL,
                    uv_index_min REAL NULL,
                    uv_index_max REAL NULL,

                    solar_radiation_avg REAL NULL,
                    solar_radiation_min REAL NULL,
                    solar_radiation_max REAL NULL,

                    rain_accumulation_sum REAL NULL,

                    battery_level_avg REAL NULL,
                    battery_level_min REAL NULL,
                    battery_level_max REAL NULL,

                    reporting_interval_mode INTEGER NULL,

                    PRIMARY KEY (installation_id, bucket_start_epoch)
                );

                CREATE INDEX IF NOT EXISTS idx_observation_rollup_1h_bucket_start_epoch ON observation_rollup_1h
                (
                    bucket_start_epoch
                );

                CREATE INDEX IF NOT EXISTS idx_observation_rollup_1h_installation_id_bucket_start_epoch ON observation_rollup_1h
                (
                    installation_id,
                    bucket_start_epoch
                );
                """),
            new SqlScript("observation_rollup_1d",
                """
                CREATE TABLE IF NOT EXISTS observation_rollup_1d
                (
                    installation_id TEXT NOT NULL,
                    bucket_start_epoch INTEGER NOT NULL,

                    -- Note: aggregates are over raw observation rows whose device_received_utc_timestamp_epoch falls within
                    -- [bucket_start_epoch, bucket_start_epoch + 86400).

                    sample_count INTEGER NOT NULL,

                    air_temperature_avg REAL NULL,
                    air_temperature_min REAL NULL,
                    air_temperature_max REAL NULL,

                    station_pressure_avg REAL NULL,
                    station_pressure_min REAL NULL,
                    station_pressure_max REAL NULL,

                    relative_humidity_avg REAL NULL,
                    relative_humidity_min REAL NULL,
                    relative_humidity_max REAL NULL,

                    illuminance_avg REAL NULL,
                    illuminance_min REAL NULL,
                    illuminance_max REAL NULL,

                    uv_index_avg REAL NULL,
                    uv_index_min REAL NULL,
                    uv_index_max REAL NULL,

                    solar_radiation_avg REAL NULL,
                    solar_radiation_min REAL NULL,
                    solar_radiation_max REAL NULL,

                    rain_accumulation_sum REAL NULL,

                    battery_level_avg REAL NULL,
                    battery_level_min REAL NULL,
                    battery_level_max REAL NULL,

                    reporting_interval_mode INTEGER NULL,

                    PRIMARY KEY (installation_id, bucket_start_epoch)
                );

                CREATE INDEX IF NOT EXISTS idx_observation_rollup_1d_bucket_start_epoch ON observation_rollup_1d
                (
                    bucket_start_epoch
                );

                CREATE INDEX IF NOT EXISTS idx_observation_rollup_1d_installation_id_bucket_start_epoch ON observation_rollup_1d
                (
                    installation_id,
                    bucket_start_epoch
                );
                """),
            new SqlScript("wind_rollup_1h",
                """
                CREATE TABLE IF NOT EXISTS wind_rollup_1h
                (
                    installation_id TEXT NOT NULL,
                    bucket_start_epoch INTEGER NOT NULL,

                    -- Note: aggregates are over raw wind rows whose device_received_utc_timestamp_epoch falls within
                    -- [bucket_start_epoch, bucket_start_epoch + 3600).

                    sample_count INTEGER NOT NULL,

                    wind_speed_avg REAL NULL,
                    wind_speed_min REAL NULL,
                    wind_speed_max REAL NULL,

                    -- Prevailing direction is intended to be computed as a circular mean (vector average).
                    wind_direction_prevailing_deg REAL NULL,

                    PRIMARY KEY (installation_id, bucket_start_epoch)
                );

                CREATE INDEX IF NOT EXISTS idx_wind_rollup_1h_bucket_start_epoch ON wind_rollup_1h
                (
                    bucket_start_epoch
                );

                CREATE INDEX IF NOT EXISTS idx_wind_rollup_1h_installation_id_bucket_start_epoch ON wind_rollup_1h
                (
                    installation_id,
                    bucket_start_epoch
                );
                """),
            new SqlScript("wind_rollup_1d",
                """
                CREATE TABLE IF NOT EXISTS wind_rollup_1d
                (
                    installation_id TEXT NOT NULL,
                    bucket_start_epoch INTEGER NOT NULL,

                    -- Note: aggregates are over raw wind rows whose device_received_utc_timestamp_epoch falls within
                    -- [bucket_start_epoch, bucket_start_epoch + 86400).

                    sample_count INTEGER NOT NULL,

                    wind_speed_avg REAL NULL,
                    wind_speed_min REAL NULL,
                    wind_speed_max REAL NULL,

                    -- Prevailing direction is intended to be computed as a circular mean (vector average).
                    wind_direction_prevailing_deg REAL NULL,

                    PRIMARY KEY (installation_id, bucket_start_epoch)
                );

                CREATE INDEX IF NOT EXISTS idx_wind_rollup_1d_bucket_start_epoch ON wind_rollup_1d
                (
                    bucket_start_epoch
                );

                CREATE INDEX IF NOT EXISTS idx_wind_rollup_1d_installation_id_bucket_start_epoch ON wind_rollup_1d
                (
                    installation_id,
                    bucket_start_epoch
                );
                """),
            new SqlScript("lightning_rollup_1d",
                """
                CREATE TABLE IF NOT EXISTS lightning_rollup_1d
                (
                    installation_id TEXT NOT NULL,
                    bucket_start_epoch INTEGER NOT NULL,

                    -- Note: aggregates are over raw lightning rows whose device_received_utc_timestamp_epoch falls within
                    -- [bucket_start_epoch, bucket_start_epoch + 86400).

                    strike_count INTEGER NOT NULL,

                    strike_distance_min INTEGER NULL,
                    strike_distance_avg REAL NULL,

                    relative_energy_max INTEGER NULL,
                    relative_energy_avg REAL NULL,

                    PRIMARY KEY (installation_id, bucket_start_epoch)
                );

                CREATE INDEX IF NOT EXISTS idx_lightning_rollup_1d_bucket_start_epoch ON lightning_rollup_1d
                (
                    bucket_start_epoch
                );

                CREATE INDEX IF NOT EXISTS idx_lightning_rollup_1d_installation_id_bucket_start_epoch ON lightning_rollup_1d
                (
                    installation_id,
                    bucket_start_epoch
                );
                """),
        };
    }
}
