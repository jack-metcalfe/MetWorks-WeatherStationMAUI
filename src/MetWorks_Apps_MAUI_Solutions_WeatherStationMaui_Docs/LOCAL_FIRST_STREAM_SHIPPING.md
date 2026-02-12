# Local-first shipping via file/stream (database-independent)

Pushing **files/streams** to a remote ingestion service (which then syncs into whatever database) is a solid *database-independent* alternative, and it fits a “local-first” pipeline well. In practice it’s an **offline-first replication log** + **remote apply/ack** pattern.

## Core idea
1. App writes everything locally (SQLite tables).
2. A shipper produces an **append-only transfer stream** (records or batches).
3. Remote service receives it, **applies idempotently**, then returns an **ack** (watermark) so the app can safely advance local shipping state / retention.

This keeps the app ignorant of Postgres (or any DB).

## Payload format options (from simplest to most robust)

### Option A: NDJSON “events” stream (recommended starting point)
- Each record is one JSON object per line, e.g.
  - `{ "table":"observation", "id":"...", "installationId":"...", "deviceEpoch":..., "payload":{...} }`
- Easy to stream (`HttpClient` request stream) and easy for the server to process incrementally.
- Compress on the fly (gzip) to reduce bandwidth.

**Idempotency:** remote service upserts by `id` (your COMB GUID) and/or `(installationId,id)`.

### Option B: “SQLite changeset” / WAL shipping (powerful but trickier)
- Ship SQLite’s replication artifacts (WAL frames / session changesets) to server.
- Very efficient, but:
  - ties you more to SQLite semantics
  - more complexity to validate/apply safely
  - harder to evolve schema + keep compatibility

### Option C: Periodic “export file” snapshots
- Export a range of rows to a file (csv/parquet/ndjson) and upload.
- Good when you don’t need near-real-time and want simple retry.
- Downside: duplicates unless you have precise watermarks.

## Watermarking / acknowledgements (key to reliability)
Even with file/stream shipping, you still want a `shipper_state` table locally, like:
- `source` (e.g., `observation`, `log`, `metrics_summary`)
- `last_shipped_rowid` (or other monotonic key)
- `last_acked_rowid`
- timestamps, failure counts

Flow:
1. App ships rows `(rowid > last_acked_rowid)` in batches.
2. Server responds with `{ ackedUpToRowId: N }` (per source stream).
3. App updates `last_acked_rowid = N`.
4. Retention deletes anything older than the last-N-hours policy **but never deletes rows > last_acked_rowid** unless you explicitly allow “best effort / lossy”.

**Note:** this design stays DB-independent *and* avoids relying on COMB sort order by using SQLite `rowid` as the monotonic cursor.

## Optimization opportunities (why this can be better than direct DB writes)
- **Compression:** JSON + gzip is often 5–20× smaller.
- **Shaping:** you can send only needed columns, or pre-normalize.
- **Batching:** fewer round trips than per-row DB inserts.
- **Backpressure:** server can throttle; client can adapt batch size.
- **Schema evolution:** payload can include version tags; server can transform.

## How to include “all data” (facts + logs + metrics)
Treat every local table as a source stream:
- `observation`, `wind`, `lightning`, `precipitation`
- `log`
- `metrics_summary`

Each record includes at minimum:
- `installationId`
- `sourceTable`
- `rowid` (or local sequence)
- `id` (COMB guid, if present)
- `application_received_utc_timestampz`
- `payload` (raw JSON or structured columns)

Logs/metrics may not have COMB ids today—if they don’t, rely on `(installationId, rowid)` and let the server store a synthetic id.

## Security considerations
- TLS always.
- Auth token per installation (device credential) to prevent spoofing.
- Server validates `installationId` matches credential.
- Optional signing (HMAC) per batch if you expect hostile networks.
- Rate limiting per installation.

## Recommended next design decision
Pick **NDJSON-over-HTTP with gzip**, with local `rowid` watermarking, and one endpoint like:

- `POST /ingest/v1/stream`
  - headers: `Content-Encoding: gzip`, `Content-Type: application/x-ndjson`
  - response: per-source ack watermarks + counts + errors

Then you can later:
- switch server storage engine (Postgres/Cosmos/etc.)
- add parallelism
- introduce protobuf/avro/parquet if needed

## Two questions to decide next
1. Do you want shipping to be **lossless** (never drop unacked) or **best-effort** (keep only last N hours even if not shipped)?
2. Do you want the server to store **raw JSON** (and derive later) or **normalized fields** (derive on device and send typed columns)?

### If you choose best-effort: capturing "what might have been lost"
Yes—treat potential loss as a first-class metric so you can detect and quantify gaps.

**Local additions (per source stream):**
- Track a separate watermark for *lossy deletions*, e.g. `last_lossy_deleted_rowid` (or `lossy_deleted_through_rowid`).
- Track counters like `lossy_deleted_row_count` and timestamps like `last_lossy_delete_utc`.

**How it works:**
1. Normal shipping continues to use `last_acked_rowid`.
2. Retention is allowed to delete rows older than your time window even if unacked.
3. When retention deletes unacked rows, advance `last_lossy_deleted_rowid` to the highest deleted `rowid` and increment `lossy_deleted_row_count`.

**What you can report upstream:**
- Include `last_acked_rowid` *and* `last_lossy_deleted_rowid` in periodic `METRICS` and/or in each ingest request.
- The server can detect possible gaps when `last_lossy_deleted_rowid > last_acked_rowid` and record a "may have been lost" interval `(last_acked_rowid, last_lossy_deleted_rowid]` for that `installationId` + `source`.

This stays database-independent and gives you a pragmatic, auditable signal of data loss without requiring the device to keep all unacked rows forever.

## Local storage consumption metrics
The app now includes a best-effort local storage size snapshot in the periodic `METRICS` JSON payload.

Settings:
- `/services/metrics/storageEnabled` (Boolean, default `true`)
- `/services/metrics/storageTopN` (Int32, default `10`) – how many of the largest rolled log files to include

Payload node:
- `storage`
  - `settings_override_bytes`
  - `log_file` (`path`, `bytes`) – current active file path when file logger is configured
  - `logger_sqlite_bytes`
  - `readings_sqlite_bytes`
  - `top_log_files[]` (`path`, `bytes`)
