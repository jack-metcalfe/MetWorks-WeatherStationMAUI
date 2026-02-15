# Session restore / running log — 2026-02-14

## Goal
Continue SQLite layering migration by removing legacy SQLite persistence usage (`MetWorks_Persistence_SQLite`) and moving functional components to the new persistence + data layer pattern.

## Functional areas touched

### Dead code / cleanup
- Removed legacy stream-shipping helper files that were present on disk but not included in `MetWorks_Ingest_SQLite` project builds:
  - `src/MetWorks_Ingest_SQLite/Shipping/StandardReadingsStreamShipping.cs`
  - `src/MetWorks_Ingest_SQLite/Shipping/StationMetadataStreamShipping.cs`
  - `src/MetWorks_Ingest_SQLite/Shipping/LoggerSQLiteStreamShipping.cs`

### Logging
- Migrated `LoggerSQLite` (write path) to persistence + data layer.
  - `MetWorks.Common.Logging.LoggerSQLite` now depends on `MetWorks.Persistence.Logging`.
  - Added `MetWorks.Persistence.Logging` slice:
    - `ILoggingDatabaseReadiness` / `LoggingDatabaseReadiness`
    - `ILoggerSqliteRepository` / `LoggerSqliteRepository`
    - `LoggerSqliteLogEvent` DTO
    - `LoggingSqlScripts` DDL
  - Updated `MetWorks_Common_Logging` to reference `MetWorks_Persistence` instead of `MetWorks_Persistence_SQLite`.

### Ingest (SQLite)
- Migrated `RawPacketIngestor` to persistence + data layer.
  - Added `MetWorks.Persistence.Ingest` slice:
    - `IRawPacketDatabaseReadiness` / `RawPacketDatabaseReadiness`
    - `IRawPacketIngestRepository` / `RawPacketIngestRepository`
    - `RawPacketRecord` DTO
    - `RawPacketSqlScripts` loads DDL from `MetWorks_Resource_Store` embedded resources (`Ingest/SQLite/*.sql`).

- Migrated `StationMetadataIngestor` to persistence + data layer.
  - Added `MetWorks.Persistence.StationMetadata` slice:
    - `IStationMetadataDatabaseReadiness` / `StationMetadataDatabaseReadiness`
    - `IStationMetadataRepository` / `StationMetadataRepository`
    - `StationMetadataInsertRow` DTO
    - `StationMetadataSqlScripts` DDL

- Migrated `MetricsSummaryIngestor` to persistence + data layer.
  - Added `MetWorks.Persistence.Metrics` slice:
    - `IMetricsDatabaseReadiness` / `MetricsDatabaseReadiness` (validated dynamic table names)
    - `IMetricsSummaryRepository` / `MetricsSummaryRepository`
    - `MetricsSummaryInsertRow` DTO
    - `MetricsSqlScripts` DDL generator

### SQLite initialization / feature probing
- Retired ingest-local schema initializer and moved SQLite runtime feature probing to the data layer:
  - Deleted `src/MetWorks_Ingest_SQLite/Initializer.cs` (DDL belongs in persistence readiness slices now).
  - Deleted `src/MetWorks_Ingest_SQLite/SqliteFeatureProbe.cs`.
  - Added `src/MetWorks_Data_Sqlite/SqliteFeatureProbe.cs` (provider-isolated) with `SupportsGeneratedColumnsAsync(connectionString, ct)`.

### Rollups + stream shipping state
- Removed ingest-local, provider-typed state stores that were duplicating persistence/data-layer capabilities:
  - Deleted `src/MetWorks_Ingest_SQLite/Rollups/RollupWatermarkStore.cs` (watermarks are managed in `MetWorks.Persistence.Rollups.RollupWatermarkStore` and used by `ObservationRollupRepository`).
  - Deleted `src/MetWorks_Ingest_SQLite/Shipping/ShipperStateStore.cs` (shipper state is managed by `MetWorks.Persistence.StreamShipping.StreamShippingRepository` + `StreamShippingDatabaseReadiness`).

### DDI / generated registry glue
- Updated `MetWorks_DdiRegistry` generated glue (`*.g.cs`) to create and inject new persistence readiness/repository instances for:
  - logging
  - raw packet ingest
  - station metadata ingest
  - metrics summary ingest

- Removed remaining generated references to legacy `MetWorks.Persistence.SQLite.*` DDI instances:
  - Remapped `TheDefaultPlatformPaths` to `MetWorks.Common.DefaultPlatformPaths`.
  - Removed `TheSqliteBootstrapper` and `TheSqliteWriteService` from generated registry flow (`Registry.g.cs`, `Accessors.g.cs`, `ExposeToMauiDi.g.cs`) and deleted their generated instance files.
  - Removed `src/MetWorks_Persistence_SQLite/MetWorks_Persistence_SQLite.csproj` from the solution; build stayed green.

## Notes / current constraints
- `MetWorks_Ingest_SQLite` no longer needs a direct `Microsoft.Data.Sqlite` global using; remaining SQLite provider usage is isolated to `MetWorks_Data_Sqlite`.
- `rg` (ripgrep) works outside Visual Studio but not in the VS terminal (likely PATH/session). Workspace search tools were used instead.

## Follow-up cleanup
- Removed `global using Microsoft.Data.Sqlite;` from `src/MetWorks_Ingest_SQLite/GlobalUsings.cs`.
- Removed legacy project reference `MetWorks_Persistence_SQLite` from `src/MetWorks_Ingest_SQLite/MetWorks_Ingest_SQLite.csproj`.

## Build status
- `dotnet build` succeeded at the end of the session.
