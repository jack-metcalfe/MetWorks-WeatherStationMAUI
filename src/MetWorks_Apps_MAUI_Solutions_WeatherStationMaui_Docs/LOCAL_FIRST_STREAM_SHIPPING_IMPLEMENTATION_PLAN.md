# Local-first stream shipping – implementation plan (readings-first)

Status note: this document is a living plan and has been updated to reflect the current repository implementation. For broader layering direction (Data layer vs Persistence layer), see `SQLITE_LAYERING_OBJECTIVES.md`.

This document captures the implementation approach for **local-first shipping via file/stream** (NDJSON-over-HTTP) in this solution, starting with *readings* tables (e.g., `observation`) and expanding to all SQLite-persisted data.

It is intended to be readable both by developers and by automated agents working in this repo.

## Goals

- Ship **all SQLite-persisted** device data to a remote ingestion service without coupling the app to a specific server database.
- Use **append-only + watermark/ack** semantics so shipping is restartable and idempotent.
- Start with one readings table (currently `observation`) and then expand to other readings and aux tables.

## Current implementation status (in repo)

### Implemented

- `shipper_state` schema embedded resource: `Ingest/SQLite/shipper_state.sql`
- Shipper services (NDJSON-over-HTTP + gzip, watermarking via `shipper_state`):
  - `MetWorks.Ingest.SQLite.Shipping.ObservationStreamShipper` (`source=observation`)
  - `MetWorks.Ingest.SQLite.Shipping.WindStreamShipper` (`source=wind`)
  - `MetWorks.Ingest.SQLite.Shipping.PrecipitationStreamShipper` (`source=precipitation`)
  - `MetWorks.Ingest.SQLite.Shipping.LightningStreamShipper` (`source=lightning`)
  - `MetWorks.Ingest.SQLite.Shipping.StationMetadataStreamShipper` (`source=station_metadata`)
  - `MetWorks.Ingest.SQLite.Shipping.LoggerSQLiteStreamShipper` (`source=logger_sqlite`)
  - All ship batches use `(installation_id, rowid)` (or log `id`) as the monotonic cursor
  - All shippers call `IStreamShippingDatabaseReadiness.EnsureReadyAsync(...)` before reading
- Settings wiring:
  - `SettingConstants.StreamShipping_*`
  - `LookupDictionaries.StreamShippingGroupSettingsDefinition`
- HTTP client provider:
  - `StreamShippingHttpClientProvider` reads `/services/streamShippingHttp/timeoutSeconds`
- Persistence-side helpers:
  - `MetWorks.Persistence.StreamShipping.StreamShippingRepository` (ships state + standard reading batches)

Note: SQLite DB settings are now unified under `/services/sqlite/*` (see `sqlite-migration-plan.md`).

### Not implemented yet

- Confirm/complete `instance:` wiring for all shippers in `WeatherStationMaui.yaml` (required to actually construct/run them)
- Additional shippers for other tables (e.g. `metrics_summary`, and any future rollup/aux tables)
- Best-effort retention cleanup that can delete unacked rows + record `last_lossy_deleted_rowid`

Status update: stream shipping `instance:` wiring is implemented in `WeatherStationMaui.yaml` for all current shippers.

## Concepts and patterns

### NDJSON (Newline-Delimited JSON)

**NDJSON** is a streaming-friendly format where each line is a standalone JSON object.

Example (two records):

- line 1: `{ "table":"observation", "rowid":123, ... }`
- line 2: `{ "table":"observation", "rowid":124, ... }`

Why it’s used here:

- Easy to generate incrementally and upload as a stream
- Easy for a server to process incrementally without loading the entire payload into memory
- Easy to compress effectively with gzip

### Idempotency

The server should be able to apply the same record more than once without creating duplicates.

Recommended idempotency keys:

- **Server dedupe/upsert should be keyed by** `(installationId, table, id)` where `id` is the COMB GUID.
- Still ship and store `rowid` on the server for provenance/debugging, but **do not** use `rowid` as the dedupe key because it resets on DB recreation.
- **Client shipping cursor/watermark remains** `rowid` because it is monotonic and efficient.

