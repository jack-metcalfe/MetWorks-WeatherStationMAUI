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

## F3: Common_Logging is heavyweight — PARTIALLY RESOLVED

> **Status**: Npgsql removed (D2). ServiceBase dependency now goes through lightweight `MetWorks_ServiceBase` instead of all of Common (M1). InstanceIdentifier no longer pulls in Common_Settings (D4). Remaining: Common_Logging still references Common and Persistence.

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

### Recommended Remediation
1. Extract a core `ILogger` / logging abstraction that has zero infrastructure dependencies.
2. Move database sinks (PostgreSQL, SQLite) into separate sink projects (e.g., `MetWorks_Logging_Sink_Postgres`, `MetWorks_Logging_Sink_Sqlite`).
3. Keep `Common_Logging` as a thin orchestrator that references only the core abstraction + Serilog.
4. Register sinks via DDI so consumers only pull in the sinks they need.

---

## F4: DdiRegistry is a "god aggregator"

### Problem
`MetWorks_DdiRegistry` has **9 direct project references**:
1. Common
2. Common_Logging
3. Common_Settings
4. Maui_Services
5. Ingest_SQLite
6. Ingest_Transformer
7. InstanceIdentifier
8. Networking_Udp_Transformer
9. RedStar_Amounts_WeatherExtensions

The MAUI app references `DdiRegistry`, which means it transitively inherits the union of all 9 trees — approximately **25 projects**. This is the single biggest contributor to the solution's dependency complexity.

### Root Cause
DdiRegistry is generated code. The MSBuild target `GenerateDdiRegistryCode` generates C# classes that `new()` concrete types from across the entire solution. To compile, the registry project must reference every project whose types it instantiates.

### Impact
- **High**: The MAUI app cannot pick and choose which subsystems it needs — it gets everything.
- Build times increase because a change in any referenced project triggers recompilation of DdiRegistry.
- The generated registry is a compile-time bottleneck and a conceptual single point of failure.

### Structural Note
This is an inherent property of the DDI design: a single registry that wires all instances needs visibility into all types. Possible mitigations:
1. **Split the registry** into sub-registries per domain (e.g., `Registry_Ingest`, `Registry_Networking`, `Registry_Persistence`) so each sub-registry references only its domain.
2. **Lazy loading**: Generate factories that load assemblies on demand rather than referencing them at compile time.
3. **Accept the cost**: If build times are acceptable and the MAUI app truly needs everything, this may be an acceptable tradeoff — but document it as a known architectural constraint.

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
| F3: Common_Logging heavyweight | Medium-High | Medium | Medium | ⚠️ Partially resolved |
| F4: DdiRegistry god aggregator | High | High | High | Open (accepted constraint) |
| F5: Server-side isolation | Low (opportunity) | Low | Very Low | Open (recommended) |

### Recommended Priority Order
1. ~~**F5** — Move server-side projects out (zero risk, immediate noise reduction)~~ — Still recommended
2. ~~**F2** — Remove Npgsql from Common (small, surgical)~~ — ✅ Done
3. ~~**F1** — Decouple Common_Settings from MAUI (enables cross-platform settings)~~ — ✅ Done
4. **F3** — Lighten Common_Logging further (remaining: still references Common and Persistence)
5. **F4** — Address DdiRegistry aggregation (largest effort, needs DDI design discussion)
