CREATE TABLE IF NOT EXISTS wind
(
    id TEXT PRIMARY KEY -- COMB-style GUID supplied by SQLite sync or C# writer
    -- Timestamps (all UTC)
    , application_received_utc_timestampz TEXT NOT NULL
    , database_received_utc_timestampz TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now'))
    -- JSON payloads
    , json_document_original TEXT NOT NULL
    , json_document_jsonb TEXT GENERATED ALWAYS AS (json_document_original) STORED
    -- JSON derived fields
    , device_received_utc_timestamp_epoch INTEGER GENERATED ALWAYS AS (CAST(json_extract(json_document_original, '$.ob[0]') AS INTEGER)) STORED
    , device_received_utc_timestampz TEXT GENERATED ALWAYS AS (datetime(CAST(json_extract(json_document_original, '$.ob[0]') AS INTEGER), 'unixepoch')) STORED
    -- Type-specific JSON derived fields
    , wind_speed REAL GENERATED ALWAYS AS (CAST(json_extract(json_document_original, '$.ob[1]') AS REAL)) STORED
    , wind_direction INTEGER GENERATED ALWAYS AS (CAST(json_extract(json_document_original, '$.ob[2]') AS INTEGER)) STORED
    -- Per-installation identifier to distinguish records from different app installs
    , installation_id TEXT NULL
);

CREATE INDEX IF NOT EXISTS idx_wind_device_received_utc_timestampz ON wind
(
    device_received_utc_timestampz
);

CREATE INDEX IF NOT EXISTS idx_wind_installation_id ON wind
(
    installation_id
);
