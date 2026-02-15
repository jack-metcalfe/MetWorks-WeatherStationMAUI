# Constructive criticisms of DDI (backlog)

Date: 2026-02-15

This document captures concrete pain points observed while wiring the WeatherStation MAUI app through Declarative DI (DDI), plus actionable improvements to address together.

## 1) Manual `instance:` ordering is a foot-gun
**Symptom:** the YAML `instance:` list is order-sensitive (“define before use”), which forces humans to manually maintain a topological sort.

**Impact:**
- Small refactors create unrelated breakage (forward references).
- Reviewing diffs becomes harder because “move-only” changes are semantically meaningful.

**Action items:**
- Add generator-side dependency graph construction and auto-ordering.
- Ensure emitted order is deterministic/stable (same graph => same output) to avoid noisy diffs.
- Emit a clear error when manual ordering is invalid (and show the first invalid edge).

## 2) Cycles are easy to create and hard to diagnose
**Symptom:** cycles arise naturally (e.g., DB-backed logging vs components needed to create the DB). We hit a real cycle and had to change the logger used by `SqliteDatabaseOptionsFactory` to break it.

**Impact:**
- Cycles tend to appear late (during generation, or even runtime init).
- Fixes can require architectural knowledge and are not discoverable from the YAML alone.

**Action items:**
- Add explicit cycle detection with a precise path in diagnostics (e.g., `A -> B -> C -> A`).
- Provide remediation hints for common patterns (bootstrap logger, defer/late-bind DB logging, split responsibilities).

## 3) Two-phase initialization (`new()` + `InitializeAsync`) is brittle
**Symptom:** DDI prefers `new()` + `InitializeAsync(...)`. This enables codegen, but it makes “unsafe before init” object states common.

**Impact:**
- Guard boilerplate proliferates ("is initialized" flags, null checks).
- Failures can show up as delayed `InvalidOperationException` rather than a clean construction-time error.

**Action items:**
- Enforce a consistent “initialized contract” in generated code:
  - `InitializeAsync` called exactly once.
  - any access prior to init fails fast with a precise exception.
- Prefer a shared base pattern (e.g., `ServiceBase`-style initialization guard) where it fits.

## 4) YAML duplicates the truth that exists in C# (drift risk)
**Symptom:** class metadata in YAML must mirror actual C# initializer parameters and property shapes. We had to manually align YAML to the `LoggerSQLite.InitializeAsync` signature.

**Impact:**
- Drift is inevitable.
- Fixes are reactive (generated `.g.cs` becomes the “source of truth”).

**Action items:**
- Add build/codegen-time validation that reflects C# signatures and verifies YAML compatibility.
- Favor “fail fast with a diff-like message” over “generate broken code.”

## 5) Dotted-property references are fragile
**Symptom:** references such as `TheRootCancellationTokenSource.Token` and `TheStreamShippingHttpClientProvider.Client` rely on:
- the property existing on the underlying type, and
- the property being declared in YAML metadata.

**Impact:**
- Renames/refactors can silently break wiring.
- The safety of the reference depends on metadata discipline.

**Action items:**
- Add codegen-time validation:
  - property exists on the concrete type.
  - property is present in YAML `property:` list.
- Add a rule/validator for interface exposure:
  - if `exposeToMauiDi: true`, prefer that dotted refs go through members on the interface (or explicitly forbid dotted refs when the interface doesn’t expose it).

## 6) Lifetimes and cross-container expectations are underspecified
**Symptom:** `exposeToMauiDi: true` bridges DDI instances into MAUI DI, but the lifetime/scoping rules and guarantees are not explicit.

**Impact:**
- Risk of surprising multiple instances or mismatched lifetime expectations.
- Harder to reason about “who owns disposal” and background service shutdown.

**Action items:**
- Document intended lifetimes (singleton/transient) per instance.
- Consider adding explicit lifetime metadata to YAML, and generate consistent MAUI registrations.
- Decide and document who owns disposal (
  - DDI registry,
  - MAUI DI,
  - or shared/explicit disposal).

## 7) `CancellationTokenSource` as a service is a layering smell
**Symptom:** the object graph depends on a mutable control primitive (`CancellationTokenSource`) and uses dotted access to `.Token`.

**Impact:**
- Mutability becomes part of the DI graph.
- Injecting a `CancellationTokenSource` makes cancellation “global state” rather than an app-lifetime abstraction.

**Action items:**
- Prefer injecting `CancellationToken` directly from generated code where possible.
- Consider an “app lifetime” service abstraction if more behavior is needed than a token.

## 8) Diagnostics and debugging rely too heavily on reading generated code
**Symptom:** when wiring fails, the shortest path is often “inspect `.g.cs` output to see what it tried to do.”

**Impact:**
- Slows iteration.
- Couples developers to generator implementation details.

**Action items:**
- Improve generator diagnostics:
  - instance creation/init trace (“created”, “initialized”, “failed”, elapsed)
  - parameter binding report (which YAML assignment bound which initializer parameter)
  - first-failure root-cause errors

## Candidate prioritization (suggested)
1. Auto-ordering + graph diagnostics (`instance:`)
2. Cycle detection with actionable paths
3. YAML-vs-C# signature validation
4. Initialization safety guarantees
5. Dotted-property validation rules

