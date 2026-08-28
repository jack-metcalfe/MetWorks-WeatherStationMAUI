# Dependency Cleanup

This document proposes concrete dependency-cleanup work to reduce coupling between projects and minimize public API surfaces. It is the actionable companion to `SOLUTION_ARCHITECTURE.md` (current state) and `MULTI_SOLUTION_EVALUATION.md` (solution-split evaluation).

Items are grouped by effort and priority.

---

## 1. Layering violations to fix

These are cross-tier references identified in `SOLUTION_ARCHITECTURE.md`. Each one leaks a concern from a higher tier into a lower-tier project, making the lower-tier project harder to test and reuse.

### L1 — `MetWorks_Common` → `Npgsql`

**Problem:** The `Npgsql` NuGet package is a direct dependency of `MetWorks_Common`. This makes every consumer of `MetWorks_Common` transitively depend on the PostgreSQL driver, even on Android where PostgreSQL is never used.

**Root cause:** `TempestRestClient` or `StationMetadataProvider` in `MetWorks_Common` likely contains Postgres-specific code that does not belong there.

**Proposed fix:**
1. Identify which classes in `MetWorks_Common` actually import `Npgsql` namespaces.
2. Move any Postgres-specific code into `MetWorks_Ingest_Postgres` (where `Npgsql` already belongs).
3. Remove the `<PackageReference Include="Npgsql" />` from `MetWorks_Common.csproj`.

**Expected benefit:** Android and test builds stop pulling in the Postgres driver (~3 MB).

---

### L2 — `MetWorks_Common_Settings` → `Microsoft.Maui.Controls`

**Problem:** The settings library has a hard dependency on `Microsoft.Maui.Controls`. This means any project that consumes `MetWorks_Common_Settings` (including test projects) requires the MAUI SDK workload to be installed.

**Root cause:** `MetWorks_Common_Settings` likely uses MAUI types (`Application.Current`, `IAppInfo`, or platform path APIs) directly instead of behind the `IPlatformPaths` abstraction.

**Proposed fix:**
1. Audit `MetWorks_Common_Settings` for MAUI type usage.
2. Move MAUI-specific logic into `MetWorks_Maui_Services` or a new thin adapter class.
3. Pass platform-specific values to `SettingRepository` through constructor parameters or the `InitializeAsync` call rather than resolving them directly.
4. Remove `<PackageReference Include="Microsoft.Maui.Controls" />` from `MetWorks_Common_Settings.csproj`.

**Expected benefit:** Settings library becomes testable in a plain `net10.0` test project without installing the MAUI workload.

---

### L3 — `MetWorks_Common_Settings` → `MetWorks_Data_Sqlite`

**Problem:** The settings library reaches directly into the SQLite data layer (`MetWorks_Data_Sqlite`) to load or persist settings. This creates a downward reference from Tier 2 into Tier 4, preventing the settings layer from being unit tested without a SQLite database.

**Proposed fix:**
1. Introduce a thin `ISettingStorage` abstraction (one or two methods: `ReadAsync`, `WriteAsync`) in `MetWorks_Interfaces` or in `MetWorks_Common_Settings` itself.
2. Have `SettingRepository` consume `ISettingStorage` via `InitializeAsync`.
3. Provide the SQLite-backed implementation of `ISettingStorage` in `MetWorks_Data_Sqlite` or `MetWorks_Persistence_SQLite`.
4. Remove the `<ProjectReference>` to `MetWorks_Data_Sqlite` from `MetWorks_Common_Settings.csproj`.

**Expected benefit:** `MetWorks_Common_Settings` becomes independently testable with a stub storage.

---

### L4 — `MetWorks_Common_Logging` → `MetWorks_Persistence`

**Problem:** The logging project depends on the persistence project (`MetWorks_Persistence`), which itself depends on `MetWorks_InstanceIdentifier` → `MetWorks_Common_Settings` → `MetWorks_Data_Sqlite`. This creates a deep transitive chain where logging pulls in the entire settings and data stack.

The natural dependency direction should be reversed: persistence services should *use* logging, not the other way around.

