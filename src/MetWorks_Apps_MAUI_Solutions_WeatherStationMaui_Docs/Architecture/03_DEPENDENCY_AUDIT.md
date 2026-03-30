# Dependency Audit

> **Purpose**: Catalog specific dependency problems, their impact, and recommended remediation.

---

## F1: ✅ RESOLVED — Common_Settings no longer depends on Microsoft.Maui.Controls

> **Status**: Fixed via simplification proposals D3 (removed MAUI package + dead `#if MAUI` blocks) and S3 (injected `IPlatformPaths` to eliminate duplicated path logic).

### Original Problem
`MetWorks_Common_Settings` referenced `Microsoft.Maui.Controls`, coupling a shared settings library to the MAUI platform SDK.
- Any non-MAUI host (console tool, web service, test project) that needs settings must pull in the entire MAUI SDK.
- It prevents reuse of the settings layer in server-side or build-time contexts.
- It creates a platform lock-in for what should be a platform-agnostic concern.

### Root Cause
The settings library uses MAUI `Preferences` API for device-local persistence. The MAUI dependency was added to access `Microsoft.Maui.Storage.Preferences` directly rather than through an abstraction.

### Impact
- **High**: Blocks any non-MAUI consumer from using the settings system.
- Common_Settings is referenced by DdiRegistry, which is referenced by the MAUI app — so the coupling propagates upward.

### Recommended Remediation
1. Extract a platform-agnostic `IPreferencesProvider` interface into `MetWorks_Interfaces`.
2. Move the MAUI `Preferences` bridge implementation to `MetWorks_Maui_Services` (or a new `MetWorks_Settings_Maui` project).
3. Remove the `Microsoft.Maui.Controls` reference from `MetWorks_Common_Settings`.
4. Inject `IPreferencesProvider` via DDI or constructor DI.

---

## F2: ✅ RESOLVED — Npgsql removed from Common

> **Status**: Fixed via simplification proposals D1 (removed Npgsql from Common) and D2 (removed Npgsql from Common_Logging). No source files in either project referenced Npgsql.

### Original Problem
`MetWorks_Common` referenced `Npgsql` (PostgreSQL client), but this library is intended to be a shared utility layer used by all consumers. A database client in "Common" is a layering violation.

### Root Cause
Likely a metrics persistence or logging sink that targets PostgreSQL was added directly into the Common library rather than into a dedicated persistence project.

### Impact
- **Medium**: Every project that depends on Common transitively pulls in PostgreSQL client assemblies, even if they never connect to PostgreSQL.
- Increases deployment size unnecessarily.
- Violates the principle that shared/common libraries should be infrastructure-agnostic.

### Recommended Remediation
1. Identify the specific class(es) in Common that use Npgsql.
2. Move them to `MetWorks_Persistence` or a new `MetWorks_Persistence_Postgres` project.
3. Remove the `Npgsql` reference from Common.
4. Wire the moved class via DDI or constructor DI.

---

## F3: ✅ RESOLVED — Common_Logging dependency footprint rationalized

> **Status**: All unnecessary dependencies removed. Npgsql removed (D2). `MetWorks_Common` reference removed (was dead — zero types used) and replaced with direct `MetWorks_ServiceBase` reference. Bootstrap blind spot eliminated (LoggerStub A+B: Debug.WriteLine + buffer with drain into LoggerResilient). Dead `ILoggerBase.cs` removed. Remaining reference to `MetWorks_Persistence` is **intentional by design** (SQLite sink + stream-shipping pipeline).

### Original Problem
`MetWorks_Common_Logging` depends on:
- `Npgsql` (PostgreSQL client)
- `Serilog` + `Serilog.Sinks.File`
- `MetWorks_Persistence` (the full persistence layer)
- `MetWorks_Common` (which itself brings in Npgsql — see F2)
- `MetWorks_InstanceIdentifier`
- `MetWorks_Constants`

For a logging library, this is an unusually heavy dependency footprint.

### Root Cause
Multi-sink logging (file, SQLite, PostgreSQL, resilient) was implemented directly in the logging library, pulling in persistence and database client dependencies.

### Impact
- **Medium-High**: Any project that needs logging must accept the entire persistence + database stack.
- Creates circular-like dependency pressure (logging needs persistence; persistence may need logging).
- Makes it difficult to use logging in lightweight contexts (tests, tools).

