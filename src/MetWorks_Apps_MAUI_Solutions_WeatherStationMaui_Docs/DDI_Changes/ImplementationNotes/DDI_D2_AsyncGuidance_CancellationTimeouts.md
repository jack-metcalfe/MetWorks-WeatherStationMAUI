# DDI D2 — Async guidance: cancellation, timeouts, ConfigureAwait

Goal: standardize async usage across the DDI ↔ MAUI startup boundary, specifically:

- cancellation support
- timeout support
- consistent `ConfigureAwait(false)` usage in library / generated code

## What changed

### Generated `Registry` API now supports cancellation + timeouts

The generator now emits additional overloads on the registry initialization API:

- `Task InitializeAllAsync()`
  - remains for compatibility
- `Task InitializeAllAsync(CancellationToken cancellationToken)`
  - supports *waiting* with cancellation via `WaitAsync(cancellationToken)`
- `Task InitializeAllAsync(TimeSpan timeout, CancellationToken cancellationToken = default)`
  - supports timeouts via a linked CTS with `CancelAfter(timeout)`

Note: the cancellation/timeout applies to the *wait* on initialization completion. It does not forcibly stop already-started initialization tasks unless the underlying service respects the cancellation token passed into its `InitializeAsync(...)`.

### Per-service readiness gates support cancellation

For each assignment-based instance, the registry now emits:

- `Task WhenTheXInitializedAsync()`
- `Task WhenTheXInitializedAsync(CancellationToken cancellationToken)`

The cancellation overload uses `WaitAsync(cancellationToken)` so callers can stop waiting without blocking the UI indefinitely.

### ConfigureAwait policy

- Generated code (registry + per-instance initializers) continues to use `ConfigureAwait(false)`.
- App/UI code can omit `ConfigureAwait(false)` when appropriate, but current code frequently uses it for consistency.

## MAUI startup behavior

`InitializationSplashPage` now starts initialization via:

- `StartupInitializer.InitializeWithTimeoutAsync(TimeSpan.FromMinutes(2))`

This is a UI-level safety net to avoid “stuck forever” behavior if a downstream I/O dependency never completes.

## Implementation locations

### Generator templates

- `src/MetWorks_DI_Declarative_Resources/Templates/Registry.hbs`
  - added cancellation/timeout overloads for `InitializeAllAsync(...)`
  - added cancellation overloads for `WhenXInitializedAsync(...)`

### App startup

- `src/MetWorks_Apps_MAUI_WeatherStationMaui/StartupInitializer.cs`
  - `InitializeAsync(CancellationToken)`
  - `InitializeWithTimeoutAsync(TimeSpan, CancellationToken)`
  - propagates the token into `Registry.InitializeAllAsync(cancellationToken)`

- `src/MetWorks_Apps_MAUI_WeatherStationMaui/Pages/InitializationSplashPage.xaml.cs`
  - uses `InitializeWithTimeoutAsync(TimeSpan.FromMinutes(2))` for both initial run and retry

## Notes / follow-ups

- D3 (generator passing `CancellationToken` values vs sources) is separate; D2 focuses on consistent cancellation/timeout patterns at call sites and the registry-level orchestration APIs.
- Future work: if we want cancellation to *actually stop work*, services must respect a propagated token (typically passed into `InitializeAsync(...)` via YAML assignment). D2 makes the orchestration layer cancellation-aware.
