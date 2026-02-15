# SQLite layering work

This folder collects the “layering” documentation for SQLite in the MAUI Weather Station solution.

## Index

- [Objectives](./SQLITE_LAYERING_OBJECTIVES.md)
- [Data layer](./DATA_LAYER_SQLITE.md)
- [Persistence layer](./PERSISTENCE_LAYER_SQLITE.md)
- [Rollups vertical slice notes](./ROLLUPS_IMPLEMENTATION_PLAN.md)
- [Migration plan (legacy notes)](./sqlite-migration-plan.md)

## Vertical slices (SQLite layering)

Current approach is to migrate SQLite usage via end-to-end “vertical slices”:

- **Rollups** (`MetWorks.Persistence.Rollups`)
  - Readiness: `IRollupsDatabaseReadiness`
  - Operations: `IObservationRollupRepository`

- **Stream shipping** (`MetWorks.Persistence.StreamShipping`)
  - Readiness: `IStreamShippingDatabaseReadiness`
  - Operations (state + standard readings): `IStreamShippingRepository`
  - Operations (logger): `ILoggerStreamShippingRepository`

As additional slices are migrated, add them here (and update `PERSISTENCE_LAYER_SQLITE.md`).
