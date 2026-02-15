# SQLite Data layer: `MetWorks_Data_Sqlite`

This document describes the SQLite **Data layer** assembly introduced to centralize provider usage.

## Objective

`MetWorks_Data_Sqlite` exists to:

- Provide a **tiny**, provider-backed API for executing SQL against SQLite.
- Ensure **no leakage** of `Microsoft.Data.Sqlite` types into other implementation assemblies.
- Centralize connection handling and per-connection configuration (PRAGMAs).

In the target architecture, all other layers (persistence/domain/business/UI) depend on this assembly via its **interfaces and interop DTOs**, not via provider types.

## Non-goals

- This is not a general-purpose database wrapper.
- No ORM.
- No query builder.
- No schema/versioning framework.

The Data layer should only grow when a higher layer has a clear need.

## Where it fits

- **Persistence layer → Data layer**: persistence holds SQL + mapping logic; data layer performs DB I/O.
- **Business/functional callers → persistence layer**: callers exchange domain/persistence DTOs and do not see SQL.

See also:
- `SQLITE_LAYERING_OBJECTIVES.md`
- `PERSISTENCE_LAYER_SQLITE.md`

## Key rule: no provider leakage

Outside `MetWorks_Data_Sqlite`:

- Do not reference `Microsoft.Data.Sqlite`.
- Do not use any `Sqlite*` types.
- Do not catch provider exceptions (`SqliteException`, etc.).

## Public API surface

Namespace: `MetWorks.Data.Sqlite`

### Entry point

- `ISqliteDatabase`
  - `OpenSessionAsync(CancellationToken)` → opens a session
  - `ExecuteDdlAsync(IReadOnlyList<SqlScript>, CancellationToken)` → executes persistence-owned DDL scripts

### Session (unit of work)

- `ISqliteSession : IAsyncDisposable`
  - `ExecuteAsync(sql, parameters, ct)`
  - `ScalarAsync<T>(sql, parameters, ct)`
  - `QueryAsync<T>(sql, parameters, mapRow, ct)`
  - `ExecuteInTransactionAsync(work, ct)`

### Interop DTOs (persistence → data)

- `DbParam(name, value)`
  - Used by persistence when binding SQL parameters.

- `DbRow`
  - Neutral row accessor passed to the persistence mapper callback.
  - Includes helpers like `TryGetInt64`, `TryGetDouble`, `TryGetString`.

- `SqlScript(name, sql)`
  - Persistence-owned DDL scripts (embedded resources or string literals).
  - The data layer executes these, but does not own schema knowledge.

### Implementation types

- `SqliteDatabaseOptions`
  - `ConnectionString`
  - `JournalMode` (default: `WAL`)
  - `BusyTimeoutMs` (default: `5000`)

- `SqliteDatabase`
  - Concrete `ISqliteDatabase` implementation.
  - Opens the connection and applies PRAGMAs.

## Intended usage patterns

### 1) Execute DDL provided by the persistence layer

The persistence layer should expose a readiness API (e.g. `EnsureDatabaseReadyAsync`) that gathers all required `SqlScript` and passes them to the data layer.

Example (persistence layer owning DDL):

```csharp
using MetWorks.Data.Sqlite;

public sealed class SqliteReadinessService(ISqliteDatabase sqliteDatabase)
{
    public Task EnsureDatabaseReadyAsync(CancellationToken ct)
    {
        var scripts = new List<SqlScript>
        {
            new("metworks_meta", "CREATE TABLE IF NOT EXISTS metworks_meta (key TEXT PRIMARY KEY, value TEXT);"),
            // ...more DDL scripts owned by persistence...
        };

        return sqliteDatabase.ExecuteDdlAsync(scripts, ct);
    }
}
```

### 2) Write (non-query)

Use a session and `ExecuteAsync`.

Example:

```csharp
await using var session = await sqliteDatabase.OpenSessionAsync(ct);

_ = await session.ExecuteAsync(
    "INSERT INTO station_metadata (installation_id, json) VALUES ($installation_id, $json);",
    [
        new DbParam("$installation_id", installationId),
        new DbParam("$json", json)
    ],
    ct);
```

### 3) Scalar query

Use `ScalarAsync<T>`.

Example:

```csharp
await using var session = await sqliteDatabase.OpenSessionAsync(ct);

var latestEpoch = await session.ScalarAsync<long?>(
    "SELECT MAX(device_received_utc_timestamp_epoch) FROM observation WHERE installation_id = $installation_id;",
    [new DbParam("$installation_id", installationId)],
    ct);
```

### 4) Query + map to persistence DTOs

Use `QueryAsync<T>` and map `DbRow` to a persistence DTO.

Example:

```csharp
public sealed record ShipperStateDto(string StreamName, long WatermarkEpoch);

await using var session = await sqliteDatabase.OpenSessionAsync(ct);

var states = await session.QueryAsync(
    "SELECT stream_name, watermark_epoch FROM shipper_state WHERE installation_id = $installation_id;",
    [new DbParam("$installation_id", installationId)],
    row =>
    {
        _ = row.TryGetString("stream_name", out var streamName);
        _ = row.TryGetInt64("watermark_epoch", out var watermark);
        return new ShipperStateDto(streamName ?? string.Empty, watermark);
    },
    ct);
```

### 5) Transaction wrapper

Use `ExecuteInTransactionAsync` to ensure a set of operations is atomic.

Example:

```csharp
await using var session = await sqliteDatabase.OpenSessionAsync(ct);

await session.ExecuteInTransactionAsync(async (s, ct2) =>
{
    _ = await s.ExecuteAsync(
        "INSERT INTO rollup_state (installation_id, bucket_width_seconds, watermark_epoch) VALUES ($installation_id, $w, $watermark) " +
        "ON CONFLICT(installation_id, bucket_width_seconds) DO UPDATE SET watermark_epoch = excluded.watermark_epoch;",
        [
            new DbParam("$installation_id", installationId),
            new DbParam("$w", bucketWidthSeconds),
            new DbParam("$watermark", endExclusive)
        ],
        ct2);

    _ = await s.ExecuteAsync(
        "INSERT INTO observation_rollup_1h (installation_id, bucket_start_epoch, sample_count) VALUES ($installation_id, $bucket, $n) " +
        "ON CONFLICT(installation_id, bucket_start_epoch) DO UPDATE SET sample_count = excluded.sample_count;",
        [
            new DbParam("$installation_id", installationId),
            new DbParam("$bucket", bucketStart),
            new DbParam("$n", sampleCount)
        ],
        ct2);
}, ct);
```