**Proposed fix:**
1. Extract the `LoggerSQLite` class (the only class in `MetWorks_Common_Logging` that likely needs persistence) into a separate project (e.g., `MetWorks_Logging_SQLite` in Tier 4 or 5).
2. Keep `MetWorks_Common_Logging` (`LoggerStub`, `LoggerFile`, `LoggerResilient`, `ContextualLogger`) free of persistence references.
3. Update DDI wiring in `WeatherStationMaui.yaml` to reference the new project for `LoggerSQLite`.

**Expected benefit:** `MetWorks_Common_Logging` becomes a lightweight dependency usable in the domain tier without pulling in SQLite or settings.

---

### L5 — `MetWorks_Resource_Store` → `MetWorks_Common`

**Problem:** `MetWorks_Resource_Store` (embedded SQL schema files and settings YAML) depends on `MetWorks_Common`. This creates a transitive chain: any project needing embedded resources also pulls in the full common library (including `Npgsql` via L1).

**Proposed fix:**
1. Determine which `MetWorks_Common` types are actually used in `MetWorks_Resource_Store`.
2. If the usage is minimal (e.g., only `Common_Utility` helpers), replace the `MetWorks_Common` reference with a `MetWorks_Common_Utility` reference.
3. Remove the `<ProjectReference>` to `MetWorks_Common` from `MetWorks_Resource_Store.csproj`.

**Expected benefit:** `MetWorks_Resource_Store` becomes a low-level library (Tier 3 or 4) with no Postgres or HTTP transitive dependencies.

---

### L6 — `MetWorks_Common_Logging` → `Npgsql` + Serilog PostgreSQL sinks

**Problem:** Even if L4 is fixed, `MetWorks_Common_Logging` still pulls in `Npgsql` and two Serilog PostgreSQL sinks for the `LoggerSQLite` / `LoggerResilient` PostgreSQL path.

**Proposed fix:** Same as L4 — moving `LoggerSQLite` (and any Postgres sink configuration) into a separate `MetWorks_Logging_SQLite` or `MetWorks_Logging_Postgres` project eliminates these from the base logging library.

---

## 2. Migrate `MetWorks_Persistence_SQLite` (legacy)

`MetWorks_Persistence_SQLite` is explicitly marked as legacy (see `SOLUTION_ARCHITECTURE.md`). Its functionality is being migrated into `MetWorks_Data_Sqlite` + `MetWorks_Persistence`.

**Migration steps:**
1. Identify any types in `MetWorks_Persistence_SQLite` not yet represented in `MetWorks_Data_Sqlite` or `MetWorks_Persistence`.
2. Move them one at a time, updating references in consuming projects.
3. Once no project references `MetWorks_Persistence_SQLite`, remove the project from the solution.

**Milestone:** When `MetWorks_Persistence_SQLite` is removed, all SQLite persistence code lives in the `MetWorks_Data_Sqlite` assembly as the sole non-interface assembly that references `Microsoft.Data.Sqlite`.

---

## 3. Public API surface minimization

### 3a — Use `internal` for implementation types

Many implementation classes are currently `public` by default but are only ever consumed within their own project or via an interface. Making them `internal` reduces the public API surface and prevents accidental coupling.

**Candidates:**

| Project | Type pattern | Suggested visibility |
|---------|-------------|---------------------|
| `MetWorks_IoT_UDP_Tempest` | DTO types (`WindDto`, `ObservationDto`, …) | `internal` — consumed only by `PacketFactory` in the same project |
| `MetWorks_IoT_UDP_Tempest` | `PacketFactory`, `RawPacketRecordTypedFactory` | `internal` — only `MetWorks_Networking_Udp_Transformer` uses these |
| `MetWorks_Common` | Concrete implementations of `IReading*` subtypes | `internal` — consumers depend on the interface |
| `MetWorks_Common_Logging` | `LoggerStub`, `ContextualLogger` internals | `internal` where not required by DDI interface binding |
| `MetWorks_Ingest_SQLite` | Internal rollup and shipping worker classes | `internal` |
| `MetWorks_Data_Sqlite` | `DbConnectionFactory` or equivalent | `internal` |

**Action:** Run a project-by-project audit (`grep -r "^public class\|^public sealed" src/<project>`) and change any type that:
- is not referenced by another project, **and**
- is not used as a DDI service interface implementation that requires a public constructor

