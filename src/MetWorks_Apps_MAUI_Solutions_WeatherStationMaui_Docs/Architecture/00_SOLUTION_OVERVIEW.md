# Solution Overview

> **Purpose**: Provide a single-page map of the MetWorks-WeatherStationMAUI solution — what it contains, how it is organized, and where to find deeper analysis.

## Executables

The solution produces **four runnable executables** from distinct project types:

| # | Executable | SDK | Purpose |
|---|-----------|-----|---------|
| 1 | **WeatherStationMaui** | .NET MAUI (Android + Windows) | End-user weather dashboard app |
| 2 | **StreamReceiver** | ASP.NET Core Web | Self-hosted web service ingesting weather data into SQL Server |
| 3 | **QueueWorker** | ASP.NET Core Web | Background queue processor writing to SQL Server |
| 4 | **DDI Generator** | Console | Build-time code generation tool (YAML → C# registry) |

## Project Inventory

The solution contains **~35 unique projects** (some appear twice due to solution folder grouping). De-duplicated, they break down as follows:

### Application Projects
| Project | Type |
|---------|------|
| `MetWorks_Apps_MAUI_WeatherStationMaui` | .NET MAUI app |
| `MetWorks_Ingest_StreamReceiver` | ASP.NET Core web service |
| `MetWorks_Ingest_QueueWorker` | ASP.NET Core web service |
| `MetWorks_DI_Declarative_Generator` | Console tool |

### Core Libraries
| Project | Role |
|---------|------|
| `MetWorks_Interfaces` | Shared interfaces & contracts |
| `MetWorks_Constants` | Shared constants & lookup dictionaries |
| `MetWorks_EnumDefinitions` | Shared enumerations |
| `MetWorks_ServiceBase` | ServiceBase + ProvenanceTracker (namespace `MetWorks.ServiceFoundation`) |
| `MetWorks_Common` | Shared services (REST client, metadata provider) |
| `MetWorks_Common_Utility` | Utility helpers, NullPropertyGuard, DefaultPlatformPaths |
| `MetWorks_Common_Logging` | Multi-sink structured logging |
| `MetWorks_Common_Settings` | Settings repository |
| `MetWorks_Common_Metrics` | Performance metrics sampling |
| `MetWorks_InstanceIdentifier` | Per-installation identity |

### Domain & Data
| Project | Role |
|---------|------|
| `MetWorks_Models_Observables` | Observable domain models |
| `MetWorks_IoT_UDP_Tempest` | Tempest UDP protocol parsing |
| `MetWorks_Ingest` | Ingest contracts |
| `MetWorks_Ingest_Transformer` | Data transformation pipeline |
| `MetWorks_Ingest_SQLite` | SQLite ingest persistence |
| `MetWorks_Networking_Udp_Transformer` | UDP ↔ domain transformer |
| `MetWorks_EventRelay` | Event messaging (WeakReferenceMessenger wrapper) |
| `MetWorks_Maui_Services` | MAUI-specific services |

### Persistence
| Project | Role |
|---------|------|
| `MetWorks_Data_Sqlite` | SQLite database abstraction + SqliteDatabaseOptionsFactory |
| `MetWorks_Persistence` | Persistence orchestration |
| `MetWorks_Resource_Store` | Centralized embedded resources (settings.yaml, SQL DDL) — leaf assembly, zero project deps |

### DDI (Declarative DI) Toolchain
| Project | Role |
|---------|------|
| `MetWorks_DI_Declarative_Generator` | Code generation engine (Handlebars templates) |
| `MetWorks_DI_Declarative_Loader` | YAML model loader |
| `MetWorks_DI_Declarative_Interfaces` | DDI contracts |
| `MetWorks_DI_Declarative_EnumDefinitions` | DDI enumerations |
| `MetWorks_DI_Declarative_Resources` | DDI embedded templates |
| `MetWorks_DI_Declarative_Diagnostics` | DDI diagnostic utilities |
| `MetWorks_DdiRegistry` | Generated DI registry (god aggregator) |

### Third-Party Forks / Extensions
| Project | Role |
|---------|------|
| `RedStar_Amounts` | Unit-of-measure types |
| `RedStar_Amounts_StandardUnits` | Standard unit definitions |
| `RedStar_Amounts_WeatherExtensions` | Weather-specific units |

### Test Projects
| Project | Role |
|---------|------|
| `MetWorks_DI_Declarative_Loader_Tests` | DDI loader tests (xUnit) |

### Documentation
| Project | Role |
|---------|------|
| `MetWorks_Apps_MAUI_Solutions_WeatherStationMaui_Docs` | Architecture docs, DDI YAML, guides |

## Document Index

| Document | Contents |
|----------|----------|
| [01 — Executable Profiles](01_EXECUTABLE_PROFILES.md) | Per-executable deep dive: SDK, dependencies, deployment |
| [02 — Dependency Graph](02_DEPENDENCY_GRAPH.md) | Mermaid diagrams, NuGet package table |
| [DependencyGraph.dgml](DependencyGraph.dgml) | Interactive VS Enterprise diagram (double-click to open) |
| [03 — Dependency Audit](03_DEPENDENCY_AUDIT.md) | Findings and risks in current dependency structure |
| [04 — Decomposition Analysis](04_DECOMPOSITION_ANALYSIS.md) | Should this be one solution or many? |
| [05 — API Surface Recommendations](05_API_SURFACE_RECOMMENDATIONS.md) | Per-assembly visibility guidance |
| [06 — Simplification Proposals](06_SIMPLIFICATION_PROPOSALS.md) | Dead references, misplaced classes, structural fixes |

## Key Observations

1. **StreamReceiver and QueueWorker share zero project references** with the MAUI app — they are entirely standalone.
2. **DdiRegistry is a "god aggregator"** — it has 9 direct project references and the MAUI app depends on it, pulling ~25 projects transitively.
3. ~~**Common_Settings depends on `Microsoft.Maui.Controls`**~~ — ✅ Resolved (D3). MAUI SDK dependency removed.
4. ~~**Common depends on `Npgsql`**~~ — ✅ Resolved (D1, D2). Npgsql removed from both Common and Common_Logging.
5. **ServiceBase extracted** — ✅ Resolved (M1). `ServiceBase` + provenance types now live in lightweight `MetWorks_ServiceBase` assembly (`MetWorks.ServiceFoundation` namespace), breaking the #1 coupling driver.
6. **Metrics migrated** — ✅ Resolved (M2). All `Common/Metrics/*` files moved to `MetWorks_Common_Metrics`.
7. **SqliteDatabaseOptionsFactory relocated** — ✅ Resolved (M3). Moved from Common_Settings to Data_Sqlite, decoupling settings → data.
8. **DefaultPlatformPaths relocated** — ✅ Resolved (M4). Moved from Common to Common_Utility.
9. **Resource_Store is a true leaf** — ✅ Resolved (D5). Zero project dependencies.
10. **InstanceIdentifier decoupled** — ✅ Resolved (D4). No longer references Common_Settings.
11. **GetAppDataDirectory deduplicated** — ✅ Resolved (S3). SettingProvider now uses injected `IPlatformPaths`.

See [06 — Simplification Proposals](06_SIMPLIFICATION_PROPOSALS.md) for full details on all completed and pending changes.