Key strategy (SQLite):

- Use **Option A**: keep the COMB GUID `id` as `PRIMARY KEY` (or `UNIQUE`) for local integrity.
  - Note: in SQLite, `PRIMARY KEY`/`UNIQUE` implies an index; this is required to enforce uniqueness and prevent subtle duplication bugs.
- Use SQLite **`rowid`** (implicit) as the local monotonic cursor for batching, watermarking, and best-effort retention decisions.

### Watermark + Ack

Each local table is treated as a **source stream**.

- The shipper sends rows where `rowid > last_acked_rowid`.
- The server responds with `ackedUpToRowId`.
- The client persists the ack watermark and can safely retry.

### Installation scoping (`installation_id` filtering)

For readings tables (`observation`, `wind`, `precipitation`, `lightning`), shippers filter on `installation_id = <current installationId>`.

Implication:

- If a SQLite database file contains rows written under a previous `installation_id` (for example, after a reinstall or a DB migration/copy), those rows will not be shipped by the current shipper run.
- This is intentional for stream separation and to reduce accidental cross-install “replay”, but it can surprise you if you expected a new install to ship historical rows from an older install.

### DDI (Declarative Dependency Injection) + InitializeAsync conventions

This repo uses a DDI/codegen approach:

- Classes are expected to have **parameterless constructors**.
- Runtime wiring happens through an async `InitializeAsync(...)` method.
- Prefer passing **interfaces** (especially for cross-layer services) and avoid reflection-heavy patterns.
- In `WeatherStationMaui.yaml`, **`instance:` order matters**:
  - any instances referenced in an instance’s `assignment:` must appear earlier in the list.

## Data model: `shipper_state`

Per `(installation_id, source)`:

- `last_shipped_rowid` – last rowid attempted/shipped
- `last_acked_rowid` – last rowid acknowledged by the server
- `last_lossy_deleted_rowid` – watermark for *unacked but deleted* rows in best-effort mode (future)
- `lossy_deleted_row_count` – cumulative count of such deletions (future)
- `last_lossy_delete_utc` – timestamp of last lossy deletion event (future)

## Settings

Settings live under the existing settings mechanism.

Recommended settings under `/services/streamShipping/*`:

- `enabled` (bool)
- `endpointUrl` (string) – e.g. `https://example.com/ingest/v1/stream`
- `shipIntervalSeconds` (int)
- `maxBatchRows` (int)

SQLite db settings are under `/services/sqlite/*`.

## Implementation steps (incremental)

### Step 1: Add the shipper to `instance:` in `WeatherStationMaui.yaml`

Add a new instance entry *after* all required dependencies exist (must appear earlier):

- `RootCancellationTokenSource`
- `TheLoggerResilient`
- `TheSettingRepository`
- `TheEventRelayBasic`
- `TheInstanceIdentifier`
- A centralized `HttpClient` provider instance (recommended)

Then add (authoritative instance names in this repo as of this writing):

- `TheStreamShippingHttpClientProvider`
  - `iLogger` -> `TheLoggerResilient`
  - `iSettingRepository` -> `TheSettingRepository`
  - `iEventRelayBasic` -> `TheEventRelayBasic`
  - `externalCancellation` -> `TheRootCancellationTokenSource.Token`
  - `provenanceTracker` -> `TheProvenanceTracker`

- `TheStreamShippingDatabaseReadiness`
  - `sqliteDatabase` -> `TheSqliteDatabase`
  - `cancellationToken` -> `TheRootCancellationTokenSource.Token`

- `TheStreamShippingRepository`
  - `sqliteDatabase` -> `TheSqliteDatabase`
  - `instanceIdentifier` -> `TheInstanceIdentifier`
  - `cancellationToken` -> `TheRootCancellationTokenSource.Token`

