# Miniscuplter Handoff

> Immediate execution baton. Inspect actual Git/release/CI first; TECHNICAL_ROADMAP owns strategic direction.

Last updated: 2026-09-10

## Current state

- **Latest published stable:** `v1.0.22` at `c1ba2d01517cc6bca5a6e6cde3b1e85f853dd0d7` (immutable).
- **Current development branch:** `v1.0.23`.
- **Latest fully validated bounded code/test checkpoint before documentation commits:** `ae1b081f195332028a2ad929381236030fcfefde`.
- **Overall completion:** **57% acceptance-weighted**.
- **Release state:** DEVELOPMENT CONTINUES / NO v1.0.23 RELEASE FREEZE. Dev must not create release requests/tags/releases.
- **Critical path:** released-v1.0.22 Stage-C reference-machine acceptance. Any reproduced correctness/persistence/viewport/data-safety/storage/cancellation regression preempts UI fallback work.

## What this Dev Cycle completed

With no new reference-machine evidence available and no release freeze, this run implemented exactly one Coordinator-approved MS-027 fallback slice: **view cube + selected-object orbit pivot**.

### View cube

New stable version-neutral `Scripts/Main.ViewCube.cs`:

- overlays a compact camera-linked view selector in the existing `ViewportHost`;
- exposes six orthographic face snaps: Front / Back / Left / Right / Top / Bottom;
- repositions/filters face controls from the existing live camera basis so the selector reflects camera orientation;
- snaps by changing the existing `_yaw` / `_pitch` state and calling the existing `UpdateCamera()` owner;
- does not create another `Camera3D`, project state, viewport sizing owner or tool/input state machine.

### Selected-object orbit pivot

The view-cube layer observes RMB press on the existing viewport input surface only to prepare the pivot before V1018 orbit motion:

- when a valid object is selected, the selected object's global AABB center becomes `_focus`;
- current camera position is preserved;
- existing `_distance`, `_pitch` and `_yaw` are recomputed from the current camera position to the new focus before orbit motion, preventing a pivot-change jump;
- when no object is selected, existing empty-scene focus behavior is untouched;
- V1018 remains the owner of `_orbiting`, mouse motion, panning, zoom and viewport tools.

Composition: `ExtrasInstaller` calls `InstallViewCube()` after the earlier v1.0.23 preference/tool-strip/hierarchy presentation slices.

Commits:
- `8264391a70c49652f036cc749f447a9f0ce715ca` — initial view cube;
- `8e926f11ca7fc60bff125947c460d439c5efcb6c` — selected-orbit-pivot observer;
- `ce38692e29d60b47a8716422d2f6553eb5856eb8` — composition;
- `58f3c2e5ea19eeb2379a874a7199d717c104c115` — initial wiring regression guard;
- `ae1b081f195332028a2ad929381236030fcfefde` — corrected wiring guard.

## Failed attempt retained

`core-foundation` run `34535187018` at `58f3c2e...` failed the newly added static guard with `front/back face snaps missing`. The product implementation was present; the test incorrectly searched for literal runtime button names even though names are generated from `AddViewCubeFace(...)`. Commit `ae1b081...` corrected the assertion to inspect the six actual registrations. Do not reinterpret this as a product/runtime failure.

## Validation

Exact code/test checkpoint `ae1b081f195332028a2ad929381236030fcfefde`:

- `core-foundation` run `34535418564`: **PASS**;
- `build` run `34535418525` dotnet job: **PASS** — editor, launcher, updater and Core tests build/run;
- Python compile/dependency resolution: **PASS**;
- core/execution/job regressions: **PASS**;
- geometry regressions: **PASS**;
- release audit: **PASS**;
- portable package layout + ZIP SHA-256: **PASS**;
- installer-definition compilation: **PASS**.

Full Windows/Godot release and publication jobs are not part of ordinary development pushes. `ae1b081...` is a useful validated checkpoint only; it is **not** a release decision and does not freeze v1.0.23.

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

**Next fallback slice: unified AI command/history/dispatcher.**

Guardrails:
- converge existing AI actions on one authoritative dispatcher; do not create parallel generation/edit ownership;
- provide one primary command entry/history surface with Up/Down history navigation and previous/next controls;
- expose clearly named buttons only for real existing actions;
- reuse existing Stage-C candidate/apply and provider execution paths;
- no durable project-state duplication;
- stop after this bounded slice, before MS-026 telemetry/density polish.

## Documentation / issue notes

- `PROJECT_STATUS.md` is reconciled through the view-cube checkpoint.
- No new product issue ID was created for the failed `58f3c2e...` assertion because it was a development test false negative detected and corrected within this slice; its failed evidence is preserved here.
- Existing MS-028 is already RESOLVED in the Coordinator roadmap; do not reopen it for this unrelated static-test mistake.
- Do not edit `TECHNICAL_ROADMAP.md`, `COORDINATOR_LOG.md` or `AUTOMATION_MANAGER.md` from Dev unless required for execution safety.

## Release ownership / continuous development

Release readiness, release chunk size, freeze and publication belong exclusively to the Project Coordinator.

Dev may record exact validated checkpoints and continue roadmap work. A checkpoint is advisory and does not freeze the branch. Only an actual Coordinator release-control request freezes the source semantic-version branch. If Coordinator later freezes v1.0.23 and creates/identifies a forward branch from that SHA, continue work only on the forward branch while publication runs.

## User dependency

No product/design decision is required. The external dependency remains target-machine verification of released v1.0.22. Autonomous engineering may continue under the Coordinator fallback while that evidence is unavailable.
