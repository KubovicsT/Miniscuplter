# Miniscuplter Handoff

> Immediate and downstream execution baton. TECHNICAL_ROADMAP owns whole-system strategy; this file tells Dev what it may execute continuously.

Last updated: 2026-09-11

## Repository / release state

- **Latest published stable:** `v1.0.25` at `a7fc4bcf5f771c18060e5aee7c98026131731c2a`.
- **Current writable development branch:** `v1.0.26`.
- **Current branch HEAD reviewed by Coordinator:** `9e1b2b75275e1c6bd4be32730475630645d6ea10`.
- **Latest CI-green implementation checkpoint:** `1e6ed3541de6de4bec19838e71595e4b76dbedfa`.
- **Release freeze:** none.
- **v1.0.26 release-control request:** none.
- **Release decision:** KEEP ACCUMULATING; not ready to freeze.

## Important completed work in ancestry

- Core-authoritative viewport Move/Rotate/Scale drag persistence is implemented and validated.
- MS-029 updater health-probe logic was fixed in `9f36f44...` and regression-covered in `e6ae3d35...`.
- MS-030 interpreter/runtime ownership was substantially improved in `4efd840e...`, `6dcb754b...`, `1e6ed354...`: editor and Repair now target the same backend-local venv; Repair adds a health smoke; startup diagnostics expose interpreter/path/exit/stderr.

**Do not treat MS-030 as fixed yet.** Coordinator end-to-end review found that both editor and Repair still invoke `python app.py`, while `app.py` only defines the FastAPI `app` and has no executable server entry point. Green CI did not test the actual server lifecycle.

## CURRENT OBJECTIVE — A: canonical backend startup / repaired-runtime contract

**Goal / outcome**  
Make Repair and the editor launch the same real local FastAPI server through one deterministic startup contract.

**Constraints**
- backend-local repaired venv remains authoritative;
- no PATH/system-Python fallback;
- start an actual Uvicorn/FastAPI server, not merely execute an import-only module;
- loopback only;
- Repair smoke must not succeed against an unrelated pre-existing server on the production port;
- preserve process-tree containment, cancellation recovery, runtime/data-root environment and useful diagnostics;
- prefer one canonical start contract rather than separately evolving editor and Repair command lines.

**Dependencies**  
None; this is P0.

**Acceptance / stop condition**
- actual server reaches health under the repaired venv;
- Repair proves the server instance/runtime it launched, using an isolated smoke port or equivalent process-bound identity, validates expected health version/identity and cleans the probe process;
- editor production launch uses the same server contract;
- focused startup/runtime regressions and full exact-head validation/package gates pass;
- update MS-030 to FIXED - NEEDS USER VERIFICATION.

**Preemption**  
Only a new higher-severity data-loss/runtime-corruption/release-safety issue or contradictory user evidence.

**Auto-proceed:** YES → Objective B.

## NEXT OBJECTIVE — B: MS-009 viewport resize/render authority

**Goal / outcome**  
Resize must not change grid/background/model/gizmo presentation.

**Constraints**
- instrument before changing architecture;
- Godot remains presentation/input owner;
- keep Stretch as normal render-target size owner unless evidence disproves it;
- no timer/watchdog, blind delayed full repair, or second manual SubViewport size owner;
- identify and retire the conflicting historical resize/presentation path instead of stacking another repair layer;
- preserve non-occluding grid and model visibility.

**Dependencies**  
Objective A accepted; no new Critical blocker.

**Acceptance / stop condition**  
Root state transition identified, focused regression added, strongest Windows/Godot rendering validation plus exact-head CI/package gates green; leave MS-009 FIXED - NEEDS USER VERIFICATION until user retest.

**Preemption**  
Runtime/persistence/data-loss/blank-viewport regression.

**Auto-proceed:** YES → Objective C.

## NEXT OBJECTIVE — C: user-directed MS-027 compact workspace tranche

**Goal / outcome**  
Deliver the four visible UI corrections requested by the user as one coherent workspace outcome.

**Required scope**
1. multi-line scrollable AI command console/history with Run;
2. real interactive orientation cube;
3. compact viewport tool controls without permanent instruction text;
4. remove workflow-panel explanation prose and replace detailed help with small circular `i` hover help/tooltips.

**Constraints**
- reuse authoritative command/action, viewport-tool, camera and selection owners;
- converge toward one current workspace composition owner; do not add a parallel UI state machine;
- replacement controls must be wired before obsolete visible controls are retired;
- viewport remains dominant;
- no scene-tree/resource-graph/new-modeling/Rig-Pose expansion.

**Dependencies**  
Objective B accepted.

**Acceptance / stop condition**  
UI/layout regressions cover routing/ownership and persistence; supported resize/font scales remain usable; Stage-C/save/load/transform/cancellation/export behavior is unchanged; exact-head validation/package gates green.

**Preemption**  
Any new Critical correctness/runtime/persistence/viewport/release issue.

**Auto-proceed:** YES → Objective D.

## NEXT OBJECTIVE — D: integrated v1.0.26 release-candidate hardening

**Goal / outcome**  
Produce one coherent exact-HEAD candidate containing runtime, updater, viewport and user-directed UI acceptance work.

**Constraints**
- no unrelated feature expansion;
- reconcile issue/status docs honestly;
- MS-029: preserve current fix and document that immutable v1.0.25's updater can still cause one manual reopen on the v1.0.25 → v1.0.26 transition;
- run strongest C#/Core/Python/runtime/geometry/execution/release-audit/Windows export/package/hash/installer validation;
- verify storage/data/runtime-cache/venv preservation and version identity;
- Dev must not create release-control, tag or GitHub Release.

**Dependencies**  
A–C accepted; no new serious blocker.

**Acceptance / stop condition**  
Exact current HEAD coherent, all available automated/package gates green, release-worthy checkpoint recorded, docs list remaining target-machine verification accurately, and HANDOFF marks `COORDINATOR REVIEW REQUESTED`.

**Preemption**  
Any failing release gate or new serious user evidence.

**Auto-proceed:** NO. Release boundary/freeze is Coordinator-owned.

## Queue depth / stop boundary

This queue intentionally contains four substantial objectives expected to span multiple Dev runs. It does not authorize MS-019 ground placement or unrelated migration after Objective D because once the integrated candidate is ready, whether to freeze/release the current HEAD is a Coordinator release-scope decision. That is a valid `COORDINATOR REVIEW REQUESTED` boundary, not artificial hourly gating.

## User verification dependency

No user action is needed now.

Do not keep retrying v1.0.25 Repair/3D generation, viewport resize reproduction or the already-recorded UI gaps.

When a fixed release is available, retest update/reopen, Repair health, Generate 3D reaching provider inference, resize invariance and the compact UI tranche; then continue the full Stage-C save/reopen/edit/cleanup/export path.