### Current Dependencies (Post-Remediation)
| Ref | Purpose | Weight |
|-----|---------|--------|
| Interfaces | `ILogger`, `ILoggerStub`, `ILoggerResilient`, `ILoggerSQLite`, `ISettingRepository` | Leaf ✅ |
| Constants | Setting keys (`SettingConstants`, `LookupDictionaries`, `DatabaseConstants`) | Leaf ✅ |
| Common_Utility | `NullPropertyGuard`, `SqliteWriteCoordinator`, `DefaultPlatformPaths` | Leaf ✅ |
| ServiceBase | `ServiceBase` for `LoggerResilient` | Lightweight ✅ |
| InstanceIdentifier | Installation ID for log context enrichment | Lightweight ✅ |
| Persistence | `ILoggingDatabaseReadiness`, `ILoggerSqliteRepository` (SQLite sink) | **Intentional** — by design |
| Serilog + Serilog.Sinks.File | File sink implementation | NuGet packages ✅ |

### Future Consideration
Rename `ILogger` → `IMetLogger` to eliminate disambiguation with `Serilog.ILogger` and `Microsoft.Extensions.Logging.ILogger`. Currently mitigated via `global using ILogger = MetWorks.Interfaces.ILogger;` in Common_Logging and fully-qualified names in ~12 other files.

### Design Intent
The Common_Logging → Persistence dependency is **intentional**, not accidental:
- **SQLite is the default local sink** — logging rows persist locally in SQLite, consistent with the rest of the app's local data storage.
- **Remote batching** reuses the same stream-shipping pipeline as weather data — log rows are batched and shipped alongside observation/wind/lightning data, not via a separate remote logging mechanism. This was a deliberate choice for coordinated, consistent batching.
- **File sink is supplementary and independent** — useful for `adb` access on Android release builds where other debug output may not be available. It should work whether SQLite logging is active or not.
- **LoggerResilient** orchestrates which sinks are active based on settings.

**F3 remediation should preserve the SQLite + stream-shipping pipeline.** The goal is to make the file-only path lighter (no Persistence dependency needed for just file logging) while keeping the SQLite sink wired through Persistence as it is today. A possible approach: split Common_Logging into a lightweight core (file sink + LoggerStub) and a SQLite sink module that references Persistence.

---

## F4: DdiRegistry is a "god aggregator" — ACCEPTED CONSTRAINT