- `TheLoggerStreamShippingRepository` (used by `logger_sqlite` shipping only)
  - `sqliteDatabase` -> `TheSqliteDatabase`
  - `streamShippingRepository` -> `TheStreamShippingRepository`
  - `cancellationToken` -> `TheRootCancellationTokenSource.Token`

- Shippers (all current shippers use the same pattern and share dependencies):
  - `TheObservationStreamShipper`
  - `TheWindStreamShipper`
  - `ThePrecipitationStreamShipper`
  - `TheLightningStreamShipper`
  - `TheStationMetadataStreamShipper`
  - `TheLoggerSQLiteStreamShipper` (also assigns `loggerStreamShippingRepository` -> `TheLoggerStreamShippingRepository`)
  - Common assignments:
    - `iLogger` -> `TheLoggerResilient`
    - `iSettingRepository` -> `TheSettingRepository`
    - `iEventRelayBasic` -> `TheEventRelayBasic`
    - `iInstanceIdentifier` -> `TheInstanceIdentifier`
    - `streamShippingDatabaseReadiness` -> `TheStreamShippingDatabaseReadiness`
    - `streamShippingRepository` -> `TheStreamShippingRepository`
    - `httpClient` -> `TheStreamShippingHttpClientProvider.Client` (dotted property access)
    - `externalCancellation` -> `TheRootCancellationTokenSource.Token`
    - `provenanceTracker` -> `TheProvenanceTracker`

Notes:

- Preferred pattern is to keep shipper signatures as `HttpClient httpClient` and centralize configuration in a single provider service.
- The provider service is responsible for constructing/configuring an `HttpClient` using settings, and exposing it as a property (e.g., `Client`).
- DDI dotted-property instance access requires that the provider's `Client` property is declared under that class’s `property:` list in the `namespace:` section.
- `instance:` ordering still applies: `TheStreamShippingHttpClientProvider` must be defined before any shipper instance that references `TheStreamShippingHttpClientProvider.Client`.

### Step 2: Clone shipper pattern for another readings table

Recommended next table:

- `wind` or `precipitation` (depending on which is most important downstream).

Implementation checklist:

- Create `<TableName>StreamShipper` class in `MetWorks.Ingest.SQLite.Shipping`
- Reuse: `shipper_state` table
- Table-specific changes:
  - `source` string
  - `SELECT rowid, id (if present), application_received_utc_timestampz, json_document_original FROM <table>`
  - NDJSON record `table` name

### Step 3: Expand to all SQLite-persisted sources

Candidate sources:

- Readings tables: `observation`, `wind`, `precipitation`, `lightning`
- Aux tables: `station_metadata`
- Logs:
  - SQLite logger table(s) (from `LoggerSQLite`)
- Operational:
  - `metrics_summary` (optional, but include if desired)

### Step 4: Add best-effort retention + loss reporting

When you add a retention job that deletes local rows on a time policy:

- allow deletion of unacked rows (best-effort mode)
- use `shipper_state.RecordLossyDeletionAsync(...)` to record:
  - deleted-through rowid
  - deleted row count
  - deletion timestamp

Then include the loss watermarks in uploads/metrics so the server can detect gaps.

## Server contract (suggested)

Endpoint:

- `POST /ingest/v1/stream`

Request:

- `Content-Type: application/x-ndjson`
- `Content-Encoding: gzip`

Response (minimal):

- `{ "ackedUpToRowId": 12345 }`

Response (recommended):

- `{ "ackedUpToRowId": 12345, "received": 500, "applied": 500, "errors": [] }`

## Security checklist

- TLS required
- Per-installation authentication token
- Server validates that token belongs to `installationId`
- Consider rate limiting per installation

## Notes for automated agents

- Respect repo conventions:
  - parameterless constructor + `InitializeAsync(...)`
  - settings via `SettingConstants` + `LookupDictionaries.*GroupSettingsDefinition`
  - DDI wiring via `WeatherStationMaui.yaml`
  - keep new code consistent with existing ingestor/worker patterns
- If introducing a new cross-cutting pattern (retry policies, HttpClient factory, new settings groups), update all similar components for consistency.
