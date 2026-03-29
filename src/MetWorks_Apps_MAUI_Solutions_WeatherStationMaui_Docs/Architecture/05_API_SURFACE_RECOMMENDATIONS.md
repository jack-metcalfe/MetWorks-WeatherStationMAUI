# API Surface Recommendations

> **Purpose**: Per-assembly guidance on what should be `public` versus `internal`, ensuring each assembly exposes only what is needed by its consumers.

## Guiding Principles

1. **Least exposure**: Default to `internal`. Only make types `public` if consumed outside the assembly.
2. **Interface over implementation**: Expose interfaces publicly; keep concrete implementations `internal` where possible (wired via DI/DDI).
3. **No transitive leakage**: A public type should not expose types from its dependencies in its public API surface unless those dependency types are also intended to be public contracts.
4. **Audit trigger**: Any new `public` type should prompt the question — "Who outside this assembly needs this?"

---

## Per-Assembly Recommendations

### MetWorks_Interfaces

**Role**: Shared contracts consumed by nearly every project.

| Recommendation | Rationale |
|---------------|-----------|
| ✅ Everything here **should** be `public` | This is the canonical contract layer |
| ⚠️ Review for bloat | Only interfaces actually consumed by 2+ projects belong here |
| ⚠️ Avoid implementation types | No concrete classes — only interfaces, enums, and DTOs |

**Action**: Audit for interfaces that are consumed by only one project. Move single-consumer interfaces into that project as `internal`.

---

### MetWorks_Constants

**Role**: Shared constants and lookup dictionaries.

| Recommendation | Rationale |
|---------------|-----------|
| ✅ Constants classes: `public` | Consumed by many projects |
| ⚠️ Lookup dictionaries: review access | If only consumed by one project, make `internal` or move |

---

### MetWorks_EnumDefinitions

**Role**: Shared enumerations.

| Recommendation | Rationale |
|---------------|-----------|
| ✅ Enums: `public` | Shared contract types |
| Keep this assembly thin | Only enums that cross assembly boundaries |

---

### MetWorks_ServiceBase

**Role**: Lightweight base for long-running services + provenance tracking (namespace `MetWorks.ServiceFoundation`).

| Recommendation | Rationale |
|---------------|-----------|
| ✅ `ServiceBase`: `public` | Abstract base consumed by ~20 classes across 5+ assemblies |
| ✅ `ProvenanceTracker`: `public` | Consumed by DDI as a named instance; wired into many services |
| ✅ Provenance data types: `public` | `DataLineage`, `ReadingProvenance`, `ProvenanceStep`, `ProcessingError` — consumed by ProvenanceTracker and services |
| ⚠️ Keep dependencies minimal | Only Common_Utility, EnumDefinitions, Interfaces |

---

### MetWorks_Common

**Role**: Shared services (TempestRestClient, StationMetadataProvider, stream shipping).

> **Note**: ServiceBase, ProvenanceTracker, and provenance data types have been moved to `MetWorks_ServiceBase` (`MetWorks.ServiceFoundation` namespace). DefaultPlatformPaths moved to `MetWorks_Common_Utility`. Npgsql removed (was dead reference).

| Recommendation | Rationale |
|---------------|-----------|
| ✅ Interfaces (if any): `public` | Contracts for DI |
| ⚠️ Concrete services: `internal` preferred | Wire via DDI; consumers depend on interfaces |
| ⚠️ Helper/utility methods: `internal` | Unless genuinely shared; prefer moving to Common_Utility |

**Action**: For each `public` class, verify it has consumers in other assemblies. If not, make `internal`.

---

### MetWorks_Common_Utility

**Role**: Utility helpers, YAML parsing.

| Recommendation | Rationale |
|---------------|-----------|
| ✅ Extension methods: `public` | Consumed broadly |
| ⚠️ Internal helpers: `internal` | If only used within this assembly |

---

### MetWorks_Common_Logging

**Role**: Multi-sink structured logging.

| Recommendation | Rationale |
|---------------|-----------|
| ✅ Logger interfaces/abstractions: `public` | Consumed by many projects via DI |
| ⚠️ Concrete sink implementations: `internal` | Wire via DDI; consumers use the interface |
| ⚠️ Configuration types: `internal` unless needed by DDI YAML | DDI may need public constructors |

**Note**: DDI-constructed types need `public` parameterless constructors (DDI requirement). This conflicts with the "make it internal" guidance. For DDI-wired types, keep the class `public` but minimize the public API surface (no unnecessary public methods/properties).

---

### MetWorks_Common_Settings

**Role**: Settings repository, MAUI Preferences bridge.

| Recommendation | Rationale |
|---------------|-----------|
| ✅ `ISettingRepository` (or equivalent): `public` | Core contract |
| ⚠️ MAUI Preferences bridge: should be `internal` in a MAUI-specific assembly | See Audit F1 |
| ⚠️ Concrete settings provider: `internal` | Wire via DDI |

---

