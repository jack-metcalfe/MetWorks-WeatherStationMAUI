# Executable Profiles

> **Purpose**: Document each of the four executables in the solution — what they do, what SDK they use, what they depend on, and how they deploy.

---

## 1. WeatherStationMaui (MAUI App)

| Attribute | Value |
|-----------|-------|
| **Project** | `MetWorks_Apps_MAUI_WeatherStationMaui` |
| **SDK** | `Microsoft.NET.Sdk` (MAUI workload) |
| **TFM** | `net10.0-android;net10.0-windows10.0.19041.0` |
| **Output** | Mobile/desktop app (APK, MSIX) |

### Direct Project References (5)
1. `MetWorks_Common_Utility`
2. `MetWorks_DdiRegistry`
3. `MetWorks_Interfaces`
4. `MetWorks_Maui_Services`
5. `RedStar_Amounts`

### Transitive Dependency Reach (~25 projects)
Through `MetWorks_DdiRegistry` (the "god aggregator"), the MAUI app transitively pulls in nearly every project in the solution: Common, Common_Logging, Common_Settings, Common_Metrics, Constants, Data_Sqlite, EnumDefinitions, EventRelay, InstanceIdentifier, Ingest, Ingest_SQLite, Ingest_Transformer, Interfaces, IoT_UDP_Tempest, Models_Observables, Networking_Udp_Transformer, Persistence, Resource_Store, RedStar_Amounts_StandardUnits, RedStar_Amounts_WeatherExtensions, and more.

### Key NuGet Packages
- `Microsoft.Maui.Controls`
- `CommunityToolkit.Maui`
- `SkiaSharp` / `LiveChartsCore.SkiaSharpView.Maui`

### Deployment
- Android: APK signed and deployed via USB/adb or store
- Windows: MSIX package

### Notes
- App startup: `InitializationSplashPage` → `MainSwipeHostPage`
- DI: DDI `Registry.InitializeAllAsync()` + MAUI constructor DI (DDI instances exposed via `exposeToMauiDi: true`)
- Settings loaded from `settings.yaml` via `MetWorks_Resource_Store`

---

## 2. StreamReceiver (Web Service)

| Attribute | Value |
|-----------|-------|
| **Project** | `MetWorks_Ingest_StreamReceiver` |
| **SDK** | `Microsoft.NET.Sdk.Web` |
| **TFM** | `net10.0` |
| **Output** | Self-hosted ASP.NET Core web service |

### Direct Project References
**None** — this is a fully standalone executable.

### Key NuGet Packages
- `Microsoft.Data.SqlClient` (SQL Server)

### Implementation
Single-file web service (`SelfHostedStreamReceiverWebService.cs`) that:
- Accepts HTTP POST of weather observation JSON
- Writes directly to SQL Server tables (`Weather.IngestQueue`, `Weather.RawIngest`)
- Uses raw ADO.NET (`SqlConnection` / `SqlCommand`)

### Deployment
- Runs as a standalone web service (likely on a server or VM)
- No dependency on MAUI, SQLite, DDI, or any MetWorks shared library

### Notes
- **Zero code overlap** with the MAUI app
- Could live in a completely separate repository with no impact

---

## 3. QueueWorker (Background Worker)

| Attribute | Value |
|-----------|-------|
| **Project** | `MetWorks_Ingest_QueueWorker` |
| **SDK** | `Microsoft.NET.Sdk.Web` |
| **TFM** | `net10.0` |
| **Output** | Background worker service |

### Direct Project References
**None** — this is a fully standalone executable.

### Key NuGet Packages
- `Microsoft.Data.SqlClient` (SQL Server)

### Implementation
Single-file worker (`QueueWorkerProgram.cs`) that:
- Polls `Weather.IngestQueue` table on a timer
- Dequeues rows and processes them into `Weather.RawIngest`
- Uses raw ADO.NET

### Deployment
- Runs as a background service (systemd, Windows Service, or container)
- No dependency on MAUI, SQLite, DDI, or any MetWorks shared library

### Notes
- **Zero code overlap** with the MAUI app
- Paired with StreamReceiver (both target the same SQL Server database)
- Could live in a separate repository alongside StreamReceiver

---

## 4. DDI Generator (Code Generation Tool)

| Attribute | Value |
|-----------|-------|
| **Project** | `MetWorks_DI_Declarative_Generator` |
| **SDK** | `Microsoft.NET.Sdk` |
| **TFM** | `net10.0` |
| **Output** | Console application (build-time tool) |

### Direct Project References (4)
1. `MetWorks_DI_Declarative_Interfaces`
2. `MetWorks_DI_Declarative_EnumDefinitions`
3. `MetWorks_DI_Declarative_Resources`
4. `MetWorks_DI_Declarative_Loader`

### Key NuGet Packages
- `Handlebars.Net` (template rendering)
- `System.Reflection.MetadataLoadContext` (reflection-only assembly loading)
- `YamlDotNet` (YAML parsing, transitive through Loader)

### Pipeline
```
WeatherStationMaui.yaml
    → DDI Loader (parse + validate)
    → DDI Generator (Handlebars templates)
    → *.g.cs files
    → Compiled into MetWorks_DdiRegistry
    → Referenced by MAUI App
```

### Invocation
Called by MSBuild target `GenerateDdiRegistryCode` in `MetWorks_DdiRegistry.csproj`, which runs before `CoreCompile`:
```xml
<Target Name="GenerateDdiRegistryCode" BeforeTargets="CoreCompile">
    <Exec Command="dotnet CodeGenTool.dll --yaml ... --out ... --refsFile ..." />
</Target>
```

### Deployment
- Not deployed — runs only at build time on the developer machine or CI

### Notes
- Self-contained DDI subsystem with its own interfaces, enums, resources, loader, and diagnostics
- No dependency on any MetWorks runtime library
- Could live in a separate repository ([MetWorks-DeclarativeDI](https://github.com/jack-metcalfe/MetWorks-DeclarativeDI))

---

## Cross-Cutting Summary

| Executable | Project Refs | Shared Code | Database | Deployment |
|-----------|-------------|-------------|----------|------------|
| MAUI App | 5 direct / ~25 transitive | Everything via DdiRegistry | SQLite (local) | Mobile/Desktop |
| StreamReceiver | 0 | None | SQL Server | Server |
| QueueWorker | 0 | None | SQL Server | Server |
| DDI Generator | 4 (DDI only) | DDI subsystem only | None | Build-time |

**Key Insight**: Only the MAUI app uses the shared MetWorks libraries. The other three executables are either standalone (StreamReceiver, QueueWorker) or use only DDI-specific libraries (Generator). This is a strong signal for potential solution decomposition.
