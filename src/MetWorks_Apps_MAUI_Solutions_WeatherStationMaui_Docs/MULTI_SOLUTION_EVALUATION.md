# Multi-Solution Evaluation

This document evaluates whether the current 38-project single-solution should be split into multiple Visual Studio solutions, and if so, how.

See also:
- Current architecture: `SOLUTION_ARCHITECTURE.md`
- Dependency cleanup: `DEPENDENCY_CLEANUP.md`

---

## Context: what a solution split means

A Visual Studio **solution** (`.slnx` / `.sln`) is primarily a workspace grouping — it controls which projects appear together in the IDE and in a CI build invocation. A project that lives in solution A can still reference a project in solution B, but there are practical consequences:

- Projects in different solutions cannot be referenced by relative path unless they share a common file system root (or are consumed as NuGet packages).
- A "multi-solution" approach typically pairs with **NuGet packages** for cross-solution project references; otherwise, each solution needs to be cloned and built in the right order.
- Splitting a solution is most valuable when different parts have very different **build cadences**, **team ownership**, or **release lifecycles**.

---

## Current state

| Metric | Value |
|--------|-------|
| Solution files | 1 (`.slnx`) |
| Projects (production) | 33 |
| Test projects | 5 |
| Target frameworks | `net10.0`, `net10.0-android`, `net10.0-windows10.0.19041.0` |
| CI/CD | Single build pipeline |
| NuGet package publishing | None (all project references) |

All 38 projects compile from a single `git clone` and a single `dotnet build` invocation. There are no published NuGet packages for internal projects.

---

## Candidate groupings

Before evaluating split options, the projects naturally fall into three independent groups based on domain concern and build-time vs. runtime role:

### Group A — DDI framework tooling (build-time only)

These projects form a standalone code-generation tool. They have **no dependency on MetWorks domain code** and run only at code-generation time (MSBuild target in `MetWorks_DdiRegistry`).

- `MetWorks_DI_Declarative_EnumDefinitions`
- `MetWorks_DI_Declarative_Interfaces`
- `MetWorks_DI_Declarative_Diagnostics`
- `MetWorks_DI_Declarative_Loader`
- `MetWorks_DI_Declarative_Resources`
- `MetWorks_DI_Declarative_Generator`
- `MetWorks_DI_Declarative_CodeGenTool`
- `MetWorks_DI_Declarative_Loader_Tests`

