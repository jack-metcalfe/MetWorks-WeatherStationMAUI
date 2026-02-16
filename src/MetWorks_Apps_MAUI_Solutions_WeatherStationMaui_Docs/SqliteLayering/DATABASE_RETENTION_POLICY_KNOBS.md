# Database retention policy knobs (raw facts + rollups)

This document is a companion to:

- `SqliteLayering/DATABASE_MAINTENANCE_COORDINATION.md`

It focuses on **what we retain** and **what knobs we want**, not the coordination protocol.

## Goals

- Prevent uncontrolled growth of the local SQLite database.
- Prefer preserving the **last N hours/days** (delete oldest first).
- Keep maintenance work bounded to reduce lock contention.
- Make storage behavior tunable without code changes.

## Non-goals

- A full schema migration/versioning system.
- A user-facing UI for retention tuning (initially).
- Perfect preservation of every raw event (some sources may be lossy by design).

## Why retention is required

Rollups improve query speed but do not cap storage:

- Raw fact tables grow unbounded unless old rows are purged.
- Rollup tables also grow unbounded unless buckets are purged.

If we want the DB to have a stable upper bound over time, we need explicit retention.

## Tables and suggested default retention windows

The values below are *starting points* and should be treated as knobs.

### Raw fact tables (shorter retention)

- `observation`
  - Suggestion: keep last **48–168 hours** (2–7 days)
  - Rationale: high value but relatively high volume

- `wind`
  - Suggestion: keep last **48–168 hours**
  - Rationale: higher rate than observation; rollups provide most UX value

- `lightning`
  - Suggestion: keep last **30–180 days** (or longer)
  - Rationale: typically sparse; low storage pressure

- `precipitation`
  - Suggestion: keep last **30–180 days**
  - Rationale: event-like; likely sparse

### Rollup tables (longer retention)

- `observation_rollup_1h`
  - Suggestion: keep last **90–365 days**

- `observation_rollup_1d`
  - Suggestion: keep last **2–10 years** (or “forever” if acceptable)

- `wind_rollup_1h`
  - Suggestion: keep last **90–365 days**

- `wind_rollup_1d`
  - Suggestion: keep last **2–10 years**

- `lightning_rollup_1d`
  - Suggestion: keep last **2–10 years**

## Retention semantics

### Basis for age

Use device-time epochs (already present in the schema):

- Raw facts: `device_received_utc_timestamp_epoch`
- Rollups: `bucket_start_epoch`

Retention should delete rows **older than a cutoff epoch**.

### Oldest-first

Deletion should remove the oldest rows first and be chunked.

Preferred patterns:

- `DELETE FROM <table> WHERE <epoch> < @cutoffEpoch LIMIT @maxRows`
- OR a two-step delete by `rowid` if needed for indexing/efficiency.

### Installation scoping

Most fact tables are keyed by `installation_id`. Retention should be scoped by installation when possible.

## Maintenance work budgeting

To reduce lock contention, retention should be bounded:

- Max rows deleted per tick per table
- Max buckets deleted per tick per rollup table
- Max total time spent per maintenance pass

A useful split:

- "frequent light" retention: runs every ~30–60 seconds; deletes small batches
- "rare heavy" maintenance: manual or infrequent; may include `VACUUM`

## Interaction with rollup watermarks

Retention must not break rollup progression:

- `rollup_state` watermarks are a coordination mechanism, not a storage strategy.
- If raw facts are retained for a shorter window than rollups, rollups should remain valid because they are stored independently.

Edge case:

- If raw facts are deleted *before* being rolled up (e.g., due to aggressive raw retention), rollup completeness is reduced.
- Mitigation: choose raw retention windows comfortably longer than the rollup catch-up horizon, or ensure rollups run frequently.

## Logging as a growth driver

SQLite growth may be dominated by logging tables depending on verbosity.

Practical operator actions:

- Reduce log verbosity.
- Apply retention to log tables.
- Consider lossy logging policies (drop oldest, ring buffer) if needed.

## Proposed configuration knobs (shape)

This is a suggested model for configurable settings; exact settings paths should follow repo conventions.

- Raw facts retention windows (per table or per group)
  - `retention/raw/observation_hours`
  - `retention/raw/wind_hours`
  - `retention/raw/lightning_days`
  - `retention/raw/precipitation_days`

- Rollup retention windows
  - `retention/rollups/observation_1h_days`
  - `retention/rollups/observation_1d_days`
  - `retention/rollups/wind_1h_days`
  - `retention/rollups/wind_1d_days`
  - `retention/rollups/lightning_1d_days`

- Work budget
  - `retention/max_rows_per_tick`
  - `retention/max_buckets_per_tick`
  - `retention/tick_seconds`

## Open questions

- Should log tables be covered by the same retention worker, or have a separate retention worker?
- Do we want a maximum DB size trigger, or purely time-window-based retention?
- How should buffering behave during maintenance mode for each writer service (in-memory vs disk vs drop)?
