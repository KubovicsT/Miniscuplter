# Miniscuplter Handoff

> Immediate execution baton. Inspect actual Git/release/CI first; TECHNICAL_ROADMAP owns strategic direction.

Last updated: 2026-09-11

## Current state

- **Latest published stable:** `v1.0.22` at `c1ba2d01517cc6bca5a6e6cde3b1e85f853dd0d7` (immutable).
- **Current development branch:** `v1.0.23`.
- **Latest fully validated bounded code/test checkpoint before documentation commits:** `4538ab0e3251bb84449318a1bd43d9986d94e6d9`.
- **Overall completion:** **57% acceptance-weighted**.
- **Release state:** DEVELOPMENT CONTINUES / NO v1.0.23 RELEASE FREEZE. Dev must not create release requests/tags/releases.
- **Critical path:** released-v1.0.22 Stage-C reference-machine acceptance. Any reproduced correctness/persistence/viewport/data-safety/storage/cancellation regression preempts UI fallback work.

## What this Dev Cycle completed

With no new reference-machine evidence available and no release freeze, this run implemented exactly one Coordinator-approved MS-027 fallback slice: **unified AI command/history/dispatcher**.

New stable version-neutral `Scripts/Main.AiCommandConsole.cs`:

- adds one always-visible AI command line directly below the main toolbar;
- provides Up/Down keyboard history plus explicit previous/next controls and a compact history indicator;
- accepts existing slash commands by delegating to `ExecuteV096CommandAsync`;
- accepts simple typed prefixes (`concept`, `edit`, `select`, `3d`, `detail3d`) and plain prompts;
- exposes clearly named buttons for Generate 2D Concept, Edit Selected Region, Smart Select, Generate 3D, and Detail 3D Preview;
- routes all console AI buttons through one `DispatchAiConsoleActionAsync` dispatcher;
- delegates to existing authoritative action owners rather than creating another implementation path:
  - `GenerateConcept()`;
  - `V096EditCommand(...)`;
  - `SmartSelectV096Async(...)`;
  - `V1020Generate3DAsync()` for identity-bound Stage-C candidate generation;
  - `V098Detail3DAsync(...)` for the existing non-destructive detail preview;
- writes prompt text back to the existing `_prompt` backing field so old provider/action code receives the same user intent;
- creates no second project store, candidate state, provider runner, camera/input state, or generation event owner.

`ExtrasInstaller` composes `InstallAiCommandConsole()` after the earlier v1.0.23 preference/tool-strip/hierarchy/view-cube presentation slices.

Commits:
- `fc9d7aa411bdb386770e7cd075af3586d6ce88dd` — unified command/history/dispatch layer;
- `4538ab0e3251bb84449318a1bd43d9986d94e6d9` — final composition.

## Validation

Exact code checkpoint `4538ab0e3251bb84449318a1bd43d9986d94e6d9` passed:

- `core-foundation` run `34540497710`: **PASS**;
- `build` run `34540497715` dotnet job: **PASS** — editor, launcher, updater and Core tests build/run;
- Python compile/dependency resolution: **PASS**;
- core/execution/job regressions: **PASS**;
- geometry regressions: **PASS**;
- release audit: **PASS**;
- portable package layout + ZIP SHA-256: **PASS**;
- installer-definition compilation: **PASS**.

Full Windows/Godot release and publication jobs were correctly skipped because this is an ordinary development-branch push. `4538ab0...` is a useful validated checkpoint only; it is **not** a release decision and does not freeze v1.0.23.

## Senior-engineer self-review

- Stage-C generation remains owned by `V1020Generate3DAsync`; the console calls that method directly and does not attach another handler to the Generate-3D button.
- Slash commands continue to use the existing V096 executor instead of a parallel command implementation.
- Smart Select, regional edit and detail preview retain their existing selection/revision/provider semantics.
- The console stores only transient UI history in memory; no durable Core/project state is duplicated.
- No storage path, runtime/provider install state, cancellation state or release-control behavior changed.

## Primary next task — always check this first

Consume any new user/reference-machine results from released **v1.0.22**. Any serious Stage-C/persistence/viewport/storage/cancellation regression immediately supersedes MS-027.

Target acceptance sequence:

1. accepted 2D baseline;
2. intended local/Hunyuan 3D generation;
3. visible Ready/Conflict candidate;
4. explicit Apply;
5. save → close → reopen → same durable object and active revision;
6. Move/Rotate/Scale + one supported sculpt/edit path;
7. cleanup + exact STL export;
8. right-panel and whole-window resize stability;
9. no starter sphere / opaque floor regression;
10. storage containment plus provider/resource and cancellation/recovery evidence where practical.

Relevant issues: **MS-023, MS-018, MS-009, MS-024, MS-025, MS-013, MS-022, MS-004**.

## If reference-machine evidence is still unavailable

If branch state is green, no release freeze exists and no higher-priority issue is unblocked, continue exactly one next Coordinator-approved MS-027 slice:

**Next fallback slice: MS-026 performance/resource telemetry panel.**

Guardrails:
- local-only, low-overhead sampling around 1 Hz;
- GPU utilization, dedicated VRAM, system RAM, GPU temperature and useful CPU values only where actually available;
- show active provider/current AI stage/elapsed time and compact observed peaks when available;
- gracefully omit unsupported sensors rather than inventing values;
- do not materially slow inference;
- do not create a second AI job/provider state owner; observe existing state only;
- stop after this bounded slice before density/spacing cleanup.

## Documentation / release rules

- Keep completion at **57%** until new acceptance evidence justifies a change.
- Do not edit `TECHNICAL_ROADMAP.md`, `COORDINATOR_LOG.md` or `AUTOMATION_MANAGER.md` from Dev unless required for execution safety.
- Release readiness, chunk size, freeze and publication belong exclusively to the Project Coordinator.
- A validated checkpoint is advisory and does not freeze the branch.

## User dependency

No product/design decision is required. The external dependency remains target-machine verification of released v1.0.22. Autonomous engineering may continue under the Coordinator fallback while that evidence is unavailable.
