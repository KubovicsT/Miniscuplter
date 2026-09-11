# Miniscuplter Handoff

> Immediate execution baton. Inspect actual Git/release/CI first; TECHNICAL_ROADMAP owns strategic direction.

Last updated: 2026-09-11

## Current state

- **Latest published stable:** `v1.0.22` at `c1ba2d01517cc6bca5a6e6cde3b1e85f853dd0d7` (immutable).
- **Current development branch:** `v1.0.23`.
- **Latest fully validated bounded code/test checkpoint before documentation commits:** `1665a9c9fabd8a3d0f1f4f8ed79411f749e158f6`.
- **Overall completion:** **57% acceptance-weighted**.
- **Release state:** DEVELOPMENT CONTINUES / NO v1.0.23 RELEASE FREEZE. Dev must not create release requests/tags/releases.
- **Critical path:** released-v1.0.22 Stage-C reference-machine acceptance. Any reproduced correctness/persistence/viewport/data-safety/storage/cancellation regression preempts fallback work.

## What this Dev Cycle completed

With no new reference-machine evidence and no release freeze, this run implemented exactly one Coordinator-approved fallback slice: **MS-027 density/spacing cleanup and retirement of superseded explanatory UI**.

New version-neutral `Scripts/Main.WorkspaceDensity.cs`:

- shortens the empty 2D-workspace hint to a compact actionable message;
- removes two long always-visible instructional paragraphs from the 2D editing/context-aware editing sections;
- preserves the removed guidance through section/button hover tooltips;
- reduces spacing only inside those affected presentation sections;
- deliberately keeps workflow state, AI job state, errors/progress and destructive-action warnings visible;
- creates no project state, AI dispatcher, viewport-tool, camera, scene-selection, persistence or provider authority.

`ExtrasInstaller` composes `InstallWorkspaceDensity()` after resource telemetry so it observes the final current MS-027 workspace composition.

Commits:
- `d5ec6aca9a06c7fbc7934aeaffafd0884bccaf89` — compact instructional workspace copy;
- `28df9b6882b02f9acb7b37647523d8c722833c3e` — compose density layer;
- `1665a9c9fabd8a3d0f1f4f8ed79411f749e158f6` — focused static regression guard source.

## Validation

Exact code/test checkpoint `1665a9c9fabd8a3d0f1f4f8ed79411f749e158f6` passed:

- `core-foundation` run `34549024074`: **PASS**;
- broader `build` run `34549024087`: **PASS**;
- editor, launcher, updater and Core C# builds/tests: **PASS**;
- Python compile/dependency resolution: **PASS**;
- core/execution/job regressions: **PASS**;
- geometry regressions: **PASS**;
- release audit: **PASS**;
- portable package build/layout + ZIP SHA-256: **PASS**;
- installer-definition compilation: **PASS**.

`tools/ui_density_tests.py` records focused ownership/presentation invariants and is syntax-checked by the normal Python compile pass. The established branch CI remains the authoritative executed validation for build/runtime regression families.

This is a useful validated checkpoint only. It does **not** freeze v1.0.23 and no publication action was initiated.

## Senior-engineer self-review

- The density layer is presentation-only and does not own any durable project/revision/history state.
- Existing Stage-C generation, AI dispatch, viewport tools, hierarchy selection, view cube/camera and telemetry owners remain unchanged.
- Important dynamic status/progress/error surfaces are explicitly not hidden by this slice.
- The change is reversible and bounded to known instructional copy already covered by hover guidance.
- No storage path, provider route, cancellation path or model/runtime behavior changed.
- Real Windows layout/readability still needs user observation; CI cannot prove visual density quality.

## Primary next task — always check this first

Consume any new user/reference-machine results from released **v1.0.22**. Any serious Stage-C/persistence/viewport/storage/cancellation regression immediately supersedes all fallback work.

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

The currently ordered bounded MS-027 fallback sequence is now implemented through density/polish. Do **not** invent another UI slice merely to continue activity.

Next run should:

1. re-check Coordinator ROADMAP/HANDOFF for a newly assigned objective;
2. re-check for any newly available user/reference-machine evidence;
3. if both remain unchanged, select only an already-approved, clearly unblocked higher-value engineering task from the Coordinator roadmap that does not displace Stage-C acceptance; otherwise leave the branch stable for Coordinator direction rather than broadening scope autonomously.

## Documentation / release rules

- Keep completion at **57%** until new acceptance evidence justifies a change.
- Do not edit `TECHNICAL_ROADMAP.md`, `COORDINATOR_LOG.md` or `AUTOMATION_MANAGER.md` from Dev unless required for execution safety.
- Release readiness, chunk size, freeze and publication belong exclusively to the Project Coordinator.
- A validated checkpoint is advisory and does not freeze the branch.

## User dependency

No product/design decision is required. The external dependency remains target-machine verification of released v1.0.22. During a long AI job, also observe the new resource panel for plausible GPU/VRAM/RAM/temperature values and whether ~1 Hz monitoring has any noticeable performance impact. Autonomous engineering may continue when the Coordinator assigns further unblocked work.
