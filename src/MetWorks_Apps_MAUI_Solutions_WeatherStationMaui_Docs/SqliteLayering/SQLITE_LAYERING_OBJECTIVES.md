# SQLite layering objectives (Data layer vs Persistence layer)

This document defines the **target layering** for SQLite usage across this solution.

It is intended to prevent the current pattern where SQLite knowledge is scattered across shippers/ingestors/workers (connection-string building, PRAGMAs, schema ensure, transactions, `SqliteConnection`, etc.).

## Definitions

### Data layer (SQLite access, no application logic)

**Goal:** Provide the smallest set of capabilities needed to interact with SQLite **without leaking** `Microsoft.Data.Sqlite` types into other assemblies.

**Characteristics**
- Owns all references to `Microsoft.Data.Sqlite`.
- Does *not* contain business logic.
- Does *not* know about application concepts like `observation`, `wind`, `metrics_summary`, etc.
- Provides safe primitives for persistence code to run SQL and map results.
- Centralizes:
  - opening connections
  - PRAGMA application / busy handling policy
  - reader/writer behavior decisions (e.g. WAL + busy_timeout)
  - transaction handling

**Hard rule (no-leak):** outside the data layer, no project may reference `Microsoft.Data.Sqlite` or use any `Sqlite*` types.

### Persistence layer (consumer-oriented persistence APIs)

**Goal:** Expose persistence operations grouped by consumer domain (readings, metrics, logging, rollups, shipping) while keeping consumer APIs free of SQL.

**Characteristics**
- Does not reference `Microsoft.Data.Sqlite`.
- Contains SQL strings and mapping logic internally.
- Exposes consumer-facing methods that accept/return **DTOs** (or domain types) and hide SQL.
- Uses the data layer for all DB I/O.

## DTO boundaries

### Business/functional callers → persistence

- Persistence methods exchange **DTOs** with business/functional callers.
- DTOs are allowed to be long-lived and should remain stable.
- DTOs must not expose DB concepts like parameter names, SQL fragments, or connection concerns.

### Persistence → data layer

- Persistence uses **DB interop DTOs** to call into the data layer.
- Example interop concepts:
  - `DbParam` (name/value)
  - `DbRow` (typed column accessor)
  - `SqlScript` (name/sql text)
- These are *not* business DTOs and must not be exposed in consumer-facing persistence APIs.

## DDL ownership (schema)

**Decision:** DDL belongs to the persistence layer.

- Persistence owns the schema scripts (because schema is part of the persistence contract).
- Data layer executes DDL scripts, but does not embed or copy schema knowledge.

### Intended flow

1. A functional class (or an early-start service) triggers readiness:
   - e.g. `EnsureDatabaseReadyAsync(...)`.
2. The persistence layer provides the set of `SqlScript` DDL scripts to execute.
3. The data layer runs them (best-effort if required by the caller) using its standard connection/transaction/PRAGMA behavior.

## Minimal data layer API (starting point)

The data layer should start as a **tiny, purpose-built wrapper** — not a general-purpose database abstraction.

Required initial capabilities (based on current solution usage):
- Execute non-query SQL
- Execute scalar SQL
- Execute query SQL with a mapper callback that produces persistence DTOs
- Run a block of work inside a transaction boundary
- Execute a batch of DDL scripts provided by persistence

Non-goals for the initial shape:
- ORM features
- general-purpose query builders
- schema versioning framework
- pluggable backend support

## Persistence layer organization (by consumer)

Suggested namespaces (adjust naming as needed):
- `MetWorks.Persistence.Readings`
- `MetWorks.Persistence.Rollups`
- `MetWorks.Persistence.Metrics`
- `MetWorks.Persistence.Logging`
- `MetWorks.Persistence.StreamShipping`

Each namespace should expose consumer-facing services/repositories like:
- `IObservationRepository`
- `IObservationRollupRepository`
- `IMetricsSummaryRepository`
- `ILoggerRepository`

## Anti-patterns to avoid

- Building connection strings in shippers/ingestors/workers.
- Calling PRAGMAs from application services.
- Running schema DDL from multiple unrelated services.
- Returning provider types (e.g. `SqliteConnection`, `SqliteDataReader`) from data-layer APIs.
- Catching provider exceptions (`SqliteException`) outside the data layer.

## Migration approach

- Prefer a vertical slice migration:
  1. Create the minimal data layer
  2. Create one persistence namespace + repository
  3. Migrate one consumer (e.g. a rollup worker) fully
  4. Repeat

- Keep the solution build green after each slice.

## Related documents

- `DATA_LAYER_SQLITE.md`
- `PERSISTENCE_LAYER_SQLITE.md`
- `../README.md` (docs folder index)
