# DDI D1 — Two-phase async startup + per-service readiness gates

Goal: remove sync-over-async and reduce the “global init gate” behavior by making DDI initialization concurrent, dependency-aware, and observable per service.

## Summary (what changed)

DDI already had a two-phase concept:

- Phase 1: `Registry.CreateAll()` (sync, creates all instances)
- Phase 2: `Registry.InitializeAllAsync()` (async, calls `InitializeAsync(...)` on instances that have assignments)

D1 updates the generated code so Phase 2 is no longer a single sequential gate and so consumers can await *only* the services they actually need.

Key outcomes:

- `Registry.InitializeAllAsync()` now initializes concurrently (uses `Task.WhenAll(...)`).
- The generated registry exposes per-instance readiness gates `WhenTheXInitializedAsync()`.
- Per-instance initializers await required *initialization* dependencies before running, so concurrency is safe.
- MAUI DI registration no longer uses sync-over-async: a synchronous `RegisterSingletonsInMaui(IServiceCollection)` is generated.

## Generated API shape

### Registry

The generator now emits (for each instance that has assignments / async init):

- `Task WhenTheXInitializedAsync()`
  - Idempotent: multiple callers all get the same cached `Task`.
  - Ensures `Initialize_TheXAsync(...)` runs at most once.

`Registry.InitializeAllAsync()` now:

- builds a `Task[]` of all `WhenTheXInitializedAsync()` tasks
- awaits them via `Task.WhenAll(...)`

### Per-instance initializers

For each assignment-based instance initializer, the generator now emits:

- `await registry.WhenTheDepInitializedAsync()` calls before invoking `instance.InitializeAsync(...)`

Only dependencies that themselves require async initialization (i.e., `HasAssignments == true`) are awaited.

## Dependency rule used for readiness ordering

Initialization dependencies are derived from YAML `assignment.instance` references:

- If instance `A` has an assignment that references instance `B`, and `B` has assignments, then `A` must await `WhenBInitializedAsync()` before initializing.

This rule ensures:

- parallel initialization is safe
- dependency ordering is honored where it matters (async init), without forcing everything to run sequentially

## MAUI DI registration

The generator now emits a synchronous method:

- `Registry.RegisterSingletonsInMaui(IServiceCollection services)`

The previous async method remains as a wrapper for compatibility:

- `Task Registry.RegisterSingletonsInMauiAsync(IServiceCollection services, CancellationToken cancellationToken = default)`

The app startup uses the synchronous method to avoid sync-over-async.

## Implementation locations

### Generator changes

- `src/MetWorks_DI_Declarative_Generator/Models/Assignments.Initializer/Instance.cs`
  - Added `InitializationDependencies`
- `src/MetWorks_DI_Declarative_Generator/ModelTransformer.cs`
  - Populates `InitializationDependencies`

### Template changes

- `src/MetWorks_DI_Declarative_Resources/Templates/Registry.hbs`
  - Generates `WhenXInitializedAsync()` gates and concurrent `InitializeAllAsync()`
- `src/MetWorks_DI_Declarative_Resources/Templates/Assignments.Initializer.hbs`
  - Emits dependency awaits before calling the instance initializer
- `src/MetWorks_DI_Declarative_Resources/Templates/ExposeToMauiDi.hbs`
  - Adds `RegisterSingletonsInMaui(...)` and makes async wrapper non-async

### App change

- `src/MetWorks_Apps_MAUI_WeatherStationMaui/StartupInitializer.cs`
  - Calls `RegisterSingletonsInMaui(...)`

### Build integration

- `src/MetWorks_DdiRegistry/MetWorks_DdiRegistry.csproj`
  - Incremental generation inputs include `MetWorks_DI_Declarative_Resources/**/*.hbs` so template edits trigger regeneration

## Follow-ups / next items

- D2/D4: formalize cancellation + timeout guidance and “initialize exactly once” semantics at the instance level (beyond the registry gating) if needed.
- E-series: emit a generation/initialization report (useful now that init is concurrent).
