# Miniscuplter Handoff

> Immediate execution baton for the next Dev Cycle. Inspect actual Git/release/CI first. `docs/TECHNICAL_ROADMAP.md` owns medium/long-horizon technical direction; this file owns the next implementation step.

Last updated: 2026-09-10

## Current state

- **Repository:** `KubovicsT/Miniscuplter`
- **Latest published stable:** `v1.0.22`
- **Stable release target:** `c1ba2d01517cc6bca5a6e6cde3b1e85f853dd0d7`
- **Current development branch:** `v1.0.23`
- **Latest fully validated bounded code/test candidate before documentation commits:** `74ec73a14645071bb2742fb68f778bedd656aabd`
- **Overall completion:** **57% acceptance-weighted**
- **Coordinator critical path:** Stage-C reference-machine acceptance; when that path is externally blocked, advance only one bounded MS-027 slice at a time in the Coordinator-approved order.

v1.0.22 is published and immutable. All new application work belongs on v1.0.23 or later.

## What this Dev Cycle completed

The immediate acceptance path is waiting on released-v1.0.22 reference-machine evidence and no newer higher-severity unblocked regression was present. Following the explicit Coordinator/HANDOFF fallback, this cycle implemented **only the first bounded MS-027 slice**.

### MS-027 slice 1 — editor workspace preferences

`Scripts/Main.V1023UiPreferences.cs` now provides editor-only presentation/preferences state without changing Core project state, provider state or AI action ownership:

- persists the outer/body splitter and viewport/right-panel splitter positions;
- stores preferences under the authoritative Miniscuplter data root at `Settings/ui_preferences.json`;
- uses a temporary file plus replacement move for preference writes;
- clamps restored splitter positions so panels remain usable;
- defaults the main interface font to 90% of the previous baseline;
- exposes **UI / font scale** from 75% to 135% in a new Settings → Interface page;
- exposes **Reset Workspace Layout** without touching model/project state;
- adds reusable hover tooltips for core toolbar actions so future density work can remove persistent explanatory text safely.

`Scripts/ExtrasInstaller.cs` installs `InstallV1023UiPreferences()` **after** `InstallV1022Acceptance()`. The layer observes the fully composed workspace and deliberately does not own `V1020Generate3DAsync` or `ProjectStore` state.

`tools/core_logic_tests.py` adds static guards for final installer ordering, controlled-root preference storage, both splitter fields, bounded font scale, Interface settings/tooltips, replacement-safe preference writes and absence of project/Stage-C-generation ownership from the UI preference layer.

Commits:
- `f169f6e3428a64804e8778c05353b9d07a87dfe3` — implementation;
- `5faa1e528e6a81fb8db15942c68b3da71391743d` — final composition hook;
- `74ec73a14645071bb2742fb68f778bedd656aabd` — regression guard.

## Validation

Exact code/test commit `74ec73a14645071bb2742fb68f778bedd656aabd` passed:

- Stage-B/Core build + regression suite — `core-foundation` run `34520858442`: **PASS**;
- C# editor/launcher/updater/Core builds — `build` run `34520858529`: **PASS**;
- Python compile/dependency resolution: **PASS**;
- core/execution/job regressions including the MS-027 wiring guard: **PASS**;
- geometry regressions: **PASS**;
- release audit: **PASS**;
- portable package layout/hash: **PASS**;
- installer-definition compilation: **PASS**.

The earlier implementation head `5faa1e528e6a81fb8db15942c68b3da71391743d` also passed the same branch-validation families in `34520603280` / `34520603310` before the explicit MS-027 guard was added.

No v1.0.23 release request has been submitted. **Do not publish v1.0.23 merely because CI is green.** The current change is a secondary bounded UI increment while the critical acceptance path waits on reference-machine evidence.

## Primary next task — always check this first

Inspect for new user/reference-machine results from **released v1.0.22**. Any reproduced correctness, persistence, viewport, data-safety, cancellation or Stage-C regression preempts MS-027 immediately.

Target acceptance sequence:

1. accepted 2D baseline;
2. qualified/Hunyuan 3D generation;
3. visible Ready/Conflict Stage-C candidate;
4. explicit Apply;
5. save → close → reopen → same durable object and active mesh revision;
6. Move/Rotate/Scale and one supported sculpt/edit path;
7. cleanup and exact STL export;
8. verify right-panel drag + whole-window resize remain visually stable;
9. verify no starter sphere/opaque floor regression;
10. inspect storage containment and, where practical, cancellation/recovery and resource/provider evidence.

Relevant issues: **MS-023, MS-009, MS-024, MS-025, MS-018, MS-013, MS-022, MS-004**.

## If reference-machine evidence is still unavailable

If no higher-priority unblocked issue exists, continue **exactly one** next Coordinator-approved MS-027 slice:

**Next fallback slice: direct icon-based viewport tool strip.**

Requirements:
- expose the existing Move / Rotate / Scale / Sculpt / Select tool owner as direct compact controls rather than creating another tool-state machine;
- active tool must be visibly clear;
- reuse the current `V1018ViewportTool` state/input implementation rather than duplicating viewport action ownership;
- preserve v1.0.22 viewport/render ownership and Stage-C transform/sculpt authority;
- use tooltips for descriptions instead of another permanent instructional panel;
- add focused regression guards;
- stop after this bounded slice rather than continuing into scene tree/view cube/AI console in the same cycle.

After that, Coordinator order remains: synchronized scene hierarchy → view cube/selected-object orbit → unified AI console/history → MS-026 telemetry → density/polish, always subordinate to critical-path acceptance findings.

## Documentation / release rules

- Keep completion at **57%** until new acceptance evidence justifies a change.
- MS-027 remains an in-progress/opportunistic workstream, not evidence that Stage-C is accepted.
- Do not modify `TECHNICAL_ROADMAP.md` or `COORDINATOR_LOG.md` unless correcting a safety-critical factual contradiction; they are Coordinator-owned.
- Do not update `DECISIONS.md` for ordinary UI implementation choices.
- A future v1.0.23 release must be scope-coherent, version-reconciled and pass the autonomous exact-SHA Godot/export/hash/installer-smoke gates before publication.

## User input

No product/design decision is required. The important external dependency is reference-machine testing of the already-published v1.0.22 acceptance fixes.
