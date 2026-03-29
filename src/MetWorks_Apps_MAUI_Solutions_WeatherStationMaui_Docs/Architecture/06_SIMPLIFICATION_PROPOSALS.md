# Dependency Simplification Proposals

> **Purpose**: Concrete, evidence-based proposals for simplifying inter-project relationships — from zero-effort dead-reference removal to targeted class relocations that break coupling chains.

> **Method**: Each finding is backed by `grep` / `Find All References` / csproj analysis performed against the actual source. No guessing.

---

## Tier 1: Dead References (Zero Risk, Immediate)

These are package or project references that **no source file** in the project actually uses. Removing them has zero behavioral impact.

---

### D1. ✅ Remove Npgsql from MetWorks_Common — COMPLETED

**What was done**: Removed the dead `Npgsql` PackageReference from `MetWorks_Common.csproj`. No source file in Common referenced Npgsql.

---

### D2. ✅ Remove Npgsql from MetWorks_Common_Logging — COMPLETED

**What was done**: Removed the dead `Npgsql` PackageReference from `MetWorks_Common_Logging.csproj`. No source file in Common_Logging referenced Npgsql.

---

### D3. ✅ Remove `Microsoft.Maui.Controls` from MetWorks_Common_Settings — COMPLETED

**What was done**:
1. Deleted `<PackageReference Include="Microsoft.Maui.Controls" />` from csproj.
2. Removed the dead `#if MAUI` / `#endif` blocks and the dead `global using Microsoft.Maui.Storage;`.
3. Removed platform-specific `<SupportedOSPlatformVersion>` and `<TargetPlatformMinVersion>` properties.

**Impact**: Eliminated the MAUI SDK dependency from a shared library.

---

### D4. ✅ Remove `MetWorks_Common_Settings` reference from MetWorks_InstanceIdentifier — COMPLETED

**What was done**: Deleted `<ProjectReference Include="..\MetWorks_Common_Settings\MetWorks_Common_Settings.csproj" />` from `MetWorks_InstanceIdentifier.csproj`. InstanceIdentifier only uses types from `MetWorks_Interfaces` and `MetWorks_Constants`.

**Impact**: Broke the transitive chain `InstanceIdentifier → Common_Settings → MAUI`. Since `Common_Logging → InstanceIdentifier`, this also removed MAUI from the logging dependency tree.

---

### D5. ✅ Remove `MetWorks_Common` reference from MetWorks_Resource_Store — COMPLETED

**Evidence**: `Resource_Store` contains only `ResourceProvider.cs` (a static helper using `Assembly.GetManifestResourceStream`) and embedded resource files. Its `GlobalUsings.cs` imports only `System`, `System.IO`, `System.Linq`. Zero types from `MetWorks.Common` are used.

**What was done**: Removed `<ProjectReference Include="..\MetWorks_Common\MetWorks_Common.csproj" />` from `MetWorks_Resource_Store.csproj`.

**Impact**: `Resource_Store` is now a **true leaf assembly** with zero project dependencies — exactly what a resource-only assembly should be. Any project referencing `Resource_Store` no longer transitively drags in `Common` and all of its dependencies.

---

## Tier 2: Misplaced Classes (Medium Effort, High Impact)

These are classes that live in a project they don't structurally belong to, creating coupling that forces other projects to take unnecessary dependencies.

---

### M1. ✅ `ServiceBase` moved to `MetWorks_ServiceBase` assembly — COMPLETED

**What was done**: Created a new `MetWorks_ServiceBase` project (namespace `MetWorks.ServiceFoundation`) containing:
- `ServiceBase.cs` — abstract base for long-running services
- `ProvenanceTracker.cs` — in-memory LRU lineage tracking
- `DataLineage.cs`, `ReadingProvenance.cs`, `ProvenanceStep.cs`, `ProcessingError.cs` — provenance data types

**Key decisions during implementation**:
- **Namespace**: Named `MetWorks.ServiceFoundation` (not `MetWorks.ServiceBase`) to avoid C# namespace/class name collision (CS0118).
- **Dependencies**: Only `Common_Utility`, `EnumDefinitions`, `Interfaces` — no cycle with Common.
- **DDI YAML**: Added `MetWorks.ServiceFoundation` namespace block, moved `ProvenanceTracker` class declaration there, updated all 17 instance `class:` references from `MetWorks.Common.ProvenanceTracker` to `MetWorks.ServiceFoundation.ProvenanceTracker`.