from `public` to `internal`.

### 3b — Remove or consolidate single-implementation interfaces

Some interfaces exist for historical reasons but have only one implementation and are not used as a substitution point. The `INTERFACE_CATALOG.md` already identifies candidates. Below is an actionable subset ordered by effort.

| Interface | Action | Rationale |
|-----------|--------|-----------|
| `IPacketFactory` | Remove; use `PacketFactory` directly | Static-forwarding interface; no real substitution |
| `IRawPacketRecordTypedFactory` | Remove; use `RawPacketRecordTypedFactory` directly | Same static-forwarding pattern |
| `IContentVariantCatalog` | Consider making concrete | Only used internally within the MAUI project |
| `IContentViewFactory` | Consider making concrete | Same — local factory |
| `IHostCompositionCatalog` | Consider making concrete | Verify implementation count first |
| `IDeviceOverrideSource` | Keep if alternate sources (remote, JSON) are planned; else make concrete | One implementation |

> Before removing any interface, verify it is not referenced in `WeatherStationMaui.yaml` (DDI wiring requires the interface) and not used as a MAUI DI registration boundary.

### 3c — `InternalsVisibleTo` for test access

Rather than keeping implementation types public solely for testing, use `[assembly: InternalsVisibleTo("...")]` in each production project to expose internals to the corresponding test project only.

Add to each project that has a corresponding test project:
```csharp
// In AssemblyInfo.cs or GlobalUsings.cs of the production project:
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("MetWorks.Common.Tests")]
```

This preserves testability while hiding implementation details from other callers.

---

## 4. `MetWorks_Common` decomposition

`MetWorks_Common` is a wide project that combines several distinct concerns:

| Class group | Concern | Target project |
|-------------|---------|---------------|
| `ServiceBase` | Service lifecycle base class | Keep in `MetWorks_Common` (or move to `MetWorks_Interfaces` as an abstract class) |
| `TempestRestClient` | HTTP REST I/O | Keep in `MetWorks_Common`; ensure `Npgsql` is removed (L1) |
| `TempestForecastProvider` | Forecast polling | Keep in `MetWorks_Common` |
| `TempestWebSocketObservationsProvider` | WebSocket I/O | Keep in `MetWorks_Common` |
| `StationMetadataProvider` | Station metadata + REST fetch | Keep in `MetWorks_Common` |
| Provenance types (`ReadingProvenance`, `ProvenanceStep`, …) | Data lineage | Consider moving to `MetWorks_Models_Observables` |
| Any Postgres-specific code (see L1) | Postgres I/O | Move to `MetWorks_Ingest_Postgres` |

The immediate priority is L1 (remove `Npgsql`). Full decomposition of `MetWorks_Common` is a larger refactor that should follow once L1–L4 are resolved.

---

## 5. Priority order

| # | Item | Effort | Impact |
|---|------|--------|--------|
| 1 | Fix L4: extract `LoggerSQLite` from `MetWorks_Common_Logging` | Medium | Removes deep transitive chain from logging into persistence |
| 2 | Fix L1: remove `Npgsql` from `MetWorks_Common` | Small | Cleans up Android/test builds |
| 3 | Fix L6: remove Postgres sinks from base logging project | Small (follows from item 1) | Keeps logging library portable |
| 4 | Fix L2: remove `Microsoft.Maui.Controls` from `MetWorks_Common_Settings` | Medium | Enables settings unit tests without MAUI workload |
| 5 | Fix L5: replace `MetWorks_Common` reference in `MetWorks_Resource_Store` with `MetWorks_Common_Utility` | Small | Breaks Npgsql transitive dep via resource store |
| 6 | Fix L3: introduce `ISettingStorage` to decouple settings from SQLite | Large | Enables settings unit testing; improves layering |
| 7 | Migrate and remove `MetWorks_Persistence_SQLite` | Medium | Removes legacy project |
| 8 | Apply `internal` to implementation types (3a) | Small per project | Reduces public surface; prevents accidental coupling |
| 9 | Remove single-implementation static-forwarding interfaces (3b) | Small | Reduces interface count |
| 10 | Add `InternalsVisibleTo` attributes (3c) | Small | Enables `internal` without losing test coverage |
