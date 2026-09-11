# Miniscuplter Handoff

> Immediate execution baton. Inspect actual Git/release/CI first; TECHNICAL_ROADMAP owns strategic direction.

Last updated: 2026-09-11

## Current state

- **Latest published stable:** `v1.0.22` at `c1ba2d01517cc6bca5a6e6cde3b1e85f853dd0d7` (immutable).
- **Current development branch:** `v1.0.23`.
- **Latest fully validated bounded code/test checkpoint before documentation commits:** `e110a746a1dd9a470c7d05de80de684edfcf1f27`.
- **Overall completion:** **57% acceptance-weighted**.
- **Release state:** DEVELOPMENT CONTINUES / NO v1.0.23 RELEASE FREEZE. Dev must not create release requests/tags/releases.
- **Critical path:** released-v1.0.22 Stage-C reference-machine acceptance. Any reproduced correctness/persistence/viewport/data-safety/storage/cancellation regression preempts UI fallback work.

## What this Dev Cycle completed

With no new reference-machine evidence and no release freeze, this run implemented exactly one Coordinator-approved fallback slice: **MS-026 local resource telemetry/performance panel**.

New version-neutral `Scripts/Main.ResourceTelemetry.cs`:

- samples at approximately 1 Hz;
- displays rolling GPU utilization, dedicated VRAM, system RAM and Miniscuplter-process CPU graphs;
- displays NVIDIA GPU temperature where `nvidia-smi` supplies it;
- obtains GPU/VRAM/temperature from `nvidia-smi` with a bounded async timeout rather than inventing unavailable metrics;
- obtains Windows physical-memory utilization from `GlobalMemoryStatusEx`;
- derives app CPU from `Process.TotalProcessorTime`, normalized by logical processor count;
- shows the existing active provider/current AI stage/elapsed time by observing `_v1093D*`, `_v108Ai*` and `_v1017Image*` state only;
- resets and records compact observed peak GPU/VRAM/RAM/temperature values on each detected AI-job start;
- gracefully degrades GPU/VRAM to unavailable after repeated query failures;
- catches telemetry failures so monitoring cannot break inference or modeling;
- creates no second job queue, provider state, project state, cancellation owner, runtime state or persistent data path.

`ExtrasInstaller` composes `InstallResourceTelemetry()` after the unified AI command console.

Commits:
- `6bc6d379a8db20f9f9a934ffc504f9cc64501904` — local resource telemetry panel;
- `0fd8b03db0f9cde3ea23021523c8e3cc3da5a455` — compose telemetry into the final workspace;
- `e110a746a1dd9a470c7d05de80de684edfcf1f27` — Godot C# nested-Control declaration correction.

## Validation

Exact code checkpoint `e110a746a1dd9a470c7d05de80de684edfcf1f27` passed:

- `core-foundation` run `34545123188`: **PASS**;
- broader `build` run `34545123110`: **PASS**;
- editor, launcher, updater and Core C# builds/tests: **PASS**;
- Python compile/dependency resolution: **PASS**;
- core/execution/job regressions: **PASS**;
- geometry regressions: **PASS**;
- release audit: **PASS**;
- portable package build/layout + ZIP SHA-256: **PASS**;
- installer-definition compilation: **PASS**.

Failed attempt preserved: exact head `0fd8b03...` passed Python/core/execution/geometry/release-audit work but failed editor C# compilation with Godot diagnostic `GD0001` because nested `ResourceSparkline : Control` lacked the required `partial` modifier. `e110a746...` corrected only that declaration and restored green validation.

This is a useful validated checkpoint only. It does **not** freeze v1.0.23 and no publication action was initiated.

## Senior-engineer self-review

- Telemetry is presentation/observability only and reads existing job/provider fields; it does not own job or provider state.
- GPU queries are asynchronous, timeout-bounded and approximately 1 Hz; failure is isolated from inference.
- Unsupported GPU sensors are omitted instead of fabricated.
- RAM and app-CPU sampling do not write outside Miniscuplter storage because they write nothing at all.
- No Core project state, Stage-C candidate state, mesh/revision state, camera/tool state, cancellation state or storage location changed.
- Observed peaks are explicitly sampling-derived, not claimed as guaranteed hardware maxima.
- Real GTX 1080 overhead/value accuracy still requires target-machine observation; CI cannot prove that.

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

If branch state remains green, no Coordinator release freeze exists and no higher-priority issue is unblocked, continue exactly one next Coordinator-approved MS-027 slice:

**Next fallback slice: density/spacing cleanup and retirement of superseded explanatory UI.**

Guardrails:
- keep the viewport dominant;
- remove or collapse only nonessential always-visible explanatory copy already covered by tooltips/status/error surfaces;
- preserve important state, progress, errors and destructive-action warnings visibly;
- do not rewrite the UI or create another composition owner;
- preserve existing Stage-C, viewport tool, hierarchy, view-cube, AI dispatcher and telemetry ownership;
- keep accessibility and minimum usable panel sizes;
- stop after this bounded polish slice.

## Documentation / release rules

- Keep completion at **57%** until new acceptance evidence justifies a change.
- Do not edit `TECHNICAL_ROADMAP.md`, `COORDINATOR_LOG.md` or `AUTOMATION_MANAGER.md` from Dev unless required for execution safety.
- Release readiness, chunk size, freeze and publication belong exclusively to the Project Coordinator.
- A validated checkpoint is advisory and does not freeze the branch.

## User dependency

No product/design decision is required. The external dependency remains target-machine verification of released v1.0.22. During a long AI job, also observe the new resource panel for plausible GPU/VRAM/RAM/temperature values and whether ~1 Hz monitoring has any noticeable performance impact. Autonomous engineering may continue under the Coordinator fallback while that evidence is unavailable.
