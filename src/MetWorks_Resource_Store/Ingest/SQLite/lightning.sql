CREATE TABLE IF NOT EXISTS lightning
(
    id TEXT PRIMARY KEY -- COMB-style GUID supplied by SQLite sync or C# writer
    -- Timestamps (all UTC)
    , application_received_utc_timestampz INTEGER NOT NULL
    , database_received_utc_timestampz TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now'))
    -- JSON payloads
    , json_document_original TEXT NOT NULL
    , json_document_jsonb TEXT GENERATED ALWAYS AS (json_document_original) STORED
    -- JSON derived fields
    , device_received_utc_timestamp_epoch INTEGER GENERATED ALWAYS AS (CAST(json_extract(json_document_original, '$.evt[0]') AS INTEGER)) STORED
    , device_received_utc_timestampz TEXT GENERATED ALWAYS AS (datetime(CAST(json_extract(json_document_original, '$.evt[0]') AS INTEGER), 'unixepoch')) STORED
    -- Type specific JSON derived fields
    , lightning_strike_distance_at_timestamp INTEGER GENERATED ALWAYS AS (CAST(json_extract(json_document_original, '$.evt[1]') AS INTEGER)) STORED
    , relative_energy_content_at_timestamp INTEGER GENERATED ALWAYS AS (CAST(json_extract(json_document_original, '$.evt[2]') AS INTEGER)) STORED
    -- Per-installation identifier to distinguish records from different app installs
    , installation_id TEXT NULL
);

CREATE INDEX IF NOT EXISTS idx_lightning_device_received_utc_timestamp_epoch ON lightning
(
    device_received_utc_timestamp_epoch
);

CREATE INDEX IF NOT EXISTS idx_lightning_installation_id ON lightning
(
    installation_id
);

CREATE INDEX IF NOT EXISTS idx_lightning_installation_id_device_received_utc_timestamp_epoch ON lightning
(
    installation_id,
    device_received_utc_timestamp_epoch
);
