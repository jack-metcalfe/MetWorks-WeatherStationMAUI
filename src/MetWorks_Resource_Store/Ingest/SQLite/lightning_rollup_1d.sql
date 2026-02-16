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
