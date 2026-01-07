CREATE TABLE IF NOT EXISTS public.observation
    (
        id TEXT PRIMARY KEY -- COMB-style GUID supplied by SQLite sync or C# writer
        -- Timestamps (all UTC)
        , application_received_utc_timestampz TIMESTAMPTZ NOT NULL
        , database_received_utc_timestampz TIMESTAMPTZ DEFAULT now()
        -- JSON payloads
        , json_document_original JSON NOT NULL
        , json_document_jsonb JSONB GENERATED ALWAYS AS (json_document_original) STORED
        -- JSON derived fields (obs is a 2D array; only first row parsed)
        , has_multiple_obs_rows               BOOLEAN GENERATED ALWAYS AS ( jsonb_array_length(json_document_original::jsonb -> 'obs') > 1 ) STORED
        , device_received_utc_timestamp_epoch BIGINT GENERATED ALWAYS AS ( (json_document_original -> 'obs' -> 0 ->> 0)::BIGINT ) STORED
        , device_received_utc_timestampz TIMESTAMPTZ GENERATED ALWAYS AS ( to_timestamp((json_document_original -> 'obs' -> 0 ->> 0)::DOUBLE PRECISION) ) STORED
        -- Type-specific JSON derived fields
        , air_temperature_at_timestamp                            REAL GENERATED ALWAYS AS ((json_document_original -> 'obs' -> 0 ->> 7)::REAL) STORED
        , illuminance_at_timestamp                                REAL GENERATED ALWAYS AS ((json_document_original -> 'obs' -> 0 ->> 9)::REAL) STORED
        , lightning_strike_average_distance_in_reporting_interval REAL GENERATED ALWAYS AS ((json_document_original -> 'obs' -> 0 ->> 14)::REAL) STORED
        , lightning_strikes_in_reporting_interval                 INTEGER GENERATED ALWAYS AS ((json_document_original -> 'obs' -> 0 ->> 15)::INTEGER) STORED
        , rain_accumulation_in_reporting_interval                 REAL GENERATED ALWAYS AS ((json_document_original -> 'obs' -> 0 ->> 12)::REAL) STORED
        , relative_humidity_at_timestamp                          REAL GENERATED ALWAYS AS ((json_document_original -> 'obs' -> 0 ->> 8)::REAL) STORED
        , reporting_interval                                      INTEGER GENERATED ALWAYS AS ((json_document_original -> 'obs' -> 0 ->> 17)::INTEGER) STORED
        , solar_radiation_at_timestamp                            REAL GENERATED ALWAYS AS ((json_document_original -> 'obs' -> 0 ->> 11)::REAL) STORED
        , station_pressure_at_timestamp                           REAL GENERATED ALWAYS AS ((json_document_original -> 'obs' -> 0 ->> 6)::REAL) STORED
        , uv_index_at_timestamp                                   REAL GENERATED ALWAYS AS ((json_document_original -> 'obs' -> 0 ->> 10)::REAL) STORED
        , wind_direction_average_in_wind_sample_interval          INTEGER GENERATED ALWAYS AS ((json_document_original -> 'obs' -> 0 ->> 4)::INTEGER) STORED
        , wind_sample_interval                                    INTEGER GENERATED ALWAYS AS ((json_document_original -> 'obs' -> 0 ->> 5)::INTEGER) STORED
        , wind_speed_average_in_wind_sample_interval              REAL GENERATED ALWAYS AS ((json_document_original -> 'obs' -> 0 ->> 2)::REAL) STORED
        , wind_speed_gust_in_wind_sample_interval                 REAL GENERATED ALWAYS AS ((json_document_original -> 'obs' -> 0 ->> 3)::REAL) STORED
        , wind_speed_lull_in_wind_sample_interval                 REAL GENERATED ALWAYS AS ((json_document_original -> 'obs' -> 0 ->> 1)::REAL) STORED
    )
;
ALTER TABLE public.observation OWNER TO weather;
CREATE INDEX IF
NOT EXISTS idx_observation_device_received_utc_timestampz ON public.observation
    (
        device_received_utc_timestampz
    )
;