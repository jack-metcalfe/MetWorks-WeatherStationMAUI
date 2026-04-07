# 07 — Receiver Migration Plan: LAN → Azure

> **Status**: Planning  
> **Date**: 2026-01-19  
> **Scope**: StreamReceiver + QueueWorker (server-side ingest)

## 1. Context

The MAUI weather station app streams SQLite data to a `StreamReceiver` web service via
HTTP POST at configurable intervals. Today the receiver runs on the same LAN as the MAUI
device and writes to a local SQL Server instance. The goal is to move the receiver to Azure
while remaining **call-compatible** — the MAUI app's shipping code does not change.

Both solutions (`MetWorks-WeatherStationMAUI.sln` and the server-side solution) live at
the same directory level and share a common `.github/copilot-instructions.md`.

---

## 2. Current Architecture (LAN)

```
MAUI App                          LAN Server
┌──────────────┐   NDJSON+gzip   ┌────────────────────────┐
│ Observation   │──POST /ingest/──│ StreamReceiver          │
│ Wind          │   v1/stream     │  → SQL Server           │
│ Lightning     │                 │    Weather.IngestQueue   │
│ Precipitation │                 └───────────┬──────────────┘
│ StationMeta   │                             │
│ Logs          │                  ┌───────────▼──────────────┐
└──────────────┘                  │ QueueWorker               │
                                  │  → SQL Server              │
                                  │    Weather.RawIngest       │
                                  └────────────────────────────┘
```

### Ingest Endpoint

- **Route**: `POST /ingest/v1/stream`
- **Content-Type**: `application/x-ndjson`
- **Content-Encoding**: `gzip` (optional)
- **Response**: `{ "ackedUpToRowId": <long>, "receivedLines": <int>, "enqueued": <int>, "duplicates": <int>, "jsonErrors": <int> }`

### Wire Format (NDJSON lines)

Each line is a JSON object. Two shapes exist depending on source table:

**Observation / Wind / Lightning / Precipitation / StationMetadata:**
```json
{
  "table": "observation",
  "installationId": "guid",
  "rowid": 501,
  "id": "comb-guid",
  "application_received_utc_timestampz": 1234567890,
  "payload": { "type": "obs_st", "serial_number": "...", ... }
}
```

**Log entries:**
```json
{
  "table": "log_entries",
  "installationId": "guid",
  "rowid": 1042,
  "id": "comb-guid",
  "timestamp_utc": "2026-01-19T10:00:00.000Z",
  "level": "Information",
  "message": "...",
  "exception": null,
  "properties_json": "{...}",
  "installation_id": "guid"
}
```

### Key Fields

| Field | Type | Purpose |
|---|---|---|
| `rowid` | long | SQLite hidden rowid — monotonically increasing per table. Used for **progress tracking** (ACK watermark). |
| `id` | string (GUID) | COMB GUID — globally unique per record. Used for **dedup/identity**. Present on all table types (as of 2026-01-19 log table change). |
| `installationId` | string (GUID) | Identifies the sending device/installation. |
| `table` | string | Source table name (`observation`, `wind`, `lightning`, `precipitation`, `station_metadata`, `log_entries`). |

---

## 3. Dedup Design

### Current: SHA256 Content Hash

The receiver computes `SHA256(raw NDJSON line bytes)` and inserts with `RecordHash CHAR(64)` as a unique index on `IngestQueue`. Duplicate inserts are caught by SQL unique constraint violation (error 2601/2627).

### Planned: `(InstallationId, RecordId)` Composite Key

All source tables now ship a COMB GUID `id` field:
- Observation/wind/lightning/metadata tables: GUID assigned by `IdGenerator.CreateCombGuid()` at UDP packet arrival
- Log table: GUID assigned by `IdGenerator.CreateCombGuid()` in the Serilog sink at emit time (changed 2026-01-19, previously `INTEGER PRIMARY KEY AUTOINCREMENT`)

A `UNIQUE INDEX ON (InstallationId, RecordId)` on `IngestQueue` replaces or supplements the SHA256 hash. Benefits:
- No per-line hash computation on the receiver
- Dedup key is carried in the data, not derived from content
- Consistent across all table types

The SHA256 `RecordHash` column can be retained as a secondary integrity check or removed entirely.

### Rowid Reuse After DB Wipe

SQLite's hidden `rowid` can be reused after database deletion/recreation. This is safe because:
- `rowid` is only used for progress watermarking, not dedup
- The GUID `id` is globally unique regardless of rowid reuse
- After a DB wipe, `shipper_state` resets to `lastAckedRowId = 0`, so the shipper restarts from the beginning

---

## 4. Azure Hosting: Container Apps

### Why Container Apps

| Factor | Fit |
|---|---|
| Scale-to-zero (Consumption plan) | Weather station pushes batches at intervals — zero cost between pushes |
| Stateless HTTP | StreamReceiver is a single POST endpoint with no in-memory state |
| Built-in TLS termination | Required for internet-facing endpoint (LAN was plain HTTP) |
| Managed identity | Access Azure SQL without connection-string secrets |
| Cold start (~2-5s) | Acceptable for batch ingest, not latency-sensitive |

### Rejected Alternatives

