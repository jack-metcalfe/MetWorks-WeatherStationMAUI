-- Migration: add installation_id (uuid) to raw packet tables
-- Apply this migration before starting the Postgres listener service.
-- Usage: psql -d yourdb -f 001_add_installation_id.sql

BEGIN;

-- Add installation_id uuid column if missing
ALTER TABLE IF EXISTS public.observation ADD COLUMN IF NOT EXISTS installation_id uuid NULL;
ALTER TABLE IF EXISTS public.wind ADD COLUMN IF NOT EXISTS installation_id uuid NULL;
ALTER TABLE IF EXISTS public.precipitation ADD COLUMN IF NOT EXISTS installation_id uuid NULL;
ALTER TABLE IF EXISTS public.lightning ADD COLUMN IF NOT EXISTS installation_id uuid NULL;

-- Create indexes (use CONCURRENTLY on large tables in production)
CREATE INDEX IF NOT EXISTS idx_observation_installation_id ON public.observation (installation_id);
CREATE INDEX IF NOT EXISTS idx_wind_installation_id ON public.wind (installation_id);
CREATE INDEX IF NOT EXISTS idx_precipitation_installation_id ON public.precipitation (installation_id);
CREATE INDEX IF NOT EXISTS idx_lightning_installation_id ON public.lightning (installation_id);

COMMIT;

-- NOTES:
-- 1) If you previously stored installation ids as text in a column named installation_id and want to convert
--    that textual column to UUID, run a conversion step. Example (validate first):
--
--    -- find invalid values (non-UUID)
--    SELECT installation_id FROM public.observation WHERE installation_id IS NOT NULL AND installation_id !~ '^[0-9a-fA-F\-]{36}$';
--
--    -- convert textual column to uuid (do this only after validating values)
--    ALTER TABLE public.observation ALTER COLUMN installation_id TYPE uuid USING (installation_id::uuid);
--
-- 2) For very large tables, create indexes CONCURRENTLY to avoid long locks:
--    CREATE INDEX CONCURRENTLY idx_observation_installation_id ON public.observation (installation_id);
--
-- 3) This migration only adds the new column and index. Running the Postgres listener after applying
--    this migration is required because the service treats missing/incorrect installation_id columns as fatal.
