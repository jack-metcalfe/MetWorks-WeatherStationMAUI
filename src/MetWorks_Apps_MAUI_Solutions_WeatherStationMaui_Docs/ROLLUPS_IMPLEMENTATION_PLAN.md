# Device-time rollups (SQLite-first) – Implementation Plan

## Goals
- Enable fast, local reporting on Android by maintaining pre-aggregated rollups in SQLite.
- Use **device** time (`device_received_utc_timestamp_epoch` derived from Tempest `EpochTimeOfMeasurement`) as the canonical timeline for buckets.
- Keep ingestion append-only and resilient; rollups are best-effort and rebuildable.
- Bound local storage via configurable retention and eventual compaction.

## Non-goals (for this phase)
- Shipping aggregates to Postgres / remote sync.
- Full historical rebuild UX.
- Schema migration framework beyond “drop-and-recreate in dev” / “create-if-not-exists in prod”.

## Assumptions / Constraints
- Fact tables already exist with generated columns including `device_received_utc_timestamp_epoch` and `installation_id`.
- SQLite is single-writer; long-running writes must be chunked.
- Generated columns and JSON1 are runtime-probed in `MetWorks_Ingest_SQLite`.

## Proposed architecture
### Facts
- Continue writing raw packets to tables like `observation`, `wind`, `precipitation`, `lightning`.

### Rollups
- Add new rollup tables keyed by:
  - `installation_id`
  - `bucket_start_epoch` (INTEGER, device-time)
  - `bucket_width_seconds` (e.g., 60 / 3600 / 86400)
- Store aggregate fields needed by UI (min/max/avg/sum/count) per bucket.

### Watermarking
- Maintain a `rollup_state` table per `(installation_id, source_table, bucket_width_seconds)`.
- Watermark is a **device epoch** representing “up to but not including this epoch has been rolled up”.

### Scheduling
- A background `ServiceBase` worker in the MAUI host process:
  - runs periodically (e.g., every 30–60s)
  - processes in small batches
  - yields/cancels promptly
  - logs progress sparingly

## Steps
### 1) Repo scan & alignment
- Locate any existing “rollup/aggregate” code or docs.
- Confirm which tables the UI queries today (raw vs derived).
- Confirm the canonical device epoch for each packet type.

### 2) Schema design (SQLite)
- Define rollup tables per domain (start with `observation`):
  - `observation_rollup_1m`
  - `observation_rollup_1h`
  - optionally `observation_rollup_1d`
- For each rollup table define:
  - primary key / unique constraint
  - required indexes for range queries
  - minimal aggregate columns needed for charts

### 3) DDL implementation
- Add new DDL scripts under `src/MetWorks_Resource_Store/Ingest/SQLite/`:
  - `rollup_state.sql`
  - `observation_rollup_1m.sql`, `observation_rollup_1h.sql`, ...
- Ensure they are included in the initialization path used by `Initializer.DatabaseInitializeAsync(...)`.

### 4) Rollup worker service
- Create `RollupWorker` (or similarly named) in an appropriate project (likely `MetWorks_Ingest_SQLite` or a MAUI app services project), derived from `ServiceBase`.
- Responsibilities:
  - open SQLite connection
  - compute next bucket ranges from watermark
  - aggregate facts into buckets using `GROUP BY bucket_start_epoch`
  - upsert into rollup table
  - advance watermark transactionally

### 5) Upsert strategy
- Prefer deterministic upsert via:
  - `INSERT INTO ... ON CONFLICT(installation_id, bucket_start_epoch) DO UPDATE SET ...`
- Aggregate queries should be idempotent for the processed window.

### 6) Retention policy
- Add retention settings (paths under `/services/jsonToSQLite/` or a new `/services/rollups/` group):
  - raw retention window (days)
  - rollup retention windows per bucket width
  - max DB size (soft limit)
- Implement periodic cleanup:
  - delete old raw rows by `device_received_utc_timestamp_epoch`
  - delete old rollup rows by `bucket_start_epoch`

### 7) Reporting integration
- Add query helpers for UI:
  - choose rollup table based on time range and desired resolution
  - fallback to raw queries when range is small or rollups unavailable
- Keep queries keyed by `installation_id`.

### 8) Validation
- Add integration tests (where feasible) that:
  - insert synthetic observations
  - run rollup worker
  - verify expected buckets and aggregates
- Manual validation on Android:
  - verify worker runs without ANRs
  - verify DB size does not grow unbounded

## Open questions
- Which aggregate fields are required by the current charts/pages?
- Do we want rollups for non-observation packet types in phase 1?
- Retention policy precedence when DB hits max size (delete oldest raw first vs preserve raw last N hours)?