- **App Service**: No scale-to-zero on Basic/Free tier (~$13/month idle)
- **Azure Functions**: PipeReader-based NDJSON streaming is more natural in full ASP.NET Core; execution time limits
- **AKS**: Overkill for one lightweight API
- **Container Instances**: No built-in ingress/TLS

### Deployment Artifacts Needed

- `Dockerfile` (multi-stage: SDK build → runtime image)
- Bicep or Terraform for Container App Environment + Container App + Azure SQL
- Managed identity configuration for Azure SQL access
- Container registry (ACR or GitHub Container Registry)

---

## 5. Storage: Phased Approach

### Phase 1: Azure SQL (Minimum Code Change)

Swap the SQL Server connection string to Azure SQL. Everything else works as-is:
- `IngestQueue` table, stored procs, QueueWorker — unchanged
- Dedup via unique index works identically
- ~$5/month (Basic tier)

**Best for**: Getting to Azure fast with zero code changes to the ingest path.

### Phase 2: Blob Storage + SQL Index (Cost Optimization, ~10-50 senders)

StreamReceiver writes NDJSON batch as a blob (`ingest/{installationId}/{timestamp}-{batchId}.ndjson.gz`), inserts a lightweight index row into Azure SQL (blob URI, metadata). Download API reads from blob storage.

### Phase 3: Blob-Only (Large Scale, 100+ senders)

Eliminate Azure SQL. Blob listing by prefix replaces the index. ~$0.02/GB stored.

---

## 6. Download API (New — Does Not Exist Today)

A LAN-local service needs to retrieve data from Azure. Two new endpoints on the same Container App:

### `GET /ingest/v1/download`

**Query parameters:**
- `installationId` (required) — filter to a specific device
- `afterRowId` (optional, default 0) — return records with `SourceRowId > afterRowId`
- `table` (optional) — filter to a specific source table
- `limit` (optional, default 500) — max records to return

**Response**: `application/x-ndjson` — same wire format the MAUI app sends. The LAN service consumes the same NDJSON format, keeping the pipeline symmetric.

### `POST /ingest/v1/ack`

**Body:**
```json
{
  "installationId": "guid",
  "ackedUpToQueueId": 12345
}
```

Marks records as retrieved. Enables purge of consumed records from Azure SQL / blob storage.

### Authentication

API key header or Entra ID bearer token on all endpoints (ingest + download + ack). The MAUI app and LAN service authenticate the same way.

---

## 7. QueueWorker Disposition

### Option A: Keep as Separate Container

Run QueueWorker as a second container in the same Container App Environment. It dequeues from `IngestQueue` → inserts into `RawIngest`. No code changes.

### Option B: Merge into StreamReceiver

Add a `BackgroundService` to the StreamReceiver host that performs the same dequeue/insert logic. One container instead of two. Simpler deployment, but mixes concerns.

### Recommendation

Start with **Option A** for Phase 1 (zero code changes). Consider **Option B** later if the two-container overhead isn't justified.

---

## 8. Migration Sequence

```
Step  Action                                      Blocks
────  ──────────────────────────────────────────  ──────
 1    Create Dockerfile for StreamReceiver         —
 2    Create Dockerfile for QueueWorker            —
 3    Provision Azure SQL (Basic) + schema         —
 4    Provision Container App Environment (Bicep)  3
 5    Deploy StreamReceiver container              1, 4
 6    Deploy QueueWorker container                 2, 4
 7    Update MAUI endpointUrl setting to Azure     5
 8    Add download + ack endpoints                 5
 9    Test end-to-end: ship → ingest → download    7, 8
10    Decommission LAN SQL Server                  9
```

### Receiver-Side Code Changes for Phase 1

| File | Change |
|---|---|
| `SelfHostedStreamReceiverWebService.cs` | Connection string from config/env, add download + ack endpoints |
| `QueueWorkerProgram.cs` | Connection string from config/env (already supports env var) |
| `Dockerfile` (new) | Multi-stage build for each project |
| `deploy/` (new) | Bicep/Terraform for Azure SQL + Container Apps |

### Receiver-Side Code Changes for Dedup Transition

| File | Change |
|---|---|
| `SelfHostedStreamReceiverWebService.cs` | Add `UNIQUE INDEX (InstallationId, RecordId)`, optionally remove SHA256 computation |
| SQL schema | `ALTER TABLE Weather.IngestQueue ADD CONSTRAINT UQ_IngestQueue_InstallationRecord UNIQUE (InstallationId, RecordId)` |

---

## 9. Security Considerations

- **TLS**: Container Apps provides managed TLS certificates automatically
- **Authentication**: Add API key validation middleware or Entra ID JWT bearer auth
- **Secrets**: Azure SQL connection string via Container App secrets (not in code/config)
- **Managed Identity**: Preferred for Azure SQL access (no password in connection string)
- **Network**: Consider restricting ingest to known IP ranges if MAUI devices have static IPs

---

## 10. Cost Estimate (Phase 1)

| Component | Monthly Cost |
|---|---|
| Container Apps (Consumption, scale-to-zero) | ~$1-5 (usage-based) |
| Azure SQL Basic (5 DTU, 2 GB) | ~$5 |
| Container Registry (Basic) | ~$5 |
| **Total** | **~$11-15/month** |

At scale (50+ senders), Azure SQL would need Standard tier (~$15-30/month). At 100+ senders, the blob-based approach (Phase 3) becomes more cost-effective.
