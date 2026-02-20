# DDI change backlog (WeatherStationMaui)

This document consolidates DDI-related improvement items currently scattered across docs and adds new items discovered during integration work.

Scope: Declarative DI (DDI) YAML authoring, code generation, generated registry ergonomics, and build/IDE workflow.

## What we want from DDI improvements (desired outcomes)

1. **Generator-integrated workflow**
   - The solution should regenerate DDI output automatically when the input YAML changes, without manual steps.
   - Prefer incremental behavior (no regen if inputs/outputs are unchanged).

2. **Fast, predictable feedback**
   - Fail fast with actionable diagnostics when YAML is invalid or out of sync with C#.
   - Avoid the “read generated `.g.cs` to debug wiring” loop.

3. **Safe graphs by construction**
   - Reduce foot-guns like manual `instance:` ordering.
   - Detect cycles with explicit paths.

4. **Lower drift risk**
   - Minimize or validate duplication of truth between YAML and C# signatures/properties.

5. **Ergonomic authoring**
   - Make YAML easier to write/maintain (formatting, compact syntax, tooling support).

6. **Clear runtime semantics**
   - Make initialization/lifetimes/disposal rules explicit, especially across the DDI ↔ MAUI DI bridge.

## Inputs, outputs, and current workflow

- Input YAML (authoritative): `src/MetWorks_Apps_MAUI_Solutions_WeatherStationMaui_Docs/WeatherStationMaui.yaml`
- Generated output (runtime): `src/MetWorks_DdiRegistry/*.g.cs`
- Test harness used for generation:
  - `tests/MetWorks_DI_Declarative_Loader_Tests/GenerateCode.cs`

Note: avoid generating into directories compiled by SDK default globs unless explicitly excluded in the project file.

## Consolidated improvement items (with sources)

### A) Build / IDE integration

A1. **Generate DDI code as part of the build when YAML changes** (new)
- Goal: no manual generation step when `WeatherStationMaui.yaml` is edited.
- Candidate approach:
  - Add an MSBuild `Target` that runs the generator before compile.
  - Use Inputs/Outputs so it runs only when YAML is newer than generated files.
  - Emit to a known output folder (ideally the actual `MetWorks_DdiRegistry` location).

Status: implemented in `MetWorks_DdiRegistry.csproj` via an incremental MSBuild target that runs `MetWorks_DI_Declarative_CodeGenTool`.

A2. Visual Studio integration: “Generate DDI code” command
- Validate YAML
- Run generation
- Open diff
- Source: `DDI/IMPROVEMENT_NOTES.md`

A3. In-editor YAML validation (schema)
- JSON/YAML schema for required keys + enum-like fields
- Define-before-use checks
- Source: `DDI/IMPROVEMENT_NOTES.md`

A4. Don’t accidentally compile generated output in test harness folders
- Ensure any “generation output” folder under a project is excluded from `Compile`.
- Source: integration issue discovered while generating into `fixtures/Testing`.

### B) Instance ordering + graph correctness

B1. Auto-order `instance:` graph (topological sort)
- Deterministic/stable output ordering
- Better error when ordering is invalid
- Source: `ddi-constructive-criticisms-2026-02-15.md` (#1)

B2. Cycle detection with explicit cycle path
- Emit `A -> B -> C -> A`
- Provide hints for common cycles (bootstrap logger, late-bind DB logging)
- Source: `ddi-constructive-criticisms-2026-02-15.md` (#2)

### C) YAML ↔ C# validation (drift prevention)

C1. Reflect and validate C# `InitializeAsync(...)` signatures against YAML
- Fail fast with diff-like output
- Source: `ddi-constructive-criticisms-2026-02-15.md` (#4)

C2. Validate `assignment[].name` exists in the YAML model
- Source: `DDI/IMPROVEMENT_NOTES.md` (Stronger type checking)

C3. Validate dotted-property references
- Property exists on concrete type
- Property is declared in YAML model
- Consider interface exposure rules for `exposeToMauiDi: true`
- Source: `ddi-constructive-criticisms-2026-02-15.md` (#5)

Implementation notes: `DDI_Changes/ImplementationNotes/DDI_C3_DottedPropertyReferences.md`

### D) Initialization semantics / async startup

D1. Reduce sync-over-async and “global init gate” behavior
- Two-phase startup (create sync, init async)
- Per-service readiness gates
- Source: `ToDo_Candidates.txt` (#1, #2)

Status: implemented (D1)

Implementation notes: `DDI_Changes/ImplementationNotes/DDI_D1_AsyncStartupTwoPhase.md`

D2. Standardize async guidance (timeouts, cancellation, ConfigureAwait)
- Source: `ToDo_Candidates.txt` (#3)

Status: implemented (D2)

Implementation notes: `DDI_Changes/ImplementationNotes/DDI_D2_AsyncGuidance_CancellationTimeouts.md`

D3. Generator should pass `CancellationToken` (not CTS) and support properties on class declarations
- Source: `ToDo_Candidates.txt` (#6)

Status: implemented (D3)

Implementation notes: `DDI_Changes/ImplementationNotes/DDI_D3_CancellationTokenValues_AndModelProperties.md`

D4. Make two-phase init safer by contract
- Ensure `InitializeAsync` called exactly once
- Fail fast on use-before-init
- Source: `ddi-constructive-criticisms-2026-02-15.md` (#3)

Status: implemented (D4)

Implementation notes: `DDI_Changes/ImplementationNotes/DDI_D4_InitOnce_UseBeforeInitGuards.md`

### E) Diagnostics

E1. Better generator diagnostics output
- instance creation/init trace
- parameter binding report (which YAML assignment bound which parameter)
- first-failure root cause
- Source: `ddi-constructive-criticisms-2026-02-15.md` (#8)

E2. Generation report
- initializer call graph
- unused namespace model entries
- Source: `DDI/IMPROVEMENT_NOTES.md`

Status: implemented (E2)

### F) YAML ergonomics

F1. Support explicit factory method bindings
- Source: `DDI/IMPROVEMENT_NOTES.md`

Status: implemented (F1, Option 2)

F2. Compact syntax for simple literal assignments
- Source: `DDI/IMPROVEMENT_NOTES.md`

Status: implemented (F2)

F3. Format YAML aligned with YamlDotNet defaults
- Source: `DDI/IMPROVEMENT_NOTES.md`

Status: implemented (F3)

F4. “Sort instance list by dependency” mode
- Source: `DDI/IMPROVEMENT_NOTES.md`

Status: implemented (F4)

### G) Documentation housekeeping

G1. Consolidate DDI items from `ToDo_Candidates.txt` into `TODO.md`
- Source: `TODO.md` (DDI / initialization section)

Status: implemented (G1)

## Suggested prioritization (starting point)

1. A1 + B1 + B2 + C1 (make graphs and signatures safe; reduce churn)
2. E1/E2 (improve debuggability)
3. A2/A3 (IDE workflow)
4. F-series (ergonomics)

## Notes

- “Generate on save” can be explored later, but MSBuild incremental generation (A1) is the first step toward a reliable, team-friendly workflow.
- Any build-integrated generation should avoid producing uncompilable intermediate states (validate first, then emit output).
