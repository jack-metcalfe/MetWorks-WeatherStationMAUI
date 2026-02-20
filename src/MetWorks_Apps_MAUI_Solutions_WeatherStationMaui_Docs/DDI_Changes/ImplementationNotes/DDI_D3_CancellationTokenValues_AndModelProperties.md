# DDI D3 — CancellationToken values (not CTS) + properties on class declarations

Goal: reduce foot-guns around cancellation by ensuring the system passes and exposes `CancellationToken` values instead of `CancellationTokenSource` (CTS), and ensure dotted-property access is backed by explicit properties modeled on class declarations in YAML.

## CancellationToken values vs CancellationTokenSource

### Background

- Services in this solution follow the pattern: parameterless constructor + `InitializeAsync(..., CancellationToken)`.
- The host controls cancellation via a CTS, but **services should depend on `CancellationToken`**, not on the CTS.

CTS is a powerful primitive (it can cancel other components). Exposing it broadly increases coupling and makes cancellation semantics ambiguous.

### What D3 changes

The generated MAUI DI bridge no longer registers `CancellationTokenSource` into `IServiceCollection`.

Instead, when an instance exposed to MAUI DI is a `System.Threading.CancellationTokenSource`, the generator registers:

- `System.Threading.CancellationToken`
- resolved from `GetTheRootCancellationTokenSource().Token`

This keeps MAUI-constructed consumers aligned with the intended dependency shape (`CancellationToken`) while DDI-initialized services can still use the CTS internally through the registry.

### Implementation locations

- Generator model:
  - `src/MetWorks_DI_Declarative_Generator/Models/ExposeToMauiDi/Instance.cs`
    - added `ServiceTypeQualified` and `ResolveExpression`

- Transformation logic:
  - `src/MetWorks_DI_Declarative_Generator/ModelTransformer.cs`
    - for `ClassQualified == "System.Threading.CancellationTokenSource"`, emit:
      - `ServiceTypeQualified = "System.Threading.CancellationToken"`
      - `ResolveExpression = "GetTheRootCancellationTokenSource().Token"` (instance-name specific)

- Template:
  - `src/MetWorks_DI_Declarative_Resources/Templates/ExposeToMauiDi.hbs`
    - uses `ServiceTypeQualified` + `ResolveExpression` so no template-time type checks are required

## Properties on class declarations (YAML model)

DDI supports dotted instance access in assignments (example):

- `assignment.instance: TheRootCancellationTokenSource.Token`

Rule: **every dotted segment must be declared under `namespace:` → `class:` → `property:`** for the owning type.

Example (already used in `WeatherStationMaui.yaml`):

- `System.Threading.CancellationTokenSource` declares `Token` as a `System.Threading.CancellationToken` property.

This is required for build-time validation (YAML + reflection) and for safe code generation.

## Notes

- D2 adds cancellation/timeout overloads at the registry orchestration layer; D3 focuses on *what type* is exposed/passed (`CancellationToken` vs CTS).
- If a component truly needs cancellation control, it should own its own CTS or receive a dedicated cancellation abstraction rather than grabbing the global CTS from DI.
