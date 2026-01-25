CREATE TABLE IF NOT EXISTS public.precipitation
    (
        id TEXT PRIMARY KEY -- COMB-style GUID from C# supplied by SQLite sync or C# writer
        -- Timestamps (all UTC)
        , application_received_utc_timestampz TIMESTAMPTZ NOT NULL
        , database_received_utc_timestampz TIMESTAMPTZ DEFAULT now()
        -- JSON payloads
        , json_document_original JSON NOT NULL
        , json_document_jsonb JSONB GENERATED ALWAYS AS ( json_document_original ) STORED
        -- JSON derived fields
        , device_received_utc_timestamp_epoch BIGINT GENERATED ALWAYS AS ( (json_document_original -> 'evt' ->> 0)::BIGINT ) STORED
        , device_received_utc_timestampz TIMESTAMPTZ GENERATED ALWAYS AS ( to_timestamp((json_document_original -> 'evt' ->> 0)::DOUBLE PRECISION) ) STORED
    )
;
ALTER TABLE public.precipitation OWNER TO weather;
CREATE INDEX IF
NOT EXISTS idx_precipitation_device_received_utc_timestampz ON public.precipitation
    (
        device_received_utc_timestampz
    )
;