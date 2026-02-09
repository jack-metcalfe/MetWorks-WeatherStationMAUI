# Copilot Instructions

## General Guidelines
- First general instruction
- Second general instruction
- Use this project as a learning vehicle; leverage the assistant for guidance and clarification on concepts and practices.
- Prefer a tiny third 'workspace' Git repo to represent a multi-repo Visual Studio workspace on GitHub (instead of nesting repos).
- Use backward compatibility only as a short interim step; remove incompatible and dead/legacy code as soon as possible to reduce complexity in the codebase.
- Align with tool defaults (e.g., YamlDotNet default YAML quoting/serialization) across all repos; avoid fighting tool behavior unless there's a clear reason or deep understanding of the tool.
- Ensure UI changes consider dark mode readability, as the user typically uses a dark theme whenever available.

## Instrumentation Preferences
- Target Android primarily; emit reports to logs first.
- Persist metrics per installation to distinguish wall devices vs dev box.
- Default sampling interval is 10 seconds (eventually configurable).
- Use Postgres for metrics persistence (single table, jsonb per-interval summary); metrics are disabled by default.
- Phase 1 sampler uses `Process.TotalProcessorTime` deltas + GC deltas.
- Phase 2 wraps `EventRelayBasic` handlers for timing aggregated by `(message_type, recipient_type)` and reports interval-based top-N hotspots with snake_case fields.
- For metrics persistence (Phase 5), use `/services/metrics/{connectionString,tableName,autoCreateTable}` settings; metrics sink should be best-effort (drop on failure) and accept `IInstanceIdentifier` for installation_id, initialized via DDI like other ingestors.

## Naming Conventions
- Prefer a naming pattern where raw, unformatted properties keep their actual type and use a 'Value' suffix (e.g., AirTemperatureValue is a double).

## App Startup Flow
- App startup flow is fixed (InitializationSplashPage then MainSwipeHostPage). Host pages should contain host-specific logic but should remain data-driven where practical; device/registry matching should select guest pages by logical names from the host’s configured list, rather than selecting the host page.

## Logging Behavior
- Logger must not fail initialization when the database/network is unavailable.
- Acceptable to lose log events when the network is down.
- Logger should automatically recover and resume logging when connectivity returns.
- LoggerResilient should be implemented as a shared singleton to ensure consistent logging behavior across components.

## Service Management
- Use `ServiceBase` as a common base for long-running services across the codebase. Derived services should call `InitializeBase(...)`, use `StartBackground(...)` for background tasks, and rely on `ServiceBase`'s `LinkedCancellationToken`/`LocalCancellationToken` and `OnDisposeAsync` override instead of managing `CTS` and `IAsyncDisposable` manually. Prefer `ServiceBase` for non-UI services.
- Keep `WeatherViewModel` as `IDisposable` for now and defer converting ViewModels to `ServiceBase`. Re-evaluate `ServiceBase` adoption later; prefer gradual, non-invasive migration.
- MAUI app will use the usual MAUI constructor-DI pattern for Pages/ViewModels now that custom DI is integrated with MAUI DI.
- Prefer per-service interfaces (not concretes) for Dependency Injection (DI).
- For MAUI DI-constructed classes, prefer readonly non-null constructor-injected fields/properties over nullable fields + NullPropertyGuard; reserve guards for DDI-style parameterless ctor + InitializeAsync two-phase initialization.
- Service startup occurs via DDI `Registry.InitializeAllAsync()`; add new config settings by editing `src/MetWorks_Resource_Store/data/settings.yaml` plus `SettingConstants` and `LookupDictionaries` in `MetWorks.Constants`.
- DDI YAML rules:
  - The define-before-use rule applies to the `instance:` section only (no forward references). `namespace:` ordering is not constrained.
  - Every `instance:` assignment parameter must exist in the corresponding `namespace:` class `parameter:` list.
  - Model interfaces on `namespace:` class entries when the instance is exposed to MAUI DI (`exposeToMauiDi: true`) so non-DDI classes can depend on the interface.
  - Dotted instance access (e.g., `RootCancellationTokenSource.Token`, `TheInstanceIdentifier.InstallationId`) requires:
    - the property to be declared under that class’s `property:` list in `namespace:`
    - the property to exist on the concrete implementation, and ideally also on the associated interface if it’s exposed to MAUI DI.
  - DDI-created services don't see MAUI DI registrations; if a DDI-initialized class needs another service (e.g., MetricsLatestSnapshotStore), that dependency must be declared in DDI YAML and wired via `instance:` assignments. `exposeToMauiDi: true` in YAML registers that DDI instance into MAUI DI.

