# Miniscuplter Project Status

> Fast-moving project dashboard. Inspect actual Git/release/CI state before trusting this file.

Last reconciled: 2026-09-11

## Current release / development state

- **Latest published stable release:** `v1.0.24`.
- **Stable release target:** `784f408efba8a876b889fd704e051f22229de368`.
- **Current writable development branch:** `v1.0.25`.
- **Latest fully validated v1.0.25 implementation/test checkpoint:** `c6d129ed78a991b809783cf3457a7524029fd95e`.
- **Bounded v1.0.25 MS-019 Move-authority implementation:** `ec54845c19ed632640f813d1ee76ce2708638389`.
- **Overall completion:** **57% acceptance-weighted**.

v1.0.24 is published and immutable. All further implementation changes belong on v1.0.25 or a later Coordinator-designated forward branch.

## v1.0.24 publication outcome

v1.0.24 is now the latest published immutable release at `784f408efba8a876b889fd704e051f22229de368`. Its release chunk contains the bounded MS-020 heavyweight-job reliability work and the first bounded MS-019 generation-authority retirement seam. Reference-machine acceptance still outranks CI for user-observed Stage-C/runtime/viewport behavior.

## Current v1.0.25 progress — bounded MS-019 Move authority

The current proven authority-retirement seam is intentionally narrow:

- mapped Stage-C 1 mm Move commands derive the durable transform from Core project state rather than an already-mutated Godot scene node;
- the move delta is committed under the Stage-C transaction/persistence path and projected back to presentation afterward;
- compatibility behavior remains for unmapped/pre-migration objects;
- focused source wiring prevents mapped Move commands from regressing to the generic scene-observed persistence hook.

Implementation commit: `ec54845c19ed632640f813d1ee76ce2708638389`. Rotate/Scale/ground/viewport-drag authority was not broadened.

## v1.0.25 branch identity repair and validation

The v1.0.25 branch initially inherited `1.0.24` identity across release/version surfaces. `c6d129ed78a991b809783cf3457a7524029fd95e` reconciles all audited surfaces to `1.0.25` without changing published v1.0.24: launcher, updater, Godot assembly, installer, Windows export metadata, backend API, editor display and release audit.

Exact-head validation is green:

- `core-foundation` run `34597074576`: PASS;
- `build` run `34597074521`: PASS;
- semantic-version branch identity: PASS;
- C# editor/launcher/updater/Core restore/build: PASS;
- backend Python compile/dependency resolution: PASS;
- core logic and execution/job regressions: PASS;
- real geometry regressions: PASS;
- strict release audit: PASS;
- portable package/layout and ZIP SHA-256 sidecar: PASS;
- installer-definition compilation: PASS;
- release/publication jobs correctly skipped because this is a development-branch push.

`c6d129ed78a991b809783cf3457a7524029fd95e` is therefore a useful fully validated checkpoint, not a release freeze.

## Critical path

Stage-C reference-machine acceptance remains P0 on **released v1.0.24**:

`accepted 2D baseline → local 3D generation → Ready/Conflict candidate → Apply → save → close/reopen → same durable object/revision → Move/Rotate/Scale/sculpt → cleanup → exact STL export`

Also verify viewport resize/presentation, starter-scene removal, storage containment, provider/resource behavior and cancellation/recovery. New serious target-machine evidence immediately preempts fallback work.

## Next execution direction

Consume v1.0.24 target-machine evidence first. The current Coordinator sequencing limits MS-019 to one proven seam at a time, so do not select another Rotate/Scale/selection/persistence retirement seam speculatively. Do not broaden MS-020 or resume opportunistic MS-027 UI work without evidence or Coordinator direction.

The validated checkpoint is informational only; it does not itself stop continuous development. Continue immediately when current Coordinator sequencing identifies the next bounded task.

## User dependency

No product/design decision is required. Reference-machine verification of released v1.0.24 remains the external dependency.
