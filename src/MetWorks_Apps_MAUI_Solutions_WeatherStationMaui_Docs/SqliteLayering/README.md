# SQLite layering work

This folder collects the “layering” documentation for SQLite in the MAUI Weather Station solution.

## Index

- [Objectives](./SQLITE_LAYERING_OBJECTIVES.md)
- [Data layer](./DATA_LAYER_SQLITE.md)
- [Persistence layer](./PERSISTENCE_LAYER_SQLITE.md)
- [Rollups vertical slice notes](./ROLLUPS_IMPLEMENTATION_PLAN.md)
- [Rollups (wind/precip/lightning) plan](./ROLLUPS_WIND_PRECIP_LIGHTNING_IMPLEMENTATION_PLAN.md)
- [Migration plan (legacy notes)](./sqlite-migration-plan.md)

## Vertical slices (SQLite layering)

Current approach is to migrate SQLite usage via end-to-end “vertical slices”:

- **Rollups** (`MetWorks.Persistence.Rollups`)
  - Readiness: `IRollupsDatabaseReadiness`
  - Operations: `IObservationRollupRepository`
  - Worker: `MetWorks.Ingest.SQLite.Rollups.RollupsWorker`

- **Stream shipping** (`MetWorks.Persistence.StreamShipping`)
  - Readiness: `IStreamShippingDatabaseReadiness`
  - Operations (state + standard readings): `IStreamShippingRepository`
  - Operations (logger): `ILoggerStreamShippingRepository`

As additional slices are migrated, add them here (and update `PERSISTENCE_LAYER_SQLITE.md`).

## Documentation status / drift policy

- Treat `WeatherStationMaui.yaml` as authoritative for runtime wiring (DDI `instance:` names, ordering, and dotted property access).
- Prefer documenting patterns and responsibilities here; when listing wiring details, copy them from `WeatherStationMaui.yaml` and keep them minimal to reduce drift.
- Treat `sqlite-migration-plan.md` as legacy context only; keep it as a pointer to the current docs rather than a living plan.