## Concurrency Management
- Prefer using Interlocked-based, lock-free patterns (when appropriate) to harden concurrency and reduce brittleness.
- Use a semaphore/single-connection approach; set WAL and busy_timeout; rollup worker should run only when the DB is available and back off on SQLITE_BUSY.

## Project-Specific Rules
- InitializeAsync methods must accept a CancellationToken; the host creates a CancellationTokenSource and passes the Token.
- Components should create an internal linked CancellationTokenSource (CTS) and use an Interlocked guard to prevent concurrent InitializeAsync calls.
- An incremental migration approach and CI testing plan are documented in `src/docs/2026-01-18-refactor-plan.md`.
- Session restore files are created at `src/docs/session-restore-2026-01-18.md` and `src/docs/session-restore-2026-01-18.json`.
- Implement a resilient UDP listener/receiver in `Transformer.cs` with bind/rebind logic, provenance tracking, background receive loop, and graceful disposal. This file is currently open and contains the resilient UDP listener logic.
- The project is a .NET MAUI application located at `C:\WinRepos\MetWorks-WeatherStationMAUI` on the `main` branch, with other open files including MAUI XAML pages under `src\weather-station-maui` (MainDeviceViews variants, WeatherPage) and `App.xaml/AppShell.xaml`.
- Update Declarative DI (MetWorks-DeclarativeDI) to provide CancellationToken values (not CancellationTokenSource) from the generated code and support defining properties on class declarations in the DDI input YAML, allowing dotted-notation access to those properties when assigning named initialization parameters for `InitializeAsync`. Ensure that `instance:` entries appear after all their dependencies in the YAML, and every referenced class/interface has a corresponding entry under `namespace:`. Repository: [MetWorks-DeclarativeDI](https://github.com/jack-metcalfe/MetWorks-DeclarativeDI).
- Prefer domain-first namespaces starting with 'MetWorks'. Use technology-specific sub-namespaces (e.g., `MetWorks.Persistence.Postgres`) and program to interfaces where appropriate.
- Prefer not to create interfaces solely for testing; test via the real event-driven path (`IEventRelayBasic`) and integration tests. If programmatic ingestion is needed, expose a public `IngestAsync` on the concrete class rather than adding an interface purely for tests.
- Keep event-driven `IEventRelayBasic` (WeakReferenceMessenger wrapper) as the canonical public API for messaging; components register for specific message types.
- For MAUI startup: it's acceptable to run `StartupInitializer.InitializeAsync()` synchronously during `CreateMauiApp()` for now; consider lazy or metadata-based registration later.
- Migration workflow: perform file-by-file changes, one at a time, with each step's outcome guiding the next.
- User prefers direct, candid feedback and welcomes arguments against their opinions.
- If a helper class is only consumed by a single component, consider absorbing it into that component to simplify the design and reduce brittleness.
- It is acceptable to remove obsolete tests during refactors; adding lots of new tests is desired but should be pragmatic, prioritizing fun and velocity.
- Prefer sorting YAML settings `definitions` by `path` for discoverability; duplicates should be adjacent to ease cleanup.
- In DDI YAML, instances must be defined before first use (no forward references) within the `instance:` section; the `namespace:` section ordering is not constrained. Reorder `instance:` entries so dependencies appear earlier than dependents.
- Prefer not to fight tool defaults (e.g., YamlDotNet default YAML quoting/serialization) unless they understand the tool well; align with default behaviors.

## Data Retention Policy
- When the database hits max size, prefer a retention policy that preserves the last N hours (delete oldest first). Observation rollups should exclude wind and lightning fields (use wind/lightning tables for those) and focus on station pressure, air temperature, relative humidity, illuminance, UV, solar radiation, rain accumulation over the previous minute, and battery; optionally carry the reporting interval.
- Rollups: start with 1h and 1d (skip 1m). Observation battery is obs[0][16] (double) and should be added as a generated column. Retention: preserve last N hours applies to raw facts + rollups; rollups may outlive raw facts. For now, delete old DB rather than migrate.

## MAUI Specific Instructions
- MAUI Shell uses ShellContent route `SwipeCarousel` and splash navigates via `GoToAsync("///SwipeCarousel")`.
- The two-window issue has been fixed by removing OpenWindow/AppShell swapping and resolving AppShell from DI in App.CreateWindow.
- SwipeCarousel currently shows only arrows (content empty) at the end of the session.
- Prefer deterministic manual paging (host ContentView + swipe gestures + arrow:key navigation) over CarouselView when CarouselView exhibits virtualization/recycling issues like oscillation/self-swiping.
- Prefer deterministic manual paging (host ContentView + swipe gestures + arrow:key navigation) over CarouselView when CarouselView shows virtualization/recycling issues like oscillation/self-swiping.