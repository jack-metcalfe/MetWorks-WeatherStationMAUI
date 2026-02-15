# SQLite Persistence layer: `MetWorks_Persistence`

This document describes the SQLite **Persistence layer** assembly introduced to centralize persistence operations (by consumer domain) while keeping functional code free of SQL and provider types.

This is a **living document** and should be updated each time we migrate a new persistence vertical slice.

## Objective

`MetWorks_Persistence` exists to:

- Expose stable, consumer-oriented persistence APIs (repositories/services) that accept/return **DTOs**.
- Own SQL strings and all mapping logic between relational rows and persistence DTOs.
- Own the schema (DDL) scripts for the SQLite database.
- Use `MetWorks_Data_Sqlite` for all SQLite I/O.

See also:
- `SQLITE_LAYERING_OBJECTIVES.md`
- `DATA_LAYER_SQLITE.md`

## Where it fits

- **Business/functional callers → Persistence layer**
  - Callers do not see SQL.
  - Callers do not see data-layer interop DTOs (`DbRow`, `DbParam`, `SqlScript`).

- **Persistence layer → Data layer** (`MetWorks_Data_Sqlite`)
  - Persistence passes SQL + `DbParam`.
  - Persistence maps results from `DbRow` into persistence DTOs.
  - Persistence supplies DDL scripts (`SqlScript`) to the data layer for execution.

## Key rule: no provider leakage

`MetWorks_Persistence` must not:

- Reference `Microsoft.Data.Sqlite`.
- Use or expose any `Sqlite*` provider types.
- Catch provider exceptions (`SqliteException`, etc.).

Provider behavior decisions (PRAGMAs, connection open policy, busy handling) belong in the Data layer.

## Public API surface (current)

Namespace root: `MetWorks.Persistence.*`

### Rollups (first vertical slice)

Namespace: `MetWorks.Persistence.Rollups`

- `IRollupsDatabaseReadiness`
  - `EnsureReadyAsync(CancellationToken)`
  - Purpose: ensure schema objects required by rollups exist.

- `IObservationRollupRepository`
  - `RollupHourAsync(int maxBucketsPerRun, CancellationToken cancellationToken)`
  - `RollupDayAsync(int maxBucketsPerRun, CancellationToken cancellationToken)`
  - Purpose: create/refresh aggregated observation rollups.

#### DDL ownership

`MetWorks.Persistence.Rollups.RollupsSqlScripts` currently declares the script set:

- `rollup_state`
- `observation_rollup_1h`
- `observation_rollup_1d`

### Stream shipping (vertical slice 2)

Namespace: `MetWorks.Persistence.StreamShipping`

Readiness + shared shipper state:

- `IStreamShippingDatabaseReadiness`
  - `EnsureReadyAsync(CancellationToken)`
  - Purpose: ensure schema objects required by stream shipping exist.

- `IStreamShippingRepository`
  - `TryGetStateAsync(string source, CancellationToken)`
  - `UpsertShippingProgressAsync(string source, long? lastShippedRowId, long? lastAckedRowId, CancellationToken)`
  - `ReadStandardReadingsBatchAsync(string table, string installationId, long lastAckedRowId, int maxRows, CancellationToken)`
  - Purpose: shared shipper-state API + batched reads for standard-reading tables.

Logger-specific operations:

- `ILoggerStreamShippingRepository`
  - `ReadLoggerBatchAsync(string table, long lastAckedRowId, int maxRows, CancellationToken)`
  - `PurgeAckedOlderThanAsync(string table, long ackedUpToRowId, DateTime cutoffUtc, CancellationToken)`
  - `RecordLossyDeletionAsync(string source, long deletedThroughRowId, int deletedRowCount, DateTime deletionUtc, CancellationToken)`
  - Purpose: logger-batch reads + retention purge + lossy-delete reporting for shipper state.

#### DDL ownership

`MetWorks.Persistence.StreamShipping.StreamShippingSqlScripts` currently declares the script set:

- `shipper_state`

Operational tables (e.g. `observations`, `wind_readings`) are owned by their respective persistence slices. Stream shipping owns:

- the shared `shipper_state` table (watermarks + lossy-delete reporting)
- the SQL/mapping needed to read from consumer-owned tables for shipping batches

## DTO boundaries

### Business/functional callers → persistence

- Repositories/services should accept/return persistence DTOs (or domain types).
- No SQL strings or DB implementation concepts should appear in these signatures.

### Persistence → data layer

- Persistence uses `DbParam` and `DbRow` internally.
- These interop DTOs should not be exposed from persistence public APIs.

### Stream shipping DTO notes

Stream shipping uses persistence DTOs to keep functional shippers provider-free:

- Standard readings shipper reads return `StandardReadingRow`.
- Logger shipper reads return `LoggerLogRow`.
- Shared shipper-state reads return `ShipperStateSnapshot`.

The shipper DTOs are persistence-level DTOs (not `DbRow`), and are safe to use from functional code.

## Readiness flow (intended)

1. A functional component (or early-start service) triggers readiness.
2. Persistence identifies the required DDL scripts for its slice.
3. Persistence calls the data layer (`ISqliteDatabase.ExecuteDdlAsync`) to execute those scripts.

For Rollups, the readiness service should execute `RollupsSqlScripts.GetAll()` via the data layer.

## Intended usage patterns

### 1) Call readiness from application startup (or before first rollup run)

- Caller depends on `IRollupsDatabaseReadiness`.
- Caller does not need to know about tables, PRAGMAs, scripts, or `Microsoft.Data.Sqlite`.

### 2) Rollup worker uses repository-only calls

- Worker depends on `IObservationRollupRepository`.
- Worker does not build connection strings.
- Worker does not run PRAGMAs.
- Worker does not catch SQLite provider exceptions.

## Resilience & concurrency (implementation notes)

In functional code, it’s ok to have *small* concurrency guards, as long as they aren’t SQLite-specific.

Two distinct “single-flight” patterns are used in the rollup worker:

- **Run guard** (`SemaphoreSlim`): prevents overlapping rollup runs from timer ticks.
  - Pattern: `if (!await _gate.WaitAsync(0, ct)) return;`.
  - Rationale: timers can tick while the previous run is still executing; overlap increases lock contention.

- **Readiness guard** (`_isInitializing` + `Interlocked.CompareExchange`): prevents concurrent readiness probes.
  - Pattern: `if (Interlocked.CompareExchange(ref _isInitializing, 1, 0) != 0) return false;`.
  - Rationale: on startup or when reconnecting, multiple triggers might call readiness at the same time.
  - This guard is *non-blocking*: callers don’t queue; they just skip and try again later.

If readiness fails, callers should treat it as **degraded mode** and retry later with a backoff (e.g. a periodic reconnection timer), rather than throwing and taking down service startup.

## Functional-client health checking (recommended pattern)

In this solution, “functional clients” are long-running services that *use* persistence (listeners, workers, shippers). Some of these are timer-driven, some are event-driven, and some run a loop.

To keep the approach consistent and avoid per-class reinvention, prefer the following policy:

### 1) Keep `ServiceBase.Ready` about initialization only

`ServiceBase.Ready` / `MarkReady()` should mean:

- the service finished reading settings and wiring dependencies
- the service registered for events (if applicable)
- the service started any background loops/timers

It should not mean “SQLite is healthy”. SQLite availability is not monotonic and can change over the process lifetime.

### 2) Track external dependency health separately (degraded vs active)

Use a simple, internal flag (pattern: `_isDatabaseAvailable`) to gate work:

- **Degraded mode**: the service stays alive but avoids database work.
- **Active mode**: the service performs its primary work.

This allows app startup to succeed even when the DB is unavailable.

### 3) Centralize the probe, not the business logic

When multiple functional clients need a “DB looks reachable” signal, centralize the *polling/retry/backoff* into a single monitor service, but keep DDL ownership and domain operations in persistence.

Two practical probe layers:

- **Data-layer probe** (generic, reusable): open a session and run a lightweight query (e.g. `SELECT 1`).
- **Persistence readiness** (slice-specific): call `*DatabaseReadiness.EnsureReadyAsync` when the slice is about to do real work.