**Consumer projects updated** (added `global using MetWorks.ServiceFoundation;` to GlobalUsings.cs):
- MetWorks_Common
- MetWorks_Common_Logging
- MetWorks_Networking_Udp_Transformer
- MetWorks_Ingest_Transformer
- MetWorks_Ingest_SQLite

**Impact**: `ServiceBase` is no longer the #1 coupling driver. Assemblies that only need `ServiceBase` can reference the lightweight `MetWorks_ServiceBase` instead of all of `MetWorks_Common`.

---

### M2. ✅ `Common/Metrics/*` moved to `MetWorks_Common_Metrics` — COMPLETED

**What was done**: Moved all `Common/Metrics/*` source files into the `MetWorks_Common_Metrics` project:
- `MetricsSamplerService.cs`
- `MetricsLatestSnapshotStore.cs`
- `MetricsLatestSnapshot.cs`
- `IMetricsLatestSnapshot.cs`
- `MetricsStructuredSnapshot.cs`
- `MetricsStructuredSnapshotParser.cs`
- `StreamShippingUploadMetrics.cs`
- `Storage/LocalStorageSizeCollector.cs`
- `Storage/LocalStorageSizeSnapshot.cs`

Project references added to `MetWorks_Common_Metrics`: Common, Constants, EventRelay, Interfaces, ServiceBase. Namespace `MetWorks.Common.Metrics` retained. DDI YAML unchanged (namespace block and instance references still use `MetWorks.Common.Metrics`).

**Impact**: Completes the migration that was originally started (project shell created but files never moved). `MetWorks_Common` is now slimmed to Tempest REST/WebSocket clients + stream shipping only.

---

### M3. ✅ `SqliteDatabaseOptionsFactory` moved to MetWorks_Data_Sqlite — COMPLETED

**What was done**: Moved `SqliteDatabaseOptionsFactory` from `MetWorks_Common_Settings` to `MetWorks_Data_Sqlite` (namespace `MetWorks.Data.Sqlite`). Updated DDI YAML:
- Moved class declaration from `MetWorks.Common.Settings` namespace block to `MetWorks.Data.Sqlite` namespace block.
- Updated instance `TheSqliteDatabaseOptionsFactory` class reference from `MetWorks.Common.Settings.SqliteDatabaseOptionsFactory` to `MetWorks.Data.Sqlite.SqliteDatabaseOptionsFactory`.
- Removed the `Data_Sqlite` project reference from `Common_Settings`.

**Impact**: Decoupled the settings layer from the data layer. `Common_Settings` no longer references `Data_Sqlite`.

---

### M4. ✅ `DefaultPlatformPaths` moved to MetWorks_Common_Utility — COMPLETED

**What was done**:
1. Removed the dead `#if MAUI` block (the `MAUI` preprocessor symbol was never defined).
2. Moved `DefaultPlatformPaths` from `MetWorks_Common` to `MetWorks_Common_Utility` (namespace `MetWorks.Common.Utility`).

**Impact**: Removed one dependency path through Common. The class is now a clean 14-line implementation with zero external dependencies beyond `System.IO`.

---

## Tier 3: Structural Simplifications (Higher Effort)

---

### S1. Common_Settings → Resource_Store coupling — WON'T FIX (Intentional Pattern)

**Design Intent**: `MetWorks_Resource_Store` is a deliberate centralized embedded-resource assembly for the MAUI app. All class libraries share a single resource assembly rather than each embedding their own resources.

**Assessment**:
- The coupling surface is a single call: `ResourceProvider.GetString(SettingsTemplateResourceName)` in `SettingProvider.Load()`.
- `Resource_Store` is now a true leaf assembly (zero project dependencies after D5).
- `settings.yaml` and `Constants` change in lockstep — the Resource_Store dependency is stable and intentional.
- Pushing the load into DDI would shift complexity without removing it.

**Decision**: Keep as-is. The pattern is sound and well-encapsulated.

---

### S2. ✅ Common_Logging → Persistence → InstanceIdentifier → Common_Settings chain — RESOLVED

**How it was resolved**: Applying D4 (remove dead Common_Settings ref from InstanceIdentifier) and M1 (move ServiceBase to its own assembly) simplified the chain dramatically:

**Before**:
```
Common_Logging
  → Common (for ServiceBase) → EventRelay, Constants, Common_Utility, Interfaces
  → Persistence
    → InstanceIdentifier → Common_Settings → Resource_Store → Common (!)
    → Data_Sqlite
    → Resource_Store → Common (!)
```

