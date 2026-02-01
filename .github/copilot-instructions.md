# Copilot Instructions

## General Guidelines
- First general instruction
- Second general instruction
- Use this project as a learning vehicle; leverage the assistant for guidance and clarification on concepts and practices.
- Prefer a tiny third 'workspace' Git repo to represent a multi-repo Visual Studio workspace on GitHub (instead of nesting repos).
- Use backward compatibility only as a short interim step; remove incompatible and dead/legacy code as soon as possible to reduce complexity in the codebase.

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

## Concurrency Management
- Prefer using Interlocked-based, lock-free patterns (when appropriate) to harden concurrency and reduce brittleness.

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

## MAUI Specific Instructions
- MAUI Shell uses ShellContent route `SwipeCarousel` and splash navigates via `GoToAsync("///SwipeCarousel")`.
- The two-window issue has been fixed by removing OpenWindow/AppShell swapping and resolving AppShell from DI in App.CreateWindow.
- SwipeCarousel currently shows only arrows (content empty) at the end of the session.
- Prefer deterministic manual paging (host ContentView + swipe gestures + arrow/key navigation) over CarouselView when CarouselView exhibits virtualization/recycling issues like oscillation/self-swiping.
- Prefer deterministic manual paging (host ContentView + swipe gestures + arrow/key navigation) over CarouselView when CarouselView shows virtualization/recycling issues like oscillation/self-swiping.