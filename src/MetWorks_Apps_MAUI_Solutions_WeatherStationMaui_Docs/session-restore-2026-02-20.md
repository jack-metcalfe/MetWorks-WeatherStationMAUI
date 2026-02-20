# Session restore / running log — 2026-02-20

## Goal
Implement DDI YAML ergonomics items F1 and F2.

## Functional areas touched

### DDI YAML ergonomics (F1)
- Added `instance.factoryInstance` + `instance.factoryMethod` (Option 2: factory instance method binding).
- Updated instance dependency sorting to account for factory-instance dependencies.
- Updated `Instance.Factory` template to construct via `registry.Get<FactoryInstance>_Internal().<FactoryMethod>()` when configured.
- Added build-time factory binding validation (reflection) and wired it into `MetWorks_DI_Declarative_CodeGenTool`.
- Added loader test fixtures and xUnit tests for factory binding generation.

### DDI YAML ergonomics (F2)
- Added compact literal assignment syntax inside `assignment:` sequences: `- maxBufferSize: 1000`.
- Added loader fixture and xUnit tests validating parsing and generated initializer clauses.

### DDI YAML ergonomics (F3)
- Added a canonical YAML formatter based on YamlDotNet’s default emitter output.
- Added `MetWorks_DI_Declarative_CodeGenTool` commands:
  - `--formatYaml` (rewrite input YAML in-place)
  - `--checkYamlFormat` (fail if not canonical)
- Added loader fixture + xUnit tests for formatter idempotency and parse validity.

### DDI YAML ergonomics (F4)
- Added `MetWorks_DI_Declarative_CodeGenTool --sortInstances` to reorder the YAML `instance:` list by dependency (define-before-use order).
- Added loader fixture + xUnit tests verifying sorted instance order and no loader diagnostics after sorting.

### Docs housekeeping (G1)
- Consolidated DDI/initialization items from `ToDo_Candidates.txt` into `TODO.md` and marked G1 implemented in `DDI_CHANGE_BACKLOG.md`.

## Build/test status
- `dotnet build` succeeded.
- `dotnet test tests/MetWorks_DI_Declarative_Loader_Tests` succeeded.
