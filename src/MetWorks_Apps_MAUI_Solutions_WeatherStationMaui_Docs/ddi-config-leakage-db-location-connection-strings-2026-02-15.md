# Inventory: configuration leakage of DB location / connection details

Date: 2026-02-15

Goal: identify where database *location* (`dbPath`) and *connection details* (`connectionString`) are being consumed outside the persistence/data layer, so we can migrate toward a clearer layering model.

This was prompted by recent cleanup in `RawPacketIngestor` (SQLite) to stop reading per-service SQLite settings (connection string, db path, buffering), since those settings have global impact and should be owned centrally.

## What to look for

### “Leakage” definition
A component is considered to be leaking persistence configuration when it:
- reads SQLite/Postgres connection details directly from `ISettingRepository`, or
- branches behavior (enable/disable/degraded mode) based on connection details, or
- constructs provider-specific connection objects directly (e.g., `NpgsqlConnection`, SQLite connection string builder) outside the intended persistence/data layer.

Some exceptions may be acceptable temporarily (e.g., legacy components), but everything should be explicitly tracked.

## Findings (from repo-wide search)

### SQLite: `dbPath`
Search keys included: `SettingConstants.Sqlite_dbPath`, `/services/sqlite/dbPath`, `SqliteGroupSettingsDefinition`.

Files implicated (non-exhaustive; based on search hits):
- `src/MetWorks_Common_Logging/LoggerSQLite.cs`
- `src/MetWorks_Ingest_SQLite/Shipping/LightningStreamShipper.cs`
- `src/MetWorks_Ingest_SQLite/Shipping/LoggerSQLiteStreamShipper.cs`
- `src/MetWorks_Ingest_SQLite/Shipping/StationMetadataStreamShipper.cs`
- `src/MetWorks_Ingest_SQLite/Shipping/WindStreamShipper.cs`
- `src/MetWorks_Ingest_SQLite/StationMetadataIngestor.cs`
- `src/MetWorks_Common/Metrics/MetricsSamplerService.cs`
- Central definitions: `src/MetWorks_Constants/SettingConstants.cs`, `src/MetWorks_Constants/LookupDictionaries.cs`

### SQLite: `connectionString`
Search keys included: `SettingConstants.Sqlite_connectionString`, `/services/sqlite/connectionString`.

Files implicated:
- `src/MetWorks_Common_Logging/LoggerSQLite.cs`
- `src/MetWorks_Ingest_SQLite/Shipping/LightningStreamShipper.cs`
- `src/MetWorks_Ingest_SQLite/Shipping/LoggerSQLiteStreamShipper.cs`
- `src/MetWorks_Ingest_SQLite/Shipping/StationMetadataStreamShipper.cs`
- `src/MetWorks_Ingest_SQLite/Shipping/WindStreamShipper.cs`
- Central definitions: `src/MetWorks_Constants/SettingConstants.cs`

### Postgres: connection strings (multiple groups)
Search keys included: `jsonToPostgreSQL_connectionString`, `NpgsqlConnection`, `/services/metrics/connectionString`.

Files implicated:
- `src/MetWorks_Ingest_Postgres/RawPacketIngestor.cs` (reads connection string, constructs connections)
- `src/MetWorks_Common_Logging/LoggerPostgreSQL.cs`
- `src/MetWorks_Common/Metrics/MetricsSamplerService.cs` (metrics persistence connection string)
- Central definitions: `src/MetWorks_Constants/SettingConstants.cs`, `src/MetWorks_Constants/LookupDictionaries.cs`
- UI usage observed in search results: `src/MetWorks_Apps_MAUI_WeatherStationMaui/Pages/HostPages/MainSwipeHostPage.xaml.cs`

## Categorization (suggested)

### Category A — Persistence/data layer owners (desired end-state)
These are the components that should ultimately own DB location/connection details:
- `MetWorks.Common.Settings.SqliteDatabaseOptionsFactory`
- `MetWorks.Data.Sqlite.SqliteDatabaseOptions`
- `MetWorks.Data.Sqlite.ISqliteDatabase` / `SqliteDatabase`
- (Future) a dedicated `MetWorks_Data_Sqlite` assembly as the only non-interface assembly referencing SQLite provider packages.

### Category B — Consumers that should not read connection details
These should depend on persistence abstractions (repositories/readiness) and not read connection details:
- Ingestors (`MetWorks.Ingest.*`)
- Shippers (`MetWorks.Ingest.SQLite.Shipping.*`)
- Loggers (`MetWorks.Common.Logging.*`) except potentially a bootstrap logger
- Non-persistence services (metrics sampler, transformers, viewmodels/pages)

### Category C — UI / Host pages
UI code should not need to read raw connection strings. If it needs status, it should request a status DTO/service.

## Proposed plan (backlog items)

### 1) Build a definitive list of configuration reads
- For each file above, identify:
  - which setting paths it reads
  - why it reads them (enablement? connection creation? degraded mode?)
  - which persistence abstraction it should depend on instead

### 2) Move “enablement” decisions to a single place
- Decide where “SQLite ingestion enabled/disabled” is owned.
  - Option: persistence readiness returns a clear reason/status (e.g., configured, available, unavailable).
  - Option: introduce a single global setting like `/services/sqlite/enabled` (but avoid duplicating concerns).

### 3) Centralize dbPath + connectionString resolution
- Ensure **only** the persistence/data layer reads:
  - `/services/sqlite/dbPath`
  - `/services/sqlite/connectionString`
  - `/services/sqlite/journalMode`
  - `/services/sqlite/busyTimeoutMs`

### 4) Replace direct setting reads in non-persistence components
Work item template:
- Remove reads of `dbPath` / `connectionString` from <component>
- Replace with dependency on <repo/readiness/provider>
- Ensure degraded mode is based on readiness probing exceptions/status, not config text.

### 5) Add validation + diagnostics
- At startup, log the persistence configuration *once* (sanitized), from the persistence layer.
- Add structured “persistence readiness state” that other services can query/log without re-reading settings.

## Notes on `settings.yaml`
`src/MetWorks_Resource_Store/data/settings.yaml` currently declares both:
- `/services/sqlite/connectionString`
- `/services/sqlite/dbPath`

These definitions should remain for now, but the *read sites* should converge into the persistence/data layer.

