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
- A background `ServiceBase` worker in the MAUI host process (current implementation: `MetWorks.Ingest.SQLite.Rollups.RollupsWorker`):
  - runs periodically (e.g., every 30–60s)
  - processes in small batches (bounded per tick)
  - yields/cancels promptly
  - logs progress sparingly

#### Intent: single cadence

Even though raw readings arrive at different rates (wind faster than observation; lightning/precipitation can be sparse), rollups should default to a **single cadence** driven by the observation rollup cadence.

Rationale:
- Observation rows are typically larger (JSON fan-out into columns), and observation rollups provide most of the UX/query value.
- For sparse sources, a rollup tick is usually a cheap no-op when there are no new rows.
- One worker with a single run guard reduces SQLite contention and avoids scheduling drift.

When/if we add wind/precipitation/lightning rollups:
- Prefer adding additional repository calls inside `RollupsWorker.RunOnceAsync(...)` rather than creating additional workers, unless there is a demonstrated need for separate cadences.

## Current rollup sources (implemented)

The single-cadence rollups worker (`MetWorks.Ingest.SQLite.Rollups.RollupsWorker`) now runs these rollup repositories sequentially per tick:

- `IObservationRollupRepository`
  - 1h + 1d rollups into `observation_rollup_1h` / `observation_rollup_1d`
- `IPrecipitationRollupRepository`
  - watermark-only advancement of `rollup_state` for `precipitation` (no rollup table)
- `IWindRollupRepository`
  - 1h + 1d rollups into `wind_rollup_1h` / `wind_rollup_1d`
- `ILightningRollupRepository`
  - 1d rollups into `lightning_rollup_1d`
