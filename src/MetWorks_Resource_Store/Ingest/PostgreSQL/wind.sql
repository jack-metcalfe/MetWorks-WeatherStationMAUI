CREATE TABLE IF NOT EXISTS public.wind
    (
        id TEXT PRIMARY KEY -- COMB-style GUID supplied by SQLite sync or C# writer
        -- Timestamps (all UTC)
        , application_received_utc_timestampz TIMESTAMPTZ NOT NULL
        , database_received_utc_timestampz TIMESTAMPTZ DEFAULT now()
        -- JSON payloads
        , json_document_original JSON NOT NULL
        , json_document_jsonb JSONB GENERATED ALWAYS AS (json_document_original) STORED
        -- JSON derived fields
        , device_received_utc_timestamp_epoch BIGINT GENERATED ALWAYS AS ( (json_document_original -> 'ob' ->> 0)::BIGINT ) STORED
        , device_received_utc_timestampz TIMESTAMPTZ GENERATED ALWAYS AS ( to_timestamp((json_document_original -> 'ob' ->> 0)::DOUBLE PRECISION) ) STORED
        -- Type-specific JSON derived fields
        , wind_speed     REAL GENERATED ALWAYS AS ((json_document_original -> 'ob' ->> 1)::REAL) STORED
        , wind_direction INTEGER GENERATED ALWAYS AS ((json_document_original -> 'ob' ->> 2)::INTEGER) STORED
        -- Per-installation identifier to distinguish records from different app installs
        , installation_id UUID NULL
    )
;
ALTER TABLE public.wind OWNER TO weather;
CREATE INDEX IF
NOT EXISTS idx_wind_device_received_utc_timestampz ON public.wind
    (
        device_received_utc_timestampz
    )
;
CREATE INDEX IF
NOT EXISTS idx_wind_installation_id ON public.wind
    (
        installation_id
    )
;