> **Status**: All 9 direct references verified as required (each provides at least one `new()`'d type). No stale references. Accepted as an inherent property of single-registry DDI design. Transitive fragility identified as the one actionable improvement (DDI tooling enhancement).

### Problem
`MetWorks_DdiRegistry` has **9 direct project references** and `new()`s **55 concrete types** — 28 from the 9 direct references and 27 from **transitive** dependencies.

#### Direct References (all required — each provides ≥1 `new()`'d type)
| # | Reference | Types `new()`'d |
|---|-----------|----------------|
| 1 | Common | 6 (`StationMetadataProvider`, `TempestRestClient`, `TempestForecastProvider`, `TempestRestObservationsProvider`, `TempestWebSocketObservationsProvider`, `StreamShippingHttpClientProvider`) |
| 2 | Common_Logging | 4 (`LoggerFile`, `LoggerResilient`, `LoggerSQLite`, `LoggerStub`) |
| 3 | Common_Settings | 2 (`SettingProvider`, `SettingRepository`) |
| 4 | Ingest_SQLite | 10 (all ingestors + shippers + `RollupsWorker`) |
| 5 | Ingest_Transformer | 2 (`SensorReadingTransformer`, `WeatherReadingMux`) |
| 6 | InstanceIdentifier | 1 |
| 7 | Maui_Services | 1 (`TempestOAuthTokenProvider`) |
| 8 | Networking_Udp_Transformer | 1 (`TempestPacketTransformer`) |
| 9 | RedStar_WeatherExtensions | 1 (`UnitsOfMeasureInitializer`) |

#### Transitive Dependencies (27 types `new()`'d with no direct reference)
| Project | Types | Arrives via |
|---------|-------|-------------|
| **Persistence** | **17** (repositories, database readiness, stream shipping) | Common_Logging, Ingest_SQLite |
| **Data_Sqlite** | 3 (`SqliteDatabase`, `SqliteDatabaseOptions`, `SqliteDatabaseOptionsFactory`) | Ingest_SQLite → Persistence |
| **EventRelay** | 2 (`EventRelayBasic`, `EventRelayPath`) | Common |
| **Common_Utility** | 2 (`DefaultPlatformPaths`, `SqliteWriteCoordinator`) | Common |
| **Common_Metrics** | 2 (`MetricsLatestSnapshotStore`, `MetricsSamplerService`) | Ingest_SQLite |
| **ServiceBase** | 1 (`ProvenanceTracker`) | Common |

### Root Cause
DdiRegistry is generated code. The MSBuild target `GenerateDdiRegistryCode` generates C# classes that `new()` concrete types from across the entire solution. To compile, the registry project must reference every project whose types it instantiates.

### Impact
- **Accepted**: The MAUI app genuinely needs all ~25 projects at runtime. DdiRegistry isn't inflating the dependency graph — it's honestly reflecting the app's true footprint.
- Build times: a change in any referenced project triggers recompilation of DdiRegistry, but the generated code is trivial (no complex logic), so incremental builds are fast.
- **Transitive fragility**: The real risk is that 27 types are `new()`'d from projects the registry doesn't directly reference. If any upstream project restructures its references, the registry silently breaks.

### Recommended Remediation: DDI `assembly-map` (Tooling Enhancement)

**Add an optional `assembly-map:` section to the DDI YAML** that maps namespaces to project paths:

```yaml
assembly-map:
  MetWorks.Persistence:        ../MetWorks_Persistence/MetWorks_Persistence.csproj
  MetWorks.Data.Sqlite:        ../MetWorks_Data_Sqlite/MetWorks_Data_Sqlite.csproj
  MetWorks.EventRelay:         ../MetWorks_EventRelay/MetWorks_EventRelay.csproj
  MetWorks.ServiceFoundation:  ../MetWorks_ServiceBase/MetWorks_ServiceFoundation.csproj
  MetWorks.Common.Utility:     ../MetWorks_Common_Utility/MetWorks_Common_Utility.csproj
  MetWorks.Common.Metrics:     ../MetWorks_Common_Metrics/MetWorks_Common_Metrics.csproj
```

The codegen tool would then:
1. **Validate** that every `new()`'d type's namespace has a reachable assembly mapping.
2. **Warn or fail** when a type relies on transitive visibility without an explicit mapping.
3. **Optionally emit** the complete set of `<ProjectReference>` items in the DdiRegistry csproj, replacing manual maintenance.

#### Why not dynamic assembly loading?
- Loses compile-time safety — DDI's biggest advantage over runtime reflection DI.
- `Assembly.LoadFrom` on MAUI/Android is constrained by AOT/trimming (types only referenced via reflection can be stripped).
- No meaningful startup gain — `InitializeAllAsync` touches everything during splash anyway.
- Contradicts the project's deliberate choice: *"avoid runtime reflection for DI."*

#### Why not split registries?
- The MAUI app needs everything — splitting into sub-registries means the app references all of them, with added cross-registry initialization ordering complexity.
- Net result: more files, more wiring, same total references, harder to reason about startup order.

### Decision
**Accept the 9 direct references as irreducible.** Address transitive fragility via DDI tooling (`assembly-map`) when the DDI codegen tool is next enhanced.

---

## F5: Server-side executables share zero code with the MAUI app

### Problem (or Opportunity)
Both `MetWorks_Ingest_StreamReceiver` and `MetWorks_Ingest_QueueWorker` have **zero project references**. They are entirely standalone, using only `Microsoft.Data.SqlClient` for SQL Server access. They share:
- No code with the MAUI app
- No code with each other (beyond targeting the same database)
- No MetWorks shared libraries

### Impact
- **Low risk, high opportunity**: These projects are currently harmless in the solution — they add no dependency weight to anything else.
- However, they do add noise: developers working on the MAUI app see server-side projects in the solution explorer, and vice versa.
- Build-all commands build them unnecessarily.

### Recommended Action
1. **Move to a separate solution** (e.g., `MetWorks-ServerIngest.sln`) containing just `StreamReceiver` and `QueueWorker`.
2. Optionally create a shared `MetWorks_Ingest_Common` library between them if they start sharing code.
3. This is the lowest-risk decomposition action because there are no shared references to untangle.

---

## Summary Matrix

| Finding | Severity | Effort | Risk | Status |
|---------|----------|--------|------|--------|
| F1: Common_Settings → MAUI | High | Medium | Low | ✅ Resolved (D3, S3) |
| F2: Common → Npgsql | Medium | Low | Low | ✅ Resolved (D1, D2) |
| F3: Common_Logging heavyweight | Medium-High | Medium | Medium | ✅ Resolved (D2, M1, D4, D6, LoggerStub A+B) |
| F4: DdiRegistry god aggregator | High | High | High | ✅ Accepted constraint (assembly-map recommended for DDI tooling) |
| F5: Server-side isolation | Low (opportunity) | Low | Very Low | Open (recommended) |

### Recommended Priority Order
1. ~~**F5** — Move server-side projects out (zero risk, immediate noise reduction)~~ — Still recommended
2. ~~**F2** — Remove Npgsql from Common (small, surgical)~~ — ✅ Done
3. ~~**F1** — Decouple Common_Settings from MAUI (enables cross-platform settings)~~ — ✅ Done
4. ~~**F3** — Lighten Common_Logging (removed dead Common ref, LoggerStub A+B, Persistence kept by design)~~ — ✅ Done
5. ~~**F4** — DdiRegistry aggregation (all 9 refs verified required; transitive fragility → DDI `assembly-map` tooling enhancement)~~ — ✅ Accepted
