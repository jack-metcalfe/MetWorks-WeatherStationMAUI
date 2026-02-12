# LoggerSQLite stream shipping plan

## Steps
1. Inspect LoggerSQLite schema
   - Confirm table shape created by `LoggerSQLite.SqliteSink.EnsureTable()`.
   - Use `id INTEGER PRIMARY KEY AUTOINCREMENT` as the monotonic shipping cursor (`rowid` equivalent).
   - Expected columns: `id`, `timestamp_utc`, `level`, `message`, `exception`, `properties`, `installation_id`.
   - Note indexes on `timestamp_utc` and `installation_id` for queries/retention.

2. Inspect SQLite logging settings
   - Verify settings paths exist in `src/MetWorks_Resource_Store/data/settings.yaml` under `/services/loggerSQLite/*`.
   - Confirm relative `dbPath` resolution is app-data-dir based (matches shippers’ pattern).
   - Identify any “ship logs” knobs needed (likely under `/services/streamShipping/*` to match other shippers).
   - Confirmed current settings in `settings.yaml`:
     - `/services/loggerSQLite/dbPath` (default: `metworks-log.sqlite`)
     - `/services/loggerSQLite/tableName` (default: `log`)
     - `/services/loggerSQLite/minimumLevel` (default: `Information`)
     - `/services/loggerSQLite/autoCreateTable` (default: `true`)
   - DDI wiring exists for `LoggerSQLite` (generated initializer passes `ILoggerFile`, `ISettingRepository`, `IInstanceIdentifier`, and `RootCancellationTokenSource.Token`).

3. Choose shipping contract
   - Decide the NDJSON “row shape” for logs (this will be a separate shape from standard readings tables).
   - Prefer a stable cursor field (use `id`) and include `installation_id` (or enrich from `IInstanceIdentifier` if missing).
   - Keep payload gzip + `application/x-ndjson` to match the existing receiver + shipper pipeline.
   - Decide how to represent `properties` (ship as raw JSON string vs parsed key/value; simplest is ship as-is).
   - Proposed log row NDJSON shape (one JSON object per line):
     - `source`: `"logger_sqlite"`
     - `rowid`: `id` (from the SQLite log table)
     - `timestamp_utc`: from `timestamp_utc`
     - `level`: from `level`
     - `message`: from `message`
     - `exception`: from `exception` (nullable)
     - `properties_json`: from `properties` (ship raw string)
     - `installation_id`: from `installation_id` when present; otherwise enrich from `IInstanceIdentifier`
   - Receiver ack should mirror `rowid` semantics: only ack the max `rowid` that was successfully persisted.

4. Decide retention behavior
   - Local-first principle: shipping should be best-effort; acceptable to lose logs when offline.
   - Choose whether to delete shipped rows (optional) vs time-based retention (e.g., keep last N days) vs size-based.
   - Ensure retention is “delete oldest first” and does not block app operation if DB is locked/busy.
   - Recommendation: start with time-based retention (e.g., keep last N hours/days) because SQLite log volume can be bursty.
   - Option A (simplest to implement): time-based purge
     - Delete where `timestamp_utc < now_utc - retentionWindow`.
     - Run purge periodically (e.g., once per hour) in the shipper loop.
   - Option B (strong cursor semantics): purge only rows that are both (a) older than retention window and (b) `id <= ackedUpToRowId`.
     - Avoids deleting logs that were never successfully shipped.
   - Option C (aggressive): delete everything `id <= ackedUpToRowId` (keeps DB small)
     - Acceptable if you explicitly do not need local log history once shipped.

5. Implement log shipper
   - Implement a `ServiceBase`-style background shipper similar to readings shippers.
   - Read batches ordered by `id`, send gzip NDJSON to `/ingest/v1/stream` with a distinct source name.
   - Track watermark in `shipper_state` keyed by `(installationId, source)`.
   - Use timeout/cancellation end-to-end; treat SQLite exceptions as transient with backoff.

6. Wire shipper into DDI
   - Add the shipper to `WeatherStationMaui.yaml` using existing DDI conventions (parameterless ctor + `InitializeAsync`).
   - Ensure `instance:` ordering respects define-before-use dependencies.
   - Reuse the existing `StreamShippingHttpClientProvider` and `IInstanceIdentifier` like other shippers.

7. Add receiver handling
   - Extend the receiver to persist log events (currently it only computes `ackedUpToRowId`).
   - Create a receiver-side table/schema for logs (or a generic “stream inbox” table) and upsert/insert by cursor.
   - Validate input: enforce max line size, handle malformed JSON, keep ack semantics consistent (ack only what you stored).

8. Add smoke test
   - Add a smoke test that posts gzipped NDJSON log rows and verifies `ackedUpToRowId` advances.
   - Optionally add a local SQLite query to confirm persistence in the receiver DB.

9. Build solution
   - Run a full build to ensure all targets compile (`.NET 10` / `.NET 8`).
   - If adding new settings definitions or YAML changes, ensure ordering/paths remain lexicographically sorted.

10. Validate end-to-end
   - Confirm: logs are written locally, shipper reads them, receiver accepts/persists, and watermark persists across restarts.
   - Simulate offline/locked DB scenarios: ensure no crashes and the shipper resumes automatically.
   - Verify `LoggerSQLite.IsHealthy` remains meaningful (DB down flips unhealthy; recovery flips healthy).
