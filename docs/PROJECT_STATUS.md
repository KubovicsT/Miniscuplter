# Miniscuplter Project Status

> Fast-moving project dashboard. Inspect actual Git/release/CI state before trusting this file.

Last reconciled: 2026-09-11

## Current release / development state

- **Latest published stable release:** `v1.0.24`.
- **Stable release target:** `784f408efba8a876b889fd704e051f22229de368`.
- **Current writable development branch:** `v1.0.25`.
- **Latest fully validated v1.0.25 implementation/test checkpoint:** `c6d129ed78a991b809783cf3457a7524029fd95e`.
- **Latest bounded v1.0.25 MS-019 implementation:** `e0666bbd7e56d340f112238f78c836b958a64675` (Rotate-authority seam; exact-head CI still running when this status was written).
- **Overall completion:** **57% acceptance-weighted**.

v1.0.24 is published and immutable. All further implementation changes belong on v1.0.25 or a later Coordinator-designated forward branch.

## v1.0.24 publication outcome

v1.0.24 is now the latest published immutable release at `784f408efba8a876b889fd704e051f22229de368`. Its release chunk contains the bounded MS-020 heavyweight-job reliability work and the first bounded MS-019 generation-authority retirement seam. Reference-machine acceptance still outranks CI for user-observed Stage-C/runtime/viewport behavior.

## Current v1.0.25 progress — bounded MS-019 transform authority

Two deliberately narrow transform seams have now been implemented on the forward branch:

- mapped Stage-C 1 mm Move commands derive the durable transform from Core project state rather than an already-mutated Godot scene node;
- mapped Stage-C Rotate Y ±5° commands now likewise derive rotation from `ProjectObject.Transform.RotationEuler`, apply only the requested Y-axis delta, save through Stage-C, then project durable state back to Godot;
- compatibility behavior remains for unmapped/pre-migration objects;
- focused source-wiring regressions prevent mapped Move/Rotate commands from returning to generic scene-observed persistence.

Move implementation: `ec54845c19ed632640f813d1ee76ce2708638389`.
Rotate implementation: `e0666bbd7e56d340f112238f78c836b958a64675`.

Scale, ground placement and viewport-drag authority were deliberately not broadened in the Rotate seam.

## Validation state

The previous fully validated v1.0.25 checkpoint remains `c6d129ed78a991b809783cf3457a7524029fd95e`:

- `core-foundation` run `34597074576`: PASS;
- `build` run `34597074521`: PASS;
- semantic-version branch identity: PASS;
- C# editor/launcher/updater/Core restore/build: PASS;
- backend Python compile/dependency resolution: PASS;
- core logic and execution/job regressions: PASS;
- real geometry regressions: PASS;
- strict release audit: PASS;
- portable package/layout and ZIP SHA-256 sidecar: PASS;
- installer-definition compilation: PASS.

For Rotate checkpoint `e0666bbd7e56d340f112238f78c836b958a64675`, exact-head CI started normally:

- `core-foundation` run `34602202639`: IN PROGRESS when recorded;
- `build` run `34602202701`: IN PROGRESS when recorded;
- semantic-version identity, backend compile/dependency resolution, core logic and execution-foundation steps had already passed in the build run when last inspected.

Until those exact-head workflows finish successfully, `e0666bbd...` is an implementation checkpoint rather than a fully validated/release-worthy checkpoint.

## Critical path

Stage-C reference-machine acceptance remains P0 on **released v1.0.24**:

`accepted 2D baseline → local 3D generation → Ready/Conflict candidate → Apply → save → close/reopen → same durable object/revision → Move/Rotate/Scale/sculpt → cleanup → exact STL export`

Also verify viewport resize/presentation, starter-scene removal, storage containment, provider/resource behavior and cancellation/recovery. New serious target-machine evidence immediately preempts fallback work.

## Next execution direction

Consume v1.0.24 target-machine evidence first. If none exists, first reconcile the exact-head CI result for the bounded Rotate seam and fix any attributable defect. Do not broaden this run into Scale/ground/viewport-drag retirement. Once the Rotate seam is validated and recorded, another authority-retirement seam should be selected only under current Coordinator sequencing. Do not broaden MS-020 or resume opportunistic MS-027 UI work without evidence or Coordinator direction.

The existing validated checkpoint is informational only; it does not stop continuous development.

## User dependency

No product/design decision is required. Reference-machine verification of released v1.0.24 remains the external dependency.