### MetWorks_EventRelay

**Role**: Event messaging via `WeakReferenceMessenger`.

| Recommendation | Rationale |
|---------------|-----------|
| ✅ `IEventRelayBasic` (or equivalent): `public` | Core messaging contract |
| ⚠️ Concrete relay: `internal` preferred | Wire via DDI |
| ✅ Message types: `public` | Producers and consumers in different assemblies need these |

---

### MetWorks_Models_Observables

**Role**: Observable domain models.

| Recommendation | Rationale |
|---------------|-----------|
| ✅ Model classes: `public` | Consumed by ViewModels and other layers |
| ⚠️ Internal helpers: `internal` | |

---

### MetWorks_IoT_UDP_Tempest

**Role**: Tempest UDP protocol parsing.

| Recommendation | Rationale |
|---------------|-----------|
| ✅ Parser interfaces/entry points: `public` | Consumed by Networking_Udp_Transformer |
| ⚠️ Internal parsing logic: `internal` | Protocol details are implementation |
| ⚠️ Raw packet types: `internal` unless consumed externally | |

---

### MetWorks_Ingest_Transformer / MetWorks_Networking_Udp_Transformer

**Role**: Data transformation pipeline.

| Recommendation | Rationale |
|---------------|-----------|
| ✅ Transformer interfaces: `public` | Consumed via DI |
| ⚠️ Concrete transformers: `internal` | Wire via DDI |
| ⚠️ Internal DTOs: `internal` | Unless shared across assemblies |

---

### MetWorks_Data_Sqlite / MetWorks_Persistence

**Role**: Database abstraction and persistence orchestration.

| Recommendation | Rationale |
|---------------|-----------|
| ✅ `ISqliteDatabase` (or equivalent): `public` | Core persistence contract |
| ⚠️ Concrete implementations: `internal` | Wire via DDI |
| ⚠️ SQL helpers/builders: `internal` | Implementation details |

---

### MetWorks_DdiRegistry

**Role**: Generated DI registry.

| Recommendation | Rationale |
|---------------|-----------|
| ✅ `Registry` class: `public` | Entry point for `InitializeAllAsync()` |
| ⚠️ Individual instance factories: `internal` if possible | Generated code — may need to be public for MAUI DI registration |
| Auto-generated — don't edit | Changes must go through DDI YAML + Generator |

---

### MetWorks_Maui_Services

**Role**: MAUI-specific services.

| Recommendation | Rationale |
|---------------|-----------|
| ✅ Service interfaces: `public` | Consumed by MAUI app via constructor DI |
| ⚠️ Concrete services: `internal` preferred | Register in MAUI DI container |

---

### DDI Subsystem (Generator, Loader, Interfaces, EnumDefinitions, Resources, Diagnostics)

**Role**: Build-time code generation toolchain.

| Recommendation | Rationale |
|---------------|-----------|
| ✅ DDI Interfaces/EnumDefinitions: `public` | Cross-DDI-project contracts |
| ⚠️ Generator internals: `internal` | Only the CLI entry point needs to be public |
| ⚠️ Loader internals: `internal` except model types | Model types may need to be public for Generator |
| ✅ Resources: `public` getters for templates | Consumed by Generator |

---

### Server-Side (StreamReceiver, QueueWorker)

**Role**: Standalone executables.

| Recommendation | Rationale |
|---------------|-----------|
| N/A — these are executables, not libraries | No other project consumes their types |
| ⚠️ Make all types `internal` | No external consumers; executable types don't need to be public |

---

## DDI Constraint: Public Parameterless Constructors

The DDI code generator creates instances via `new()` (InstanceFactory pattern). This means:

- DDI-constructed types **must** have a `public` parameterless constructor.
- The type itself **must** be `public` (or at least `internal` with `InternalsVisibleTo` to the DdiRegistry project).

### Recommendation
For DDI-wired types, accept that the class must be `public`, but:
1. Keep the public API surface minimal (only what DDI and interfaces require).
2. Use `internal` for methods/properties not part of the interface contract.
3. Document the "public because DDI" constraint with a comment if it feels surprising.

---

## Audit Checklist

For each assembly, run through this checklist:

- [ ] List all `public` types
- [ ] For each `public` type, identify its consumers (use Find All References)
- [ ] If consumed only within the assembly → make `internal`
- [ ] If consumed by only one other assembly → consider moving it to that assembly
- [ ] If the type is a concrete implementation of an interface → make `internal`, expose via DI
- [ ] If the type must be `public` for DDI → document why
- [ ] Verify no `public` type exposes dependency types (e.g., `Npgsql` types) in its signature

## Tooling Support

- **Visual Studio**: "Find All References" on each `public` type to verify external usage
- **Roslyn Analyzers**: Consider enabling `CA1852` (seal internal types) and reviewing `CA1724` (type names shouldn't match namespaces)
- **Architecture tests**: Consider adding ArchUnitNET or NetArchTest to enforce layering rules programmatically