This group is already separately maintained in a companion repository ([MetWorks-DeclarativeDI](https://github.com/jack-metcalfe/MetWorks-DeclarativeDI)) and is likely to diverge with independent versioning.

### Group B — Portable domain library

These projects have **no MAUI dependency, no database driver, no platform-specific code**, and could theoretically be published as NuGet packages and consumed from other .NET projects (e.g., a server-side ingest pipeline).

- `RedStar_Amounts`, `RedStar_Amounts_StandardUnits`, `RedStar_Amounts_WeatherExtensions`
- `MetWorks_EnumDefinitions`, `MetWorks_Constants`, `MetWorks_Interfaces`
- `MetWorks_EventRelay`, `MetWorks_Models_Observables`
- `MetWorks_Common_Utility`, `MetWorks_Common_Metrics`
- `MetWorks_IoT_UDP_Tempest`
- `MetWorks_Ingest`

> Note: `MetWorks_Common` is **not** in this group today because it has `Npgsql` as a direct dependency (see L1 in `SOLUTION_ARCHITECTURE.md`). Fixing L1 would allow the non-Postgres subset of `MetWorks_Common` to join Group B.

### Group C — App and infrastructure

Everything not in Group A or B: persistence layers, MAUI services, the generated registry, the MAUI application itself, and ingest sinks.

- `MetWorks_Common`, `MetWorks_Common_Settings`, `MetWorks_Common_Logging`
- `MetWorks_Data_Sqlite`, `MetWorks_Persistence`, `MetWorks_Persistence_SQLite`
- `MetWorks_Resource_Store`
- `MetWorks_InstanceIdentifier`
- `MetWorks_Ingest_SQLite`, `MetWorks_Ingest_Postgres`, `MetWorks_Ingest_Transformer`, `MetWorks_Ingest_StreamReceiver`
- `MetWorks_Networking_Udp_Transformer`
- `MetWorks_Maui_Services`, `MetWorks_DdiRegistry`
- `MetWorks_Apps_MAUI_WeatherStationMaui`

---

## Options

### Option 1 — Keep single solution (current)

All 38 projects remain in one `.slnx`.

**Pros**
- Zero overhead — every developer gets the full build with one `git clone`.
- Instant cross-project navigation in the IDE.
- No NuGet packaging, versioning, or feed management required.
- CI/CD is a single pipeline step.
- Refactoring across tiers requires no coordination between repositories or package versions.

**Cons**
- Full solution build takes longer as the project count grows (though incremental rebuilds are fast).
- DDI framework changes are coupled to the app release cycle.
- No forced API boundary between domain code and app code — accidental coupling can creep in undetected.
- The MAUI workload must be installed even to build Group A or Group B projects.

**Verdict:** Appropriate for the current team size and velocity. The main risk is gradual coupling, which is better mitigated by dependency hygiene (see `DEPENDENCY_CLEANUP.md`) than by solution splitting.

---

### Option 2 — Extract DDI framework to its own solution

Move Group A into the already-existing `MetWorks-DeclarativeDI` companion repository and consume it as a NuGet package (or via a `global.json`-pinned tool).

```
MetWorks-DeclarativeDI.slnx       (Group A — DDI tooling)
  └─ publishes: MetWorks.DI.Declarative.CodeGenTool (NuGet tool package)

MetWorks_Apps_MAUI_Solutions_WeatherStationMaui.slnx  (Groups B + C)
  └─ consumes DDI tool via MSBuild <PackageReference> or <DotNetCliToolReference>
```

**Pros**
- DDI framework can evolve and be versioned independently.
- App solution build only needs to restore the pre-built tool package.
- Other projects (future) can consume the same DDI framework.

**Cons**
- Adds a two-step release cycle: change DDI → publish package → bump version in app solution.
- Local DDI changes require either a local NuGet feed or a path override in `NuGet.Config`.
- More friction for exploratory DDI changes made alongside app changes.

**Verdict:** Worth doing once the DDI framework stabilizes and its API surface settles. Not urgent now — the companion repo already exists and could adopt package publishing incrementally.

---

### Option 3 — Extract portable domain library to its own solution

Move Group B into a separate solution that publishes NuGet packages to a private feed, then consume those packages from the app solution.

```
MetWorks-Domain.slnx              (Group B — portable domain)
  └─ publishes: MetWorks.Interfaces, MetWorks.EventRelay, MetWorks.Constants, …

MetWorks_Apps_MAUI_Solutions_WeatherStationMaui.slnx  (Group C + app)
  └─ consumes domain packages via PackageReference
```

**Pros**
- Enforces a clean API boundary: changes to domain interfaces require a package version bump and deliberate consumption.
- Enables reuse of domain types in server-side tooling (e.g., a Linux ingest pipeline) without the MAUI workload.
- Smaller, faster build for the app solution.

**Cons**
- High friction for the current phase: domain interfaces change frequently alongside app code.
- Requires a private NuGet feed (GitHub Packages, Azure Artifacts, or a local feed).
- Cross-solution refactoring (rename an interface, add a property) requires changes to two repositories, a package publish, and a version bump before the app solution can compile.
- The current layering violations (L1, L2, L3, L4 in `SOLUTION_ARCHITECTURE.md`) must be resolved first, or the "portable" packages will still pull in MAUI and Npgsql.

**Verdict:** Desirable long-term goal, but premature until the domain library has a stable interface and the layering violations are fixed. Tackle dependency cleanup first, then reassess.

---

### Option 4 — Three-solution split (DDI + Domain + App)

Combine Options 2 and 3: DDI framework in its own solution, domain library in its own solution, app in the third.

**Pros**
- Maximum modularity and independent versioning.

**Cons**
- Maximum coordination overhead for a small team.
- Three repositories, three CI pipelines, three package versions to keep aligned.
- Any change touching all three layers (common case during active development) requires three PRs.

**Verdict:** Not recommended at current team size and development pace. Revisit after Option 2 (DDI extraction) is stable.

---

## Recommendation

| Priority | Action |
|----------|--------|
| **Now** | Stay in single solution; address the six layering concerns in `DEPENDENCY_CLEANUP.md` to keep coupling manageable. |
| **Near-term** | Adopt Option 2: formalize the `MetWorks-DeclarativeDI` companion repo and consume the DDI code-gen tool as a NuGet tool package. This decouples the tool versioning with minimal cross-repo friction. |
| **Long-term** | Reassess Option 3 (domain library extraction) once `MetWorks_Interfaces` and core domain types have stabilized and the MAUI/Npgsql leakage is eliminated from the domain tier. |

---

## Decision log

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-03-28 | Remain in single solution for now | Domain interfaces and app code change together; coordination overhead of splitting outweighs benefit at current velocity |
| 2026-03-28 | Plan DDI tool extraction (Option 2) | DDI framework already lives in companion repo; formalizing NuGet packaging is low risk and high value |
