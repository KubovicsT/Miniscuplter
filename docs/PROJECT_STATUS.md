# Miniscuplter Project Status

> Fast-moving project dashboard. Inspect actual Git/release/CI state before trusting this file.

Last reconciled: 2026-09-11

## Current release / development state

- **Latest published stable release:** `v1.0.24`.
- **Stable release target:** `784f408efba8a876b889fd704e051f22229de368`.
- **Current writable development branch:** `v1.0.25`.
- **Latest fully validated v1.0.25 implementation/test checkpoint:** `94554d21519cb06c286a686631d2ad44ca6648b7`.
- **Overall completion:** **57% acceptance-weighted**.

v1.0.24 is published and immutable. All further implementation changes belong on v1.0.25 or a later Coordinator-designated forward branch.

## v1.0.24 publication outcome

v1.0.24 is now the latest published immutable release at `784f408efba8a876b889fd704e051f22229de368`. Its release chunk contains the bounded MS-020 heavyweight-job reliability work and the first bounded MS-019 generation-authority retirement seam. Reference-machine acceptance still outranks CI for user-observed Stage-C/runtime/viewport behavior.

## Current v1.0.25 progress — bounded MS-019 transform authority

Three deliberately narrow transform seams have now been implemented and validated on the forward branch:

- mapped Stage-C 1 mm Move commands derive the durable transform from Core project state rather than an already-mutated Godot scene node;
- mapped Stage-C Rotate Y ±5° commands derive rotation from `ProjectObject.Transform.RotationEuler`, apply only the requested Y-axis delta, save through Stage-C, then project durable state back to Godot;
- mapped Stage-C Scale ±5% commands now derive scale from `ProjectObject.Transform.Scale`, apply only the requested uniform factor, save transactionally, then project the durable transform back to Godot;
- compatibility behavior remains for unmapped/pre-migration objects;
- focused source-wiring regressions prevent mapped Move/Rotate/Scale commands from returning to generic scene-observed persistence.

Move implementation: `ec54845c19ed632640f813d1ee76ce2708638389`.
Rotate implementation/test checkpoint: `e0666bbd7e56d340f112238f78c836b958a64675`.
Scale test gate: `3ad632925e6a6fd512e5dcee44bdf6f1aca2c102`.
Scale implementation/checkpoint: `94554d21519cb06c286a686631d2ad44ca6648b7`.

Ground placement, viewport-drag authority, selection retirement and broader persistence cleanup were deliberately not included in the Scale seam.

## Validation state

Exact-head CI for Scale checkpoint `94554d21519cb06c286a686631d2ad44ca6648b7` is green:

- `core-foundation` run `34607736187`: PASS;
- `build` run `34607736306`: PASS;
- semantic-version branch identity: PASS;
- C# editor/launcher/updater/Core restore/build: PASS;
- backend Python compile/dependency resolution: PASS;
- core logic and execution/job regressions: PASS;
- real geometry regressions: PASS;
- strict release audit: PASS;
- portable package/layout and ZIP SHA-256 sidecar: PASS;
- installer-definition compilation: PASS;
- release/publication jobs remained outside Dev ownership and no publication action was taken.

`94554d21...` is therefore a useful fully validated checkpoint. It is informational only and does not freeze v1.0.25.

## Critical path

Stage-C reference-machine acceptance remains P0 on **released v1.0.24**:

`accepted 2D baseline → local 3D generation → Ready/Conflict candidate → Apply → save → close/reopen → same durable object/revision → Move/Rotate/Scale/sculpt → cleanup → exact STL export`

Also verify viewport resize/presentation, starter-scene removal, storage containment, provider/resource behavior and cancellation/recovery. New serious target-machine evidence immediately preempts fallback work.

## Next execution direction

Consume v1.0.24 target-machine evidence first. The Coordinator-approved bounded Scale authority seam is complete and validated. Do not continue directly into ground placement, viewport-drag, selection or persistence authority without current Coordinator sequencing. Do not broaden MS-020 or resume opportunistic MS-027 UI work without evidence or Coordinator direction.

The validated checkpoint is informational only; it does not stop continuous development.

## User dependency

No product/design decision is required. Reference-machine verification of released v1.0.24 remains the external dependency.
