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

    -- Prevailing direction uses the circular mean of angles (vector-averaged direction).
    -- This remains meaningful even when wind swings around within the bucket.
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
