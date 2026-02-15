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

## Proposed architecture

### Facts
- Continue writing raw packets to tables like `observation`, `wind`, `precipitation`, `lightning`.

### Rollups
- Add rollup tables keyed by:
  - `installation_id`
  - `bucket_start_epoch` (INTEGER, device-time)
  - `bucket_width_seconds` (e.g., 3600 / 86400)

### Watermarking
- Maintain a `rollup_state` table per `(installation_id, bucket_width_seconds)`.
- Watermark is a **device epoch** representing “up to but not including this epoch has been rolled up”.

### Scheduling
- A background `ServiceBase` worker in the MAUI host process:
  - runs periodically (e.g., every 30–60s)
  - processes in small batches
  - yields/cancels promptly
  - logs progress sparingly
