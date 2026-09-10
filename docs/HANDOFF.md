# Miniscuplter Handoff

> Immediate execution baton. Inspect actual Git/release/CI first; TECHNICAL_ROADMAP owns strategic direction.

Last updated: 2026-09-10

## Current state

- **Latest published stable:** `v1.0.22` at `c1ba2d01517cc6bca5a6e6cde3b1e85f853dd0d7` (immutable).
- **Current development branch:** `v1.0.23`.
- **Latest fully validated bounded code candidate before documentation commits:** `ed58f35f6ae4e4a951ae1e551c8a34a955d12f12`.
- **MS-028 repair commit:** `75e4990e04e273c00bf3eecc66e0ae93924b5571`.
- **Overall completion:** **57% acceptance-weighted**.
- **Critical path:** released-v1.0.22 Stage-C reference-machine acceptance. Any reproduced correctness/persistence/viewport/data-safety regression preempts UI fallback work.

## What this Dev Cycle completed

The run first honored the Coordinator's P0 gate and repaired **MS-028**, then—only after exact-head validation was green and reference-machine evidence was still unavailable—implemented exactly one next Coordinator-approved MS-027 slice.

### MS-028 — compile regression repaired

`Main.V1023ViewportToolStrip.cs` had a private `InstallV1023ViewportToolStrip()` composition entry point even though `ExtrasInstaller` calls it from another class. Commit `75e4990e04e273c00bf3eecc66e0ae93924b5571` changes only that entry point to `public`, matching the existing public composition contract used by `InstallV1023UiPreferences()`.

The repair passed exact-head Core, C#, Python/runtime, geometry, release-audit, portable package/hash and installer-definition validation before additional feature work began.

### MS-027 slice 3 — synchronized collapsible scene hierarchy

New stable version-neutral presentation file `Scripts/Main.SceneHierarchy.cs` adds a Godot `Tree` in the existing SCENE region:

- visible hierarchy is collapsible through normal Tree parent/child nodes;
- entries are rebuilt from the existing live `_objects` collection and actual parent relationships;
- no durable/project/scene state is stored by the hierarchy;
- tree selection calls the existing `Select(obj)` owner and existing gizmo refresh path;
- viewport/object selection is reflected back into the tree;
- additions, removals, renames and reparenting are observed and rebuilt from live scene state;
- the old `_sceneList` remains alive but hidden so legacy calls to `RebuildSceneList()` continue safely during migration;
- synchronization runs at 5 Hz and performs a full rebuild only when the live object signature changes;
- `ExtrasInstaller` composes `InstallSceneHierarchy()` after the v1.0.23 preferences/tool-strip layers.

Commits:
- `c02b10d6ef9ee7660e9c955f8cc78b1b02fe25cc` — hierarchy implementation;
- `ed58f35f6ae4e4a951ae1e551c8a34a955d12f12` — final composition wiring.

## Validation

Exact code candidate `ed58f35f6ae4e4a951ae1e551c8a34a955d12f12` passed:

- `core-foundation` run `34530230515`: **PASS**;
- `build` run `34530230570` C# editor/launcher/updater/Core job: **PASS**;
- Python compile and dependency resolution: **PASS**;
- core/execution/job regressions: **PASS**;
- real geometry regressions: **PASS**;
- release audit: **PASS**;
- portable package layout/hash: **PASS**;
- installer-definition compilation: **PASS**.

The full Windows/Godot release and publish jobs were intentionally skipped because this was an ordinary development-branch push, not a release request.

No v1.0.23 release request exists. **Do not publish v1.0.23 merely because branch CI is green.**

Documentation-only commits after `ed58f35...` may trigger another exact-head branch workflow; the code candidate above is the fully validated implementation reference for this handoff.

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

Relevant issues: **MS-023, MS-018, MS-009, MS-024, MS-025, MS-013, MS-022, MS-004**.

## If reference-machine evidence is still unavailable

If exact-head branch state remains green and no higher-priority unblocked issue exists, continue exactly one next Coordinator-approved MS-027 slice:

**Next fallback slice: view cube + selected-object orbit pivot.**

Requirements:
- use the existing camera yaw/pitch/focus/distance owners rather than creating another camera state machine;
- cube faces snap Front/Back/Left/Right/Top/Bottom; edges/corners may provide diagonal/isometric snaps if the bounded slice remains coherent;
- cube orientation reflects the current camera;
- normal orbit pivots around the current selected entity when one exists, while preserving the existing empty-scene focus behavior;
- do not change durable project/object authority;
- preserve v1.0.22 viewport/render ownership and the existing viewport tool/input owner;
- add focused validation and stop before the AI console/telemetry work.

Subsequent Coordinator order remains unified AI command/history/dispatcher → MS-026 telemetry → density/polish, always subordinate to critical-path acceptance findings.

## Documentation note

`PROJECT_STATUS.md` has been reconciled to the repaired/hierarchy state. The existing MS-028 ledger entry may still show its pre-fix OPEN wording because the available GitHub contents write is whole-file replacement and this run avoided risking a destructive rewrite of the large canonical issue ledger from a truncated retrieval. Treat the exact repository/CI evidence and this HANDOFF as authoritative for MS-028: the compile blocker is fixed and validated at `75e4990...` / carried through `ed58f35...`. The next safe full-ledger maintenance pass should change MS-028 to RESOLVED while preserving its failed-build history.

## Release rules

- Keep completion at **57%** until new acceptance evidence justifies a change.
- MS-027 remains opportunistic and does not imply Stage-C acceptance.
- Do not modify `TECHNICAL_ROADMAP.md` or `COORDINATOR_LOG.md` from Dev Cycle unless correcting an execution-safety factual contradiction.
- A future v1.0.23 release must be scope-coherent, version-reconciled and pass the autonomous exact-SHA Godot/export/hash/installer-smoke gates before publication.

## User dependency

No product/design decision is required. The external dependency remains reference-machine testing of released v1.0.22. Autonomous engineering can continue under the Coordinator fallback while that evidence is unavailable.


## Release ownership / continuous-development handoff

Release ownership belongs exclusively to the Project Coordinator.

Dev Cycle must not create/update release-control requests, tags or GitHub Releases.

Current v1.0.23 release state: **DEVELOPMENT CONTINUES / NO RELEASE FREEZE**.

Release-worthy checkpoints are informational only. Dev may record an exact checkpoint SHA plus validation evidence in HANDOFF/PROJECT_STATUS and then continue implementing the Coordinator roadmap. A checkpoint does **not** require Dev to stop and does **not** freeze v1.0.23.

Only an actual Coordinator release decision/request creates a freeze.

When Coordinator decides the accumulated current version is coherent and substantial enough to release:
- release boundary is the exact current semantic-version branch HEAD;
- Coordinator creates/uses the next forward semantic-version development branch from that same frozen SHA;
- HANDOFF/STATUS/ROADMAP on the forward branch identify it as the writable development branch;
- Coordinator submits release-control for the frozen prior version;
- Dev continues on the forward branch while publication runs;
- no one mutates the frozen release source branch until the release workflow succeeds or Coordinator explicitly diagnoses and manages a failed-release fix-forward path.

Do not rewind a moving semantic-version branch to an older recorded checkpoint. Larger coherent Coordinator-curated release chunks are preferred over publishing every checkpoint.

