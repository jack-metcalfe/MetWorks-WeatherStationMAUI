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

- [ ] Review `ToDo_Candidates.txt` and migrate items that are still relevant into this file.
