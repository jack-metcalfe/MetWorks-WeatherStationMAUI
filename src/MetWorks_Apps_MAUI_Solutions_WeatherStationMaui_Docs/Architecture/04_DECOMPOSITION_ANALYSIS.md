# Decomposition Analysis

> **Purpose**: Evaluate whether the single solution should be split into multiple solutions, and if so, how.

---

## Current State

One solution (`MetWorks-WeatherStationMAUI.sln`) containing ~35 projects that produce four executables with **three distinct dependency islands**:

| Island | Projects | Shared Code |
|--------|----------|-------------|
| MAUI App | ~25 (via DdiRegistry) | All MetWorks runtime libraries |
| Server-Side Ingest | 2 (StreamReceiver + QueueWorker) | None — standalone |
| DDI Toolchain | 5-6 (Generator + DDI libs) | DDI subsystem only |

These islands have **no runtime code overlap** with each other.

---

## Option A: Keep Single Solution (Status Quo + Cleanup)

### Approach
Keep one `.sln` file. Address dependency issues (F1–F4 from the audit) through internal refactoring. Use solution folders to visually organize the islands.

### Pros
- **Single clone, single open**: Developers see everything in one IDE window.
- **Atomic commits**: Cross-cutting changes (e.g., interface renames) happen in one commit.
- **Simpler CI**: One build pipeline builds and tests everything.
- **Refactoring tools work across the entire codebase**: Rename, find-all-references, etc.

### Cons
- **Cognitive overhead**: ~35 projects is a lot to navigate, especially for contributors focused on only one island.
- **Build-all cost**: Building server-side projects when only working on MAUI (and vice versa).
- **Blurred boundaries**: Easy to accidentally add cross-island references because everything is visible.
- **Dependency issues remain internal**: Without solution boundaries, there is no enforcement that islands stay separate.

### When to Choose
- Team is small (1-3 developers) and works across all islands.
- Refactoring is frequent and touches shared interfaces.
- CI build times for the full solution are acceptable (< 2 minutes).

---

## Option B: Split Into Three Solutions

### Approach
Create three separate solutions corresponding to the three dependency islands:

1. **MetWorks-WeatherStationMAUI.sln** — MAUI app + all runtime libraries + DdiRegistry + docs
2. **MetWorks-ServerIngest.sln** — StreamReceiver + QueueWorker (+ optional shared ingest library)
3. **MetWorks-DeclarativeDI.sln** — DDI Generator + all DDI libraries + DDI tests

### Pros
- **Hard boundaries**: Impossible to accidentally add cross-island dependencies.
- **Focused developer experience**: Each solution contains only relevant projects.
- **Independent CI**: Server-side can deploy without rebuilding MAUI, and vice versa.
- **Independent versioning**: DDI toolchain can version separately from the app.
- **Reduced build times per solution**: Each solution builds only its own projects.

### Cons
- **Cross-solution refactoring is harder**: Renaming an interface in DDI requires coordinated changes in the DDI solution and the consuming solution.
- **Dependency management complexity**: DDI Generator must be published as a tool (NuGet package or path reference) so DdiRegistry's MSBuild target can invoke it.
- **Multiple clones/repos**: Developers may need to have all three repos cloned locally.
- **Shared contracts**: If server-side projects ever need MetWorks shared code, a NuGet package or shared project reference would be needed.

### Migration Steps
1. Move `StreamReceiver` and `QueueWorker` to a new repo/solution (zero risk — see Audit F5).
2. DDI toolchain already has its own repo (`MetWorks-DeclarativeDI`). Ensure the MAUI solution references the DDI Generator as a tool rather than a project reference.
3. Remove DDI source projects from the MAUI solution, keeping only `DdiRegistry` (which consumes the tool output).

### When to Choose
- Team has role specialization (e.g., one person works on server ingest, another on mobile).
- CI build times for the full solution are problematic.
- You want to enforce island boundaries at the repository level.

---

## Option C: Hybrid — Two Solutions + Workspace

### Approach
Split server-side ingest into its own solution (the lowest-risk move), keep DDI toolchain in the main solution, and use a Visual Studio workspace (`.slnf` solution filters or a multi-repo workspace) for full-solution views.

1. **MetWorks-WeatherStationMAUI.sln** — MAUI app + runtime libraries + DDI toolchain + DdiRegistry + docs + tests
2. **MetWorks-ServerIngest.sln** — StreamReceiver + QueueWorker

### Pros
- **Lowest migration effort**: Only server-side projects move.
- **No cross-solution tool publishing needed**: DDI stays internal.
- **Removes the most obvious noise** (server-side projects that share nothing).
- **Solution filters** (`.slnf`) can further narrow the view for developers who don't need to see DDI or docs.

### Cons
- **DDI boundary not enforced**: DDI projects remain in the same solution, so accidental coupling is still possible.
- **MAUI solution is still large** (~30 projects).

### When to Choose
- You want a pragmatic first step with near-zero risk.
- DDI refactoring is still evolving and benefits from being co-located with the app.
- Team is small and primarily focused on the MAUI app.

---

## Recommendation

**Start with Option C (Hybrid)**, then evaluate Option B later:

### Phase 1: Immediate (Low Effort, Zero Risk)
1. Create `MetWorks-ServerIngest.sln` with StreamReceiver + QueueWorker.
2. Remove those two projects from the main solution.
3. Create solution filters (`.slnf`) for the main solution:
   - `MAUI-App-Only.slnf` — MAUI app + runtime libraries only
   - `DDI-Toolchain.slnf` — DDI projects + tests only
   - `Full.slnf` — everything

### Phase 2: After Dependency Cleanup (Medium Effort)
1. Address F1–F3 from the dependency audit (decouple Common_Settings, Common, Common_Logging).
2. Re-evaluate whether DDI should move to its own solution once the toolchain is stable.
3. Consider splitting DdiRegistry (F4) if build times become problematic.

### Phase 3: Future Evaluation
1. If the team grows or specializes, move to full Option B.
2. If DDI toolchain stabilizes and versions independently, split it out.
3. Monitor build times and developer friction as the codebase evolves.

---

## Decision Criteria Checklist

| Criterion | Single Sln | Three Slns | Hybrid |
|-----------|-----------|-----------|--------|
| Easy to start | ✅ Already there | ❌ Migration effort | ✅ Minimal effort |
| Enforced boundaries | ❌ | ✅ | ⚠️ Partial |
| Cross-cutting refactor ease | ✅ | ❌ | ⚠️ |
| Focused developer experience | ❌ | ✅ | ✅ (with .slnf) |
| CI independence | ❌ | ✅ | ⚠️ Partial |
| Small team friendly | ✅ | ⚠️ | ✅ |
