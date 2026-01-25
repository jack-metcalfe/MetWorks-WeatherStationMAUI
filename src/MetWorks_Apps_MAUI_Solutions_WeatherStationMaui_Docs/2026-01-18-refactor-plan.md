# Refactor Plan — Shared Infrastructure (MetWorks.Common)
Date: 2026-01-18

Summary
- Create a small shared assembly (recommended name: `MetWorks.Common` / `MetWorks.Infrastructure`) that contains reusable infrastructure:
  - `ServiceBase` (cancellation, background task helpers, disposal)
  - `LoggerResilient` (resilient, buffered multi-backend logger) and `LoggerStub`
  - `EventRelayPath` and small event-relay helpers
  - Core infra interfaces (e.g., `ILogger`, `IEventRelayBasic`, `IEventRelayPath`) if not already shared
- Keep concrete application services (Transformer, ListenerSink, WeatherDataTransformer) in their existing projects and migrate them incrementally to reference the new common assembly.
- Maintain compilation & CI while migrating: add compatibility overloads / adapters where needed and mark old APIs `[Obsolete]` until removed in a major version.

Goals
- Reduce duplication of cancellation and background-task logic.
- Provide a single resilient logging entrypoint used by all components.
- Enable focused unit testing for infra with fast CI feedback.
- Keep migration incremental and reversible.

High-level migration steps
1. Create new project
   - `src/MetWorks.Common/MetWorks.Common.csproj` targeting `net10.0`.
   - Minimal project, no UI dependencies.
2. Move or copy infra sources into the new project (start by copying to keep originals until tests pass):
   - `ServiceBase.cs` (cancellation helpers, StartBackground, WaitForBackgroundTasksAsync)
   - `LoggerResilient.cs` and `LoggerStub.cs`
   - `EventRelayPath.cs`
   - Core interfaces used by infra (if suitable)
3. Update namespaces to `MetWorks.Common` (or `MetWorks.Common.Logging`, `MetWorks.Common.EventRelay`) for clarity.
4. Add `ProjectReference` from consuming projects (services and MAUI app) to `MetWorks.Common`.
5. Register shared singletons in DI / registry:
   - Register a single `LoggerResilient` instance early.
   - Register a host `CancellationTokenSource` (host-owned) and optionally register its `Token` for injection.
6. Migrate services incrementally:
   - Replace ad-hoc token creation with `InitializeBase(logger, externalCancellation)` call.
   - Use `StartBackground` to run loops (replace `Task.Run(..., token)`).
   - Remove duplicated CTS disposal and token linking when using base class.
   - Keep old API overloads (forwarders) for a short period; mark with `[Obsolete(...)]`.
7. Add unit tests for infra (in `tests/MetWorks.Common.Tests`):
   - `LoggerResilient` buffering/flush behavior.
   - `ServiceBase` background task / cancellation behavior.
   - `EventRelayPath` simple registration/send behavior.
8. Update CI (GitHub Actions) to build and run tests for the new project and existing projects.
9. After migration and verification, remove compatibility wrappers and perform major-version bump if desired.

Compatibility guidance
- Public service APIs should accept `CancellationToken` (not `CancellationTokenSource`). Host creates and owns the CTS and passes the Token.
- For DDI / generated initializers: map CTS instances to `.Token` when calling an `InitializeAsync` expecting a `CancellationToken`.
- During migration, provide compatibility overloads that accept old CTS-based inputs and forward to the token-based API:

// compatibility helper public Task<bool> InitializeAsync(..., CancellationTokenSource externalCts, ...) => InitializeAsync(..., externalCts?.Token ?? CancellationToken.None, ...);
- Mark such overloads `[Obsolete("Use InitializeAsync(..., CancellationToken) instead.")]` and remove after migration.

Testing and CI
- Add `tests/MetWorks.Common.Tests` using xUnit:
  - Fast unit tests for buffering, flush, and cancellation.
- CI pipeline stages:
  1. Restore and build all projects.
  2. Run unit tests (fast).
  3. Optional integration tests (separate job / schedule) for UDP/DB behavior.
- Keep test scope small during refactor: validate infra behavior first, then incrementally add service-level tests.

Logging & runtime behavior
- Use `LoggerResilient` as the single logging entrypoint. It:
  - Buffers messages when no backend attached.
  - Fans out to multiple backends when attached.
  - Silently drops oldest buffered messages when exceeding capacity (configurable).
  - Never throws to callers; resilient to backend failures.
  - When a setting indicates a new logger backend, instantiate the backend on a background thread and call `LoggerResilient.AddLogger(...)`. Remove old backend via `RemoveLogger(...)` (only dispose if the service created it).

Minimal immediate tasks (next 1–2 days)
- Create `src/MetWorks.Common` project with `ServiceBase.cs` and `LoggerResilient.cs`.
- Add `tests/MetWorks.Common.Tests` and unit test for `LoggerResilient`.
- Wire `LoggerResilient` singleton into the registry/MAUI DI.
- Refactor `Transformer` to call `InitializeBase(...)` and `StartBackground(...)` (demo adoption).
- Run CI locally and on PR.

Where this file is saved
- Saved to repository: `src/docs/2026-01-18-refactor-plan.md`

Notes
- Incremental migration preserves CI and reduces risk.
- This plan follows .NET conventions (CancellationToken in public APIs, components create linked CTS internally).
- We will avoid moving concrete services into the common assembly initially; move them later only if functional coupling justifies it.

Next action (if you approve)
- I will generate the `MetWorks.Common` project skeleton, add `ServiceBase.cs` and copy `LoggerResilient.cs` into it, and create a starter unit test for `LoggerResilient`. Then update DI registration in the MAUI project to register `LoggerResilient` and the host CTS.