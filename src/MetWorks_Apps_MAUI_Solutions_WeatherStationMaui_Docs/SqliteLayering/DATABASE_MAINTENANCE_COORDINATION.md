# Database maintenance coordination (pause/resume + retention)

This document captures a proposed design for coordinating **SQLite database maintenance** across multiple long-running services in the MAUI host process.

## Problem statement

SQLite is effectively a **single-writer** database.

This solution has multiple background services that can write to SQLite (ingestors, rollups, shippers, logging). Some maintenance operations are disruptive:

- Retention deletes (oldest-first purging)
- Index maintenance / `PRAGMA optimize`
- Compaction (`VACUUM`) (rare, expensive)

We want a way to:

1. Request that some or all services **pause their SQLite writes**.
2. Allow those services to **buffer** work (in memory or disk) when practical.
3. Run maintenance with bounded work to avoid long-running locks.
4. Tell services to **resume** and flush buffered work in bounded batches.

This must be done without leaking SQLite provider types into functional code.

## Design objectives

- Prefer a single, shared choke point for writes (ideal end state): `SqliteWriteCoordinator` gates *all* SQLite write operations.
- Use event-driven messaging as the canonical communication mechanism: `MetWorks.Interfaces.IEventRelayBasic`.
- Keep maintenance best-effort and resilient: services may be offline, busy, or already degraded.
- Avoid long locks: maintenance operations must be **chunked**.

## Proposed approach

### Option A: EventRelay maintenance messaging (recommended)

Introduce two messages published via `IEventRelayBasic`:

- `DatabaseMaintenanceBeginMessage`
  - Identifies a maintenance window.
  - Announces intent: "pause writes".
- `DatabaseMaintenanceEndMessage`
  - Ends the window.
  - Announces intent: "resume writes".

Services that write to SQLite subscribe to these messages.

#### Optional acknowledgements

If desired, acknowledgements can be implemented to coordinate more tightly:

- `DatabaseMaintenanceAckMessage`
  - `maintenanceId`
  - `serviceName`
  - `state` (`Paused` | `AlreadyDegraded` | `UnableToPause`)

The orchestrator waits for acks for a short time (e.g., 1–2 seconds) then proceeds anyway.

### Option B: Central write gate (`SqliteWriteCoordinator`) (target end state)

If the codebase routes all SQLite writes through a single coordinator, maintenance mode becomes a property of that coordinator:

- Maintenance enters a state that blocks or defers new write permits.
- Services need minimal bespoke logic.

This is the simplest model operationally, but requires write-path convergence.

### Option C: Opportunistic deletes on each DB call (stopgap)

"Hijack" normal database calls to perform a small number of deletes before/after each transaction.

This can be cheap to implement but is discouraged because it:

- makes latency unpredictable for unrelated operations
- complicates transaction semantics
- increases lock contention at peak write times

## Service behavior during maintenance

Services generally have one of these policies:

- **Pause without buffering**: stop writes, retry later (common for shippers/rollups)
- **Pause with bounded buffering**: keep a capped queue and drop/compact when full (common for high-rate ingestors/logging)
- **Already degraded**: if DB is already unavailable, treat as paused and ack accordingly

The recommended implementation is to reuse the existing "DB unavailable/degraded" behaviors where possible so that the maintenance path exercises and hardens those code paths.

## Maintenance operations

### Retention (frequent, bounded)

- Oldest-first deletes from raw fact tables and rollup tables.
- Deletes are chunked: `DELETE ... LIMIT @maxRows` (or equivalent strategy).
- Run on a timer, but only do small work units per tick.

### Compaction (rare)

- `VACUUM` can be used to reclaim free pages, but it is expensive.
- Prefer a separate, explicit mode (manual trigger or infrequent schedule) that uses maintenance coordination and longer pause windows.

## Suggested sequencing

1. Decide on the communication mechanism (EventRelay begin/end, with optional ack).
2. Add retention deletes as a bounded maintenance operation.
3. Gradually converge all write paths through `SqliteWriteCoordinator` so that maintenance can become a single global gate.

## Notes

- This document defines a coordination protocol only.
- It intentionally does not prescribe retention windows or table-specific policies; those belong in a retention plan/settings document.
