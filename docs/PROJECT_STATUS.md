# Miniscuplter Project Status

> Fast-moving project dashboard. Inspect actual Git/release/CI state before trusting this file.

Last reconciled: 2026-09-10

## Current release / development state

- **Latest published stable release:** `v1.0.22`
- **Stable release target commit:** `c1ba2d01517cc6bca5a6e6cde3b1e85f853dd0d7`
- **Current development branch:** `v1.0.23`
- **Current fully validated bounded code candidate before documentation commits:** `ed58f35f6ae4e4a951ae1e551c8a34a955d12f12`
- **MS-028 compile-fix commit:** `75e4990e04e273c00bf3eecc66e0ae93924b5571`
- **Overall completion:** **57% acceptance-weighted**

v1.0.22 is published and immutable. v1.0.23 is the forward application branch. No application changes belong on v1.0.22.

## Current phase

The Coordinator-defined critical path remains **Stage-C target-machine acceptance**. Released v1.0.22 contains the bounded acceptance fixes for the concrete v1.0.21 reference-machine findings:

1. **MS-023 — Stage-C generation ownership/persistence:** final composition removes historical Generate-3D owners and binds exactly one revision-aware `V1020Generate3DAsync` owner.
2. **MS-024 — resize/client-fill seams:** final composition reasserts FullRect/ExpandFill layout ownership without restoring manual `SubViewport.Size` writes.
3. **MS-025 — starter-scene/presentation residuals:** the historical starter sphere is removed from launch/New/recovery paths and the opaque legacy grid floor is hidden while useful grid/axes remain.

Those fixes remain **FIXED - NEEDS USER VERIFICATION** until the released v1.0.22 build is exercised on the reference machine. The next critical evidence is generate → Ready/Conflict candidate → Apply → save/close/reopen → same durable object/revision, plus viewport resizing/presentation, starter removal, cleanup/export, storage containment and one qualified lightweight 3D route.

## v1.0.23 bounded MS-027 progress

Acceptance remained externally blocked, so only Coordinator-approved bounded UI modernization slices were advanced.

### Slice 1 — workspace preferences / UI scale / tooltips

- `Main.V1023UiPreferences.cs` stores editor-only preferences under the authoritative Miniscuplter data root at `Settings/ui_preferences.json`.
- Body and viewport/right-panel splitter positions persist across restarts with minimum-width clamps.
- Settings includes an Interface page with bounded 75–135% UI/font scaling and workspace-layout reset.
- Default font scale is reduced to 90%.
- Reusable hover-tooltip infrastructure covers core toolbar actions.
- The layer does not reference `ProjectStore` or own Stage-C generation actions.

### Slice 2 — direct viewport tools

- `Main.V1023ViewportToolStrip.cs` presents compact direct Select / Move / Rotate / Scale / Sculpt controls and hides the old dropdown.
- The buttons delegate to the existing `V1018ToolSelected` / `_v1018Tool` authority and do not create another viewport input/tool state machine.
- **MS-028** was a development-only compile regression caused by the installer method being private while `ExtrasInstaller` called it. Commit `75e4990e04e273c00bf3eecc66e0ae93924b5571` makes the composition entry point public; exact-head C#, Core, Python/runtime, geometry, release-audit, packaging/hash and installer-definition validation is green.

### Slice 3 — synchronized collapsible scene hierarchy

- New stable version-neutral presentation file `Main.SceneHierarchy.cs` replaces the visible legacy flat scene-button list with a Godot `Tree` while leaving the old list alive but hidden for compatibility.
- The tree reflects the existing `_objects` collection and actual parent/child relationships; it does not persist or own project/scene state.
- Tree selection calls the existing `Select(obj)` owner, rebuilds the legacy compatibility list and refreshes the existing gizmo.
- Viewport/object selection is mirrored back into the tree by a low-frequency presentation-only sync timer.
- Object additions/removals/renames/reparenting trigger a hierarchy rebuild from live scene state rather than a second model.
- `ExtrasInstaller` composes `InstallSceneHierarchy()` after the v1.0.23 preference and tool-strip presentation layers.

Implementation commits for this slice: `c02b10d6ef9ee7660e9c955f8cc78b1b02fe25cc`, `ed58f35f6ae4e4a951ae1e551c8a34a955d12f12`.

## Validation

Exact code candidate `ed58f35f6ae4e4a951ae1e551c8a34a955d12f12` passed:

- Stage-B/Core workflow — `core-foundation` run `34530230515`: **PASS**;
- C# editor/launcher/updater/Core builds — `build` run `34530230570`, dotnet job: **PASS**;
- Python compile/dependency resolution: **PASS**;
- core/execution/job regressions: **PASS**;
- real geometry regressions: **PASS**;
- release audit: **PASS**;
- portable package layout/hash: **PASS**;
- installer-definition compilation: **PASS**.

The earlier MS-028 repair commit `75e4990e04e273c00bf3eecc66e0ae93924b5571` independently passed the same branch-validation families before hierarchy work began. Full Windows/Godot release and publish jobs are intentionally skipped on ordinary branch pushes and were not used to claim a release.

No release request has been submitted for v1.0.23. v1.0.23 is **development-only**; green branch CI does not itself justify publication.

## Current priorities

1. **Reference-machine retest of released v1.0.22** for MS-023, MS-024, MS-025 and the complete MS-018 Stage-C path. Any reproduced correctness/persistence/viewport/data regression immediately preempts UI modernization.
2. In the same acceptance session collect MS-013 storage and MS-022 provider/resource evidence, plus MS-004 cancellation/recovery where practical.
3. If acceptance evidence remains unavailable and no higher-priority unblocked issue appears, the next Coordinator-approved MS-027 slice is the **view cube + selected-object orbit pivot**. It must remain bounded and reuse the existing camera/selection owners.
4. Broader MS-019/MS-020 architecture remains behind Stage-C acceptance unless concrete evidence changes dependency order.

## User input currently required

No product/design decision is required. The important external dependency is reference-machine verification of released v1.0.22; until that arrives, bounded MS-027 work may continue under the Coordinator fallback rule.


## Release ownership / continuous development

Release ownership belongs exclusively to the Project Coordinator.

Dev Cycle prepares and validates release-worthy checkpoints but does not publish. Current v1.0.23 state is **development-only / NO RELEASE FREEZE**. No v1.0.23 release request exists.

A recorded checkpoint SHA is evidence for future Coordinator review, not a stop condition. Dev continues advancing the roadmap after recording a checkpoint.

Coordinator decides when the accumulated current-version scope is coherent and substantial enough to release. When that decision is made, the exact current version HEAD becomes the release boundary, the next forward semantic-version development branch is created/used from that same SHA, and only then is release-control initiated for the frozen prior branch.

This allows Dev to continue on the next version while the Coordinator publishes the larger frozen release chunk.
