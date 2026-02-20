# DDI D4 — init-once contract + use-before-init fail-fast guards

Goal: make DDI’s two-phase initialization safer by contract:

- `InitializeAsync(...)` must run **exactly once per instance** (even if multiple callers try to initialize)
- consumers must **fail fast** if they attempt to use an instance that requires async initialization before it is ready

## Summary of behavior

### Init-once

For assignment-driven instances (`HasAssignments == true`), the generated registry already uses a cached `Task` gate:

- `WhenTheXInitializedAsync()` returns a single cached `Task` per instance.
- The first call starts the initializer; subsequent calls reuse the same `Task`.

This ensures:

- the instance’s generated initializer (`X_Initializer.Initialize_XAsync(...)`) is invoked at most once
- calling `Registry.InitializeAllAsync()` and `Registry.WhenXInitializedAsync()` in any order does not cause double-initialization

### Use-before-init guards

D4 adds a fail-fast guard to the **external** accessors (`GetTheX()`) for any assignment-driven instance.

If `GetTheX()` is called when:

- initialization has not started
- initialization is still in progress
- initialization was canceled
- initialization failed

…then `GetTheX()` throws an `InvalidOperationException` with an actionable message.

Internal accessors (`GetTheX_Internal()`) remain available for generator wiring and initialization, and do **not** enforce readiness.

## Implementation details

### Template changes

- `src/MetWorks_DI_Declarative_Resources/Templates/Accessors.Triplet.hbs`
  - external accessors now check the per-instance init gate (`_initTask_TheX`) when `HasAssignments == true`
  - throws fast with a clear message that points to `WhenTheXInitializedAsync()` / `InitializeAllAsync()`

### Generator model changes

- `src/MetWorks_DI_Declarative_Generator/Models/Accessors/Instance.cs`
  - added `HasAssignments`

- `src/MetWorks_DI_Declarative_Generator/ModelTransformer.cs`
  - populates `HasAssignments` so the template can conditionally emit guards

## Notes / follow-ups

- This is intentionally a **fail-fast** policy (do not block/wait inside `GetTheX()`). Waiting in an accessor would reintroduce sync-over-async hazards in UI code.
- If a consumer truly needs a service during startup, it should await either `Registry.InitializeAllAsync(...)` or the specific `Registry.WhenTheXInitializedAsync(...)` gate.