### 4) Two non-blocking concurrency guards

When a service uses health checks and periodic work, there are two distinct “single-flight” guards that should remain separate:

- a **readiness/probe guard** (`Interlocked.CompareExchange`) to avoid concurrent probes
- a **run guard** (`SemaphoreSlim`) to avoid overlapping work runs

Both guards should be non-blocking (no backlog). If a tick arrives while a run/probe is in progress, skip and try again later.

### 5) Current examples in the repo

- `ObservationRollupWorker`
  - probe-driven “active vs degraded” gate via `_isDatabaseAvailable`
  - readiness probe is guarded by `_isInitializing`
  - work run is guarded by `_gate`

- `RawPacketIngestor`
  - uses `_isDatabaseAvailable` and periodic health reporting via a timer (`StartHealthMonitoring`)
  - continues buffering (optional) while degraded

- `*StreamShipper` / `LoggerSQLiteStreamShipper`
  - uses a cancelable loop with `Task.Delay(interval, token)`
  - treats transient SQLite/HTTP errors as expected and simply retries on the next interval

## Timer-driven actions and threading (how `StartAsync()` work happens)

Several functional clients use `System.Threading.Timer` plus `ServiceBase.StartBackground(...)`. It’s easy to get confused about what thread is doing what, so here is the mental model.

### Important primitives

- `System.Threading.Timer`
  - Executes its callback on a **ThreadPool thread**.
  - The callback signature is synchronous (`void Callback(object? state)`), so it cannot be awaited.
  - Callbacks can overlap if the callback takes longer than the timer period.

- `ServiceBase.StartBackground(Func<CancellationToken, Task>)`
  - Schedules the async delegate via `Task.Run(...)`.
  - That work also runs on **ThreadPool threads**.
  - Exceptions are logged and then rethrown on the background task (meaning: they can surface as unobserved task exceptions if nobody awaits the task; `ServiceBase` tracks tasks for disposal waiting).

### Why the pattern is “TimerCallback  StartBackground  async RunOnceAsync”

Because `TimerCallback` itself can’t safely `await`, the callback generally does only cheap checks and then schedules real work:

1. Timer fires on a ThreadPool thread.
2. Callback checks:
   - service cancellation (`LinkedCancellationToken.IsCancellationRequested`)
   - availability gate (`_isDatabaseAvailable`)
3. Callback schedules async work via `StartBackground(ct => RunOnceAsync(ct))`.
4. `RunOnceAsync` performs real async I/O and should be concurrency-guarded (`SemaphoreSlim` try-enter) to prevent overlap.

### Which thread is doing the work?

- The timer callback itself is on a ThreadPool thread.
- The scheduled `StartBackground` work starts on a ThreadPool thread.
- Within `RunOnceAsync`, each `await` may resume on *any* ThreadPool thread (because most code uses `ConfigureAwait(false)`), but it will never resume onto a UI thread unless explicitly marshaled.

### What prevents overlap?

Nothing in `Timer` prevents overlap by default.

Overlap prevention must be explicit:

- Use a `SemaphoreSlim` try-enter (`WaitAsync(0, ct)`) at the beginning of the run.
- If the semaphore is already held, return immediately (skip this tick).

This is the same reason we keep the “run guard” separate from “readiness/probe guard”.

## Conventions (recommended)

- Organize persistence by consumer domain namespace (e.g. `MetWorks.Persistence.Readings`, `MetWorks.Persistence.Rollups`, `MetWorks.Persistence.Logging`).
- Keep SQL strings private to the implementing type.
- Prefer `record` DTOs for persistence exchange types.
- Propagate `CancellationToken` through all async operations.
- Use transactions via `ISqliteSession.ExecuteInTransactionAsync(...)` when multiple statements must be atomic.

## Notes and current limitations

- Rollups is the first slice.
- As we migrate more consumers, update this document with:
  - the new namespace and repository/service interfaces
  - the DTOs added for that slice
  - the DDL scripts and tables owned by that slice
  - any new cross-slice constraints (indexes, retention policy helpers, database size constraints)
