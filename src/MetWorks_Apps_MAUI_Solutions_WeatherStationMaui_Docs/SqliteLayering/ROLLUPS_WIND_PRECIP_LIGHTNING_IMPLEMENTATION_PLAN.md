# Rollups (Wind, Precipitation, Lightning) – Implementation Plan

Status note: this document is a living plan. It is intended to be used as an execution checklist for implementing additional rollups in the SQLite Persistence layer.

This plan assumes the current rollups system is observation-only and already has:
- Worker: `MetWorks.Ingest.SQLite.Rollups.RollupsWorker`
- Readiness: `MetWorks.Persistence.Rollups.IRollupsDatabaseReadiness`
- Observation rollups: `MetWorks.Persistence.Rollups.IObservationRollupRepository`

## Goals

- Add rollups for `wind`, `precipitation`, and `lightning` without introducing SQLite provider leakage.
- Keep rollups on a **single cadence** (the existing rollups worker tick), unless a proven need arises.
- Keep work per tick bounded to avoid lock contention and long-running writes.

## Non-goals

- A generic rollup framework or query builder.
- Multiple rollup workers by source type.
- A schema migration/versioning system beyond create-if-not-exists DDL.

## Design constraints and conventions

- Persistence layer owns DDL (via `SqlScript`) and SQL/mapping.
- Data layer (`MetWorks_Data_Sqlite`) executes SQL and DDL; no provider types leak outward.
- DDI wiring is authoritative via `WeatherStationMaui.yaml`.
- Do not edit generated `*.g.cs` files; regenerate after YAML changes.

## Plan

### Step 1: Inspect current readings schemas

Confirm each source table has:
- `installation_id`
- `device_received_utc_timestamp_epoch` (device-time bucketing)

Also identify which columns are worth aggregating for each rollup.

### Step 2: Design rollup tables (DDL)

Add rollup tables and indexes (proposed starting set):

- Wind
  - `wind_rollup_1h`
  - `wind_rollup_1d`

- Precipitation
  - `precipitation_rollup_1d` (or `*_1h` if needed later)

- Lightning
  - `lightning_rollup_1d` (or `*_1h` if needed later)

All rollup tables should be keyed by:
- `installation_id`
- `bucket_start_epoch`

Continue using `rollup_state` for per-source watermarks:
- `(installation_id, source_table, bucket_width_seconds)`

### Step 3: Implement rollup SQL builders

Create internal SQL builders under `MetWorks.Persistence.Rollups`:

- `WindRollupSql`
- `PrecipitationRollupSql`
- `LightningRollupSql`

Each should:
- expose `SourceTableName`
- provide `BuildUpsertRollupSql(rollupTableName, bucketWidthSeconds)`

### Step 4: Implement persistence repositories

Add new repository interfaces under `MetWorks.Persistence.Rollups`:

- `IWindRollupRepository`
- `IPrecipitationRollupRepository`
- `ILightningRollupRepository`

Implement each repository using:
- `ISqliteDatabase`
- the existing watermarking approach (`RollupWatermarkStore` + `rollup_state`)
- bounded work per tick (`maxBucketsPerRun`)

### Step 5: Update rollups readiness

Extend `RollupsSqlScripts.GetAll()` to include:
- new rollup tables
- indexes

Keep `RollupsDatabaseReadiness` unchanged (it should still call `ExecuteDdlAsync(RollupsSqlScripts.GetAll(), ...)`).

### Step 6: Update `RollupsWorker`

Update `MetWorks.Ingest.SQLite.Rollups.RollupsWorker` to:
- inject the new repositories in `InitializeAsync(...)`
- call their rollup methods in `RunOnceAsync(...)`

Policy:
- One worker tick runs multiple rollup repos sequentially.
- Each repo remains responsible for its own "no-op if nothing new" behavior.

### Step 7: Update DDI YAML wiring

Update `WeatherStationMaui.yaml`:

- Add new interfaces/classes under `MetWorks.Persistence.Rollups`
- Add new instances:
  - `TheWindRollupRepository`
  - `ThePrecipitationRollupRepository`
  - `TheLightningRollupRepository`
- Update `TheRollupsWorker` assignments to pass the new repositories

### Step 8: Regenerate DDI code (manual step)

After Step 7:
- Regenerate DDI output from `src/MetWorks_Apps_MAUI_Solutions_WeatherStationMaui_Docs/WeatherStationMaui.yaml`.
- Ensure `src/MetWorks_DdiRegistry/*.g.cs` reflects the new instance names and initializer parameters.

### Step 9: Build

Run a full workspace build.

### Step 10: Update docs

Update:
- `SqliteLayering/PERSISTENCE_LAYER_SQLITE.md` to include the new rollup APIs and owned tables.
- `SqliteLayering/ROLLUPS_IMPLEMENTATION_PLAN.md` to summarize the additional rollup sources and that they share the same cadence.
