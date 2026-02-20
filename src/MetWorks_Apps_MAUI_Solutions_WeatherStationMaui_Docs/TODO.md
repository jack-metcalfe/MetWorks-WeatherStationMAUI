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
