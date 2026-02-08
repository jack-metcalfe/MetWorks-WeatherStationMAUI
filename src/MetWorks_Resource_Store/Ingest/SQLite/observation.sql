CREATE TABLE IF NOT EXISTS observation
(
    id TEXT PRIMARY KEY -- COMB-style GUID supplied by SQLite sync or C# writer
    -- Timestamps (all UTC)
    , application_received_utc_timestampz TEXT NOT NULL
    , database_received_utc_timestampz TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now'))
    -- JSON payloads
    , json_document_original TEXT NOT NULL
    , json_document_jsonb TEXT GENERATED ALWAYS AS (json_document_original) STORED
    -- JSON derived fields (obs is a 2D array; only first row parsed)
    , has_multiple_obs_rows INTEGER GENERATED ALWAYS AS (CASE WHEN json_array_length(json_extract(json_document_original, '$.obs')) > 1 THEN 1 ELSE 0 END) STORED
    , device_received_utc_timestamp_epoch INTEGER GENERATED ALWAYS AS (CAST(json_extract(json_document_original, '$.obs[0][0]') AS INTEGER)) STORED
    , device_received_utc_timestampz TEXT GENERATED ALWAYS AS (datetime(CAST(json_extract(json_document_original, '$.obs[0][0]') AS INTEGER), 'unixepoch')) STORED
    -- Type-specific JSON derived fields
    , air_temperature_at_timestamp REAL GENERATED ALWAYS AS (CAST(json_extract(json_document_original, '$.obs[0][7]') AS REAL)) STORED
    , illuminance_at_timestamp REAL GENERATED ALWAYS AS (CAST(json_extract(json_document_original, '$.obs[0][9]') AS REAL)) STORED
    , lightning_strike_average_distance_in_reporting_interval REAL GENERATED ALWAYS AS (CAST(json_extract(json_document_original, '$.obs[0][14]') AS REAL)) STORED
    , lightning_strikes_in_reporting_interval INTEGER GENERATED ALWAYS AS (CAST(json_extract(json_document_original, '$.obs[0][15]') AS INTEGER)) STORED
    , rain_accumulation_in_reporting_interval REAL GENERATED ALWAYS AS (CAST(json_extract(json_document_original, '$.obs[0][12]') AS REAL)) STORED
    , relative_humidity_at_timestamp REAL GENERATED ALWAYS AS (CAST(json_extract(json_document_original, '$.obs[0][8]') AS REAL)) STORED
    , reporting_interval INTEGER GENERATED ALWAYS AS (CAST(json_extract(json_document_original, '$.obs[0][17]') AS INTEGER)) STORED
    , solar_radiation_at_timestamp REAL GENERATED ALWAYS AS (CAST(json_extract(json_document_original, '$.obs[0][11]') AS REAL)) STORED
    , station_pressure_at_timestamp REAL GENERATED ALWAYS AS (CAST(json_extract(json_document_original, '$.obs[0][6]') AS REAL)) STORED
    , uv_index_at_timestamp REAL GENERATED ALWAYS AS (CAST(json_extract(json_document_original, '$.obs[0][10]') AS REAL)) STORED
    , wind_direction_average_in_wind_sample_interval INTEGER GENERATED ALWAYS AS (CAST(json_extract(json_document_original, '$.obs[0][4]') AS INTEGER)) STORED
    , wind_sample_interval INTEGER GENERATED ALWAYS AS (CAST(json_extract(json_document_original, '$.obs[0][5]') AS INTEGER)) STORED
    , wind_speed_average_in_wind_sample_interval REAL GENERATED ALWAYS AS (CAST(json_extract(json_document_original, '$.obs[0][2]') AS REAL)) STORED
    , wind_speed_gust_in_wind_sample_interval REAL GENERATED ALWAYS AS (CAST(json_extract(json_document_original, '$.obs[0][3]') AS REAL)) STORED
    , wind_speed_lull_in_wind_sample_interval REAL GENERATED ALWAYS AS (CAST(json_extract(json_document_original, '$.obs[0][1]') AS REAL)) STORED
    -- Per-installation identifier to distinguish records from different app installs
    , installation_id TEXT NULL
);

CREATE INDEX IF NOT EXISTS idx_observation_device_received_utc_timestampz ON observation
(
    device_received_utc_timestampz
);

CREATE INDEX IF NOT EXISTS idx_observation_installation_id ON observation
(
    installation_id
);