**After**:
```
Common_Logging
  → Common (for ServiceBase → ServiceBase assembly, lightweight)
  → Persistence
    → InstanceIdentifier → Interfaces + Constants (clean)
    → Data_Sqlite
    → Resource_Store (leaf, zero project deps)
```

---

### S3. ⏳ `SettingProvider.GetAppDataDirectory()` duplicates `DefaultPlatformPaths` — PENDING

**Evidence**: `SettingProvider` has a static `GetAppDataDirectory()` method (line 145) that duplicates the same logic as `DefaultPlatformPaths.AppDataDirectory` — both resolve to `Environment.SpecialFolder.LocalApplicationData` + `"MetWorks-WeatherStationMAUI"`. `SettingProvider` additionally falls back to `MyDocuments`, then temp.

**Fix**: Inject `IPlatformPaths` into `SettingProvider` and remove the duplicated static method. This requires adding an `iPlatformPaths` parameter to `SettingProvider.InitializeAsync` and updating the DDI YAML `TheSettingProvider` instance to wire it.

---

## Dependency Graph After All Completed Proposals

### Before (start of session)
```
Ingest_SQLite ──────────┐
Ingest_Transformer ─────┤
Networking_Udp_Transformer ─┤──→ MetWorks_Common ──→ EventRelay, Constants, Common_Utility, Interfaces, [Npgsql]
Common_Logging ─────────┤                        ──→ (ServiceBase, ProvenanceTracker, DefaultPlatformPaths lived here)
DdiRegistry ────────────┘

Resource_Store ──→ MetWorks_Common (dead reference!)
InstanceIdentifier ──→ Common_Settings (dead reference!)
Common_Settings ──→ Data_Sqlite (via misplaced SqliteDatabaseOptionsFactory)
```

### After (current state)
```
Ingest_SQLite ──────────┐
Ingest_Transformer ─────┤
Networking_Udp_Transformer ─┤──→ MetWorks_Common ──→ Common_Utility, Constants, EventRelay, Interfaces, ServiceBase
Common_Logging ─────────┤
DdiRegistry ────────────┘

ServiceBase (new assembly, MetWorks.ServiceFoundation) ──→ Common_Utility, EnumDefinitions, Interfaces (lightweight)
Resource_Store ──→ (leaf, zero project deps)
InstanceIdentifier ──→ Interfaces, Constants (clean)
Common_Settings ──→ Common_Utility, Constants, EventRelay, Interfaces, Resource_Store (no more Data_Sqlite)
```

---

## Implementation Status

| # | Proposal | Effort | Risk | Impact | Status |
|---|---------|--------|------|--------|--------|
| D1 | Remove Npgsql from Common | 1 min | None | Removes phantom dependency | ✅ Done |
| D2 | Remove Npgsql from Common_Logging | 1 min | None | Removes phantom dependency | ✅ Done |
| D3 | Remove MAUI from Common_Settings | 5 min | Low | Decouples settings from platform SDK | ✅ Done |
| D4 | Remove Common_Settings ref from InstanceIdentifier | 1 min | None | Breaks MAUI transitive chain | ✅ Done |
| D5 | Remove Common ref from Resource_Store | 1 min | None | Makes Resource_Store a true leaf | ✅ Done |
| M1 | Move ServiceBase + provenance → MetWorks_ServiceBase | 1-2 hr | Medium | **Biggest win**: breaks #1 coupling driver | ✅ Done |
| M3 | Move SqliteDatabaseOptionsFactory → Data_Sqlite | 15 min | Low | Decouples settings → data | ✅ Done |
| M4 | Move DefaultPlatformPaths → Common_Utility | 10 min | Low | Removes one Common dependency path | ✅ Done |
| M2 | Move Metrics/* → Common_Metrics | 30 min | Low | Completes abandoned migration | ✅ Done |
| S1 | Decouple Settings from Resource_Store | 1 hr | Medium | Cleaner initialization | Won't fix |
| S2 | Simplify Common_Logging dependency chain | — | — | Cleaner transitive deps | ✅ Achieved via D4+M1 |
| S3 | Deduplicate GetAppDataDirectory | 15 min | Low | Removes code duplication | ⏳ Pending |

**Remaining work**: S3 (deduplicate GetAppDataDirectory).