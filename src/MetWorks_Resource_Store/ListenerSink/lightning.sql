CREATE TABLE IF NOT EXISTS public.lightning
    (
        id TEXT PRIMARY KEY -- COMB-style GUID supplied by SQLite sync or C# writer
        -- Timestamps (all UTC)
        , application_received_utc_timestampz TIMESTAMPTZ NOT NULL
        , database_received_utc_timestampz TIMESTAMPTZ DEFAULT now()
        -- JSON payloads
        , json_document_original JSON NOT NULL
        , json_document_jsonb JSONB GENERATED ALWAYS AS ( json_document_original ) STORED
        -- JSON derived fields
        , device_received_utc_timestamp_epoch BIGINT GENERATED ALWAYS AS ( (json_document_original -> 'evt' ->> 0)::BIGINT ) STORED
        , device_received_utc_timestampz TIMESTAMPTZ GENERATED ALWAYS AS ( to_timestamp((json_document_original -> 'evt' ->> 0)::DOUBLE PRECISION) ) STORED
        -- Type specific JSON derived fields
        , lightning_strike_distance_at_timestamp INTEGER GENERATED ALWAYS AS ((json_document_original -> 'evt' ->> 1)::INTEGER) STORED
        , relative_energy_content_at_timestamp   INTEGER GENERATED ALWAYS AS ((json_document_original -> 'evt' ->> 2)::INTEGER) STORED
    )
;
CREATE INDEX IF
NOT EXISTS idx_lightning_device_received_utc_timestamp_epoch ON lightning
    (
        device_received_utc_timestamp_epoch
    )
;