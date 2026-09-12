# Miniscuplter Technical Roadmap

> Authoritative end-to-end technical sequencing owned by the Project Coordinator. PROJECT_CHARTER/DECISIONS own product truth; HANDOFF owns the immediate/downstream Dev baton.

Last coordinator review: 2026-09-12
Latest published stable: `v1.0.26`
Published release target: `v1.0.26` at `a41e0419ba40fd1118775e8f516b1a31145f18d8`
Current writable development branch: `v1.0.27`
Acceptance-weighted completion: **approximately 64%**

## 1. Whole-system direction

**PRESERVE THE SELECTIVE-REFACTOR DIRECTION.**

The architecture continues to converge toward the intended authority split:

- Core owns durable project/object/revision/history state.
- Godot owns presentation, viewport/input and live interaction.
- Python owns inference and geometry execution.
- Launcher/updater owns runtime setup, application update and delivery safety.

The highest technical risk remains legacy authority overlap at subsystem seams, not a need for another broad rewrite. Recent v1.0.26 work reduced that risk in backend startup, viewport resize ownership, updater lifecycle and workspace composition.

## 2. v1.0.26 release assessment

v1.0.26 accumulated a substantial coherent acceptance/reliability tranche:

- Core-authoritative viewport drag persistence;
- deterministic packaged backend server startup and Repair health validation;
- updater health-probe lifecycle correction;
- retirement of competing viewport resize/world-repair owners;
- the four explicit user-directed compact-workspace corrections.

Exact current v1.0.26 HEAD `a41e0419ba40fd1118775e8f516b1a31145f18d8` passed exact-head Core and build CI. Release identity remained 1.0.26 across launcher/updater/editor, installer, Windows metadata, backend, display and release audit. No known release blocker remained.

**Release decision: FREEZE AND RELEASE v1.0.26.**

The autonomous release-control workflow completed successfully for that exact SHA: exact-SHA validation, Godot Windows export, versioned output verification, installer smoke-install, immutable tag creation and GitHub Release publication all passed. The existing earlier v1.0.27 branch was previously premature/orphaned; as part of this actual release transition it was fast-forwarded to the true frozen candidate and mechanically bootstrapped to 1.0.27 release identity. v1.0.27 is now the authoritative writable branch.

## 3. Acceptance state carried into the release

- **MS-030:** FIXED - NEEDS USER VERIFICATION. Canonical `serve.py` Uvicorn startup and instance-bound Repair/editor health checks are implemented and automated.
- **MS-009:** FIXED - NEEDS USER VERIFICATION. Historical resize/world-repair owners are retired; renderer-sensitive acceptance still belongs to the reference machine.
- **MS-029:** FIXED - NEEDS USER VERIFICATION. Future fixed-updater transitions preserve a healthy launcher. The immutable v1.0.25 updater can still cause one manual reopen while installing v1.0.26.
- **MS-027 current tranche:** implemented and regression-covered; broader workspace modernization remains open.
- **MS-018:** remains the overarching target-hardware acceptance milestone.

## 4. Critical path after publication

The next highest-value evidence is not more provider/UI breadth. It is reference-machine validation of the newly released reliability fixes and the complete Stage-C thin slice.

Priority order:
1. v1.0.26 update/reopen behavior.
2. Repair AI Runtime health.
3. Generate 3D reaching provider resolution/inference.
4. viewport resize invariance.
5. current compact workspace UX.
6. candidate Apply → save/reopen → edit/sculpt → cleanup → exact STL export.
7. storage containment, cancellation/recovery and resource observations during the flow.

Any reproduced Critical defect from this sequence preempts forward migration work.

## 5. Rolling v1.0.27 execution plan

### Objective A — forward-version bootstrap validation

**Outcome:** make v1.0.27 internally coherent and safely writable while v1.0.26 publishes.

**Constraints:** no writes to frozen v1.0.26; version identity must be 1.0.27 on all audited surfaces; no feature work until exact-head Core/build/release-audit/package gates are green.

**Current bootstrap defect:** `tools/backend_lifecycle_tests.py` still expects backend version 1.0.26, so v1.0.27 `python-syntax` fails after the server answers 1.0.27. This is a mechanical forward-version identity defect, not a product/runtime regression. Fix that expectation first.

**Acceptance:** semantic-version identity, including lifecycle test expectation, and exact-head validation green.

**Preemption:** v1.0.26 release failure requiring source repair.

**Auto-proceed:** yes.

### Objective B — bounded MS-019 mapped-object ground-placement authority

**Outcome:** remove one remaining scene-observed transform persistence seam.

**Constraints:** Core durable transform/history authority; Godot presentation/input authority; stable object/revision identity; transactional stale-state rejection; restore presentation from Core on failure; no broad sculpt/selection/provider/job-broker expansion.

**Acceptance:** ground placement persists through the durable Core transform path with focused regressions and exact-head validation.

**Preemption:** release failure or new reference-machine evidence.

**Auto-proceed:** yes.

### Objective C — bounded durable selection/picking authority seam

**Outcome:** migrate one concrete production selection path from scene/name authority to stable object/revision identity.

**Constraints:** Godot owns hit testing/presentation; Core owns durable identity-dependent state; topology/revision changes invalidate or explicitly transfer selection; no scene-tree redesign or broad Stage-D rewrite.

**Acceptance:** one production selection path uses stable identity with stale-revision coverage and no transform/sculpt/Stage-C regression.

**Preemption:** user acceptance evidence, release regression, persistence/data-safety issue.

**Auto-proceed:** no; Coordinator review after this seam.

## 6. Explicit non-priorities

Do not add new provider families, broad Rig/Pose work, kitbash breadth, scene-tree/resource-graph redesign, generalized MS-020 infrastructure or unrelated UI features before v1.0.26 target-machine acceptance is consumed.

## 7. Branch / release anomaly record

The earlier v1.0.27 branch at `30f42c5...` was a **PREMATURE / ORPHAN FORWARD BRANCH** because no v1.0.26 freeze/release request existed and its identity still matched 1.0.26. It was not treated as writable/current. During the legitimate v1.0.26 transition it was reconciled by fast-forwarding to the exact frozen v1.0.26 candidate, then bootstrapping 1.0.27 identity.

## 8. User dependency

No product decision is required. Once v1.0.26 is published, reference-machine acceptance should begin immediately using the ordered sequence above.


## 9. Publication result / immediate post-release finding

v1.0.26 was published successfully on 2026-09-12 at exact commit `a41e0419ba40fd1118775e8f516b1a31145f18d8`. Verified release assets include the Windows installer, portable ZIP and SHA-256 sidecar.

The first v1.0.27 exact-head build exposed one mechanical bootstrap defect: `tools/backend_lifecycle_tests.py` still hardcodes `EXPECTED_VERSION = "1.0.26"`. The backend itself starts and reports 1.0.27, so the lifecycle test times out waiting for an impossible version match. Treat this as the top v1.0.27 repair before any product work. Dev is currently paused; Coordinator will not alter scheduler state.

A direct Coordinator attempt to patch that single test expectation was blocked by the GitHub connector safety layer. The repository remains writable for documentation, so the engineering repair is still straightforward and is explicitly handed to Dev/Manager process owners rather than retried blindly by Coordinator.
