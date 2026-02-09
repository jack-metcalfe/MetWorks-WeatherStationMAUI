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
