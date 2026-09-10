# Miniscuplter Project Status

> Fast-moving project dashboard. Inspect actual Git/release/CI state before trusting this file.

Last reconciled: 2026-09-10

## Current release / development state

- **Latest published stable release:** `v1.0.22`
- **Stable release target commit:** `c1ba2d01517cc6bca5a6e6cde3b1e85f853dd0d7`
- **Current development branch:** `v1.0.23`
- **Latest fully validated bounded code candidate before documentation commits:** `ae1b081f195332028a2ad929381236030fcfefde`
- **Overall completion:** **57% acceptance-weighted**

v1.0.22 is published and immutable. v1.0.23 remains the writable forward development branch. There is no v1.0.23 release-control request or Coordinator release freeze.

## Current phase

The Coordinator-defined critical path remains **Stage-C target-machine acceptance**. Released v1.0.22 contains bounded fixes for the user-observed generation/persistence, viewport/client-fill and starter-scene issues. MS-023, MS-009, MS-024, MS-025 and other target-only acceptance items remain **FIXED - NEEDS USER VERIFICATION** where applicable until the released build is retested on the reference Windows / GTX 1080 machine.

The next critical evidence remains:

`accepted 2D baseline → local 3D generation → Ready/Conflict candidate → explicit Apply → save → close/reopen → same durable object/revision → transform/sculpt → cleanup → exact STL export`

plus resize/presentation, storage containment, provider/resource and cancellation/recovery checks.

## v1.0.23 bounded MS-027 progress

While Stage-C acceptance is externally blocked, Dev has advanced only the Coordinator-approved bounded UI slices:

1. **Workspace preferences / UI scale / tooltips** — controlled-root splitter/layout persistence, 75–135% UI scale and reusable tooltips.
2. **Direct viewport tools** — compact Select/Move/Rotate/Scale/Sculpt controls delegating to the existing V1018 tool/input owner. The development-only MS-028 composition compile regression was fixed and validated.
3. **Synchronized scene hierarchy** — version-neutral collapsible `Tree` reflecting existing scene objects/parentage and delegating selection to the existing selection owner.
4. **View cube + selected-object orbit pivot** — new stable version-neutral `Main.ViewCube.cs` adds six orthographic face snaps and a camera-linked orientation display. RMB orbit start retargets the existing `_focus/_yaw/_pitch/_distance` camera state to the selected object while preserving the camera position, so the view does not jump and no second camera/input state machine is introduced. Empty-scene focus behavior is unchanged.

View-cube implementation commits:
- `8264391a70c49652f036cc749f447a9f0ce715ca` — initial view-cube presentation;
- `8e926f11ca7fc60bff125947c460d439c5efcb6c` — selected-orbit-pivot observer using existing camera authority;
- `ce38692e29d60b47a8716422d2f6553eb5856eb8` — composition after earlier MS-027 presentation slices;
- `58f3c2e5ea19eeb2379a874a7199d717c104c115` — first wiring guard;
- `ae1b081f195332028a2ad929381236030fcfefde` — corrected wiring guard after a false-negative static assertion.

The failed `58f3c2e...` Core run is retained as development history: the guard searched for literal `ViewCubeFront`/`ViewCubeBack` strings while button names are generated dynamically. The implementation was not the cause. `ae1b081...` corrected the test to assert the actual six `AddViewCubeFace(...)` registrations.

## Validation

Exact code/test candidate `ae1b081f195332028a2ad929381236030fcfefde` passed:

- Stage-B/Core workflow — `core-foundation` run `34535418564`: **PASS**;
- C# editor/launcher/updater/Core build — `build` run `34535418525`, dotnet job: **PASS**;
- Python compile and dependency resolution: **PASS**;
- core/execution/job regressions: **PASS**;
- real geometry regressions: **PASS**;
- release audit: **PASS**;
- portable package build/layout and ZIP SHA-256 verification: **PASS**;
- Inno Setup installation and installer-definition compilation: **PASS**.

The full Windows/Godot release pipeline is intentionally not run for an ordinary development-branch push. This is a useful validated checkpoint, not a release decision or freeze. Release ownership remains with the Coordinator and development continues until an explicit Coordinator release boundary is chosen.

## Current priorities

1. Consume any new reference-machine evidence from released v1.0.22 immediately; any serious correctness, persistence, viewport, data-safety, storage, cancellation or Stage-C regression preempts UI work.
2. Complete Stage-C acceptance when target-machine testing is available, including persistence, transform/sculpt, cleanup/export, storage, provider/resource and cancellation evidence.
3. If acceptance remains externally blocked and no higher-priority unblocked issue appears, the next Coordinator-approved fallback is the **unified AI command/history/dispatcher** slice. It must converge existing actions on one authoritative dispatcher rather than creating duplicate AI handlers.
4. MS-026 telemetry and density/polish remain after that, subordinate to Stage-C findings.

## Release ownership / continuous development

Release ownership belongs exclusively to the Project Coordinator. Dev may record validated checkpoints and continue implementation; a checkpoint does not freeze the branch. Only an actual Coordinator release request freezes the source semantic-version branch. Dev does not create/update release requests, tags or GitHub Releases.

## User dependency

No product/design decision is required. The principal external dependency remains reference-machine verification of released v1.0.22, especially Generate 3D → candidate → Apply → save/close/reopen → same 3D object/revision, plus viewport resizing/presentation, starter removal, transform/sculpt, cleanup/export and storage behavior.
