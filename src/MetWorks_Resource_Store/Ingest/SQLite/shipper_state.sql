CREATE TABLE IF NOT EXISTS shipper_state
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

CREATE INDEX IF NOT EXISTS idx_shipper_state_source ON shipper_state
(
    source
);
