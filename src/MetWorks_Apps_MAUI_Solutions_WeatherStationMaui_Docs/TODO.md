# TODO

This is a living backlog of technical debt, missing features, and architectural follow-ups.

## SQLite data retention / size control (high)

- [ ] Add rollups or another retention solution for non-observation fact tables.
  - Problem: without a retention policy, the local SQLite database can grow without bound.
  - Options:
    - Implement rollups similar to `MetWorks.Persistence.Rollups` for:
      - wind
      - lightning
      - precipitation
    - Or implement a deletion-based retention policy (delete oldest first), ensuring shipping/ack semantics remain correct.

## Tempest REST snapshot persistence to SQLite (high)

- [ ] Persist Tempest Better Forecast raw JSON snapshots to SQLite (instead of `tempest.forecast.snapshot.json`).
  - Forecast payload is large; SQLite retention/purge needs to ship with this.
- [ ] Persist Tempest REST observations raw JSON snapshots to SQLite (instead of `tempest.observations.snapshot.json` + `.meta.json`).
- [ ] Implement purge policy for snapshot tables (delete oldest first).
  - Candidate: keep last N hours/days or last N snapshots.
  - Must run soon after adding snapshot tables to avoid unbounded growth.

## Rollups (follow-ups)

- [ ] Decide whether rollups should run in a single worker or per-table workers.
- [ ] Add settings for rollup enable/interval/batch sizes (if needed).

## DDI / initialization

Consolidated from `ToDo_Candidates.txt`.

### Implemented (for reference)

- [x] Reduce sync-over-async and global init gate behavior (two-phase startup)
  - Notes: `DDI_Changes/ImplementationNotes/DDI_D1_AsyncStartupTwoPhase.md`
- [x] Standardize async guidance (timeouts, cancellation, `ConfigureAwait(false)`)
  - Notes: `DDI_Changes/ImplementationNotes/DDI_D2_AsyncGuidance_CancellationTimeouts.md`
- [x] Generator passes `CancellationToken` values (not `CancellationTokenSource`) and supports defining properties on class declarations
  - Notes: `DDI_Changes/ImplementationNotes/DDI_D3_CancellationTokenValues_AndModelProperties.md`
- [x] Make two-phase init safer by contract (`InitializeAsync` exactly once, use-before-init guards)
  - Notes: `DDI_Changes/ImplementationNotes/DDI_D4_InitOnce_UseBeforeInitGuards.md`

### Remaining ideas

- [ ] Per-service readiness gates (instead of/alongside a single global “all initialized” gate)
  - Candidate: `Task Ready` + `bool IsReady` per long-running service.

## Project dependency hygiene / reference cycles (medium)

- [ ] Identify and eliminate project reference cycle “ugliness” (e.g., `MetWorks_Resource_Store -> MetWorks_Common -> MetWorks_Persistence -> MetWorks_Resource_Store`).
  - Goal: enforce a clean layering where foundational projects never depend on higher-level projects.
  - Deliverables:
    - Document current cycles + why they exist (what types are causing each edge).
    - Decide target layering rules (e.g., `Interfaces/Constants` -> `Common` -> `Data` -> `Persistence` -> `App`).
    - Refactor until `dotnet restore` can’t form cycles.
  - Notes:
    - Prefer moving shared DTOs/contracts “down” rather than pulling persistence “up” into common services.
    - Avoid introducing new interfaces purely to break cycles unless they represent a real contract boundary.
