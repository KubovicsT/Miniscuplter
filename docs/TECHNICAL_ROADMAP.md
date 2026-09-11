# Miniscuplter Technical Roadmap

> Authoritative end-to-end technical sequencing owned by the Project Coordinator. PROJECT_CHARTER/DECISIONS own product truth; HANDOFF owns the immediate/downstream Dev baton.

Last coordinator review: 2026-09-11
Latest published stable: `v1.0.25` at `a7fc4bcf5f771c18060e5aee7c98026131731c2a`
Current writable development branch: `v1.0.26`
Reviewed branch HEAD: `845e76a9eb56b186eddcd5f476fb55b9cf92a694`
Latest CI-green implementation checkpoint: `1e6ed3541de6de4bec19838e71595e4b76dbedfa`
Acceptance-weighted completion: **57%**

## 1. Whole-system direction assessment

**PRESERVE THE SELECTIVE-REFACTOR DIRECTION.**

The application is converging around the intended ownership model: Core owns durable project/object/revision/history state; Godot owns presentation, viewport/input and live interaction; Python owns inference and geometry execution; Launcher/updater owns runtime installation/repair and delivery safety.

The strongest current risk is duplicate or incomplete authority at subsystem seams left by historical layering. Recent field failures show this in runtime repair vs backend launch, viewport resize/presentation, and overlapping UI composition. Prioritize authority convergence plus acceptance evidence, not generalized infrastructure or feature breadth.

## 2. Critical MS-030 finding

Dev correctly removed interpreter divergence and made Repair/editor use the same backend-local virtual environment. Those commits are CI-green.

However, end-to-end inspection found a deeper launch-contract defect:

- `ai_backend/app.py` defines the FastAPI application but has no executable server entry point;
- both `Scripts/BackendLauncher.cs` and `Launcher/RuntimeSetupService.cs` currently start it as `python app.py`;
- that process can exit without starting Uvicorn or listening on port 7868.

Therefore MS-030 is **not fixed** despite green CI.

Repair also probes fixed production port 7868. If another backend is already listening, Repair could validate the wrong process. The smoke must prove the process/runtime it launched, preferably via an isolated smoke port plus expected health identity/version.

## 3. Release decision

**KEEP v1.0.26 ACCUMULATING. DO NOT FREEZE.**

Current scope is valuable and coherent, but release is blocked by incomplete MS-030, reproduced MS-009, and the explicit user-directed MS-027 acceptance tranche.

MS-029 is no longer a reason to hold the version once its code is reconciled: the current fix protects future updater transitions, while immutable v1.0.25 may still require one manual launcher reopen when installing v1.0.26. Do not add risky retrofit machinery solely to hide that one-transition limitation.

## 4. Rolling execution plan

### Objective A — canonical backend startup / repaired-runtime contract (CURRENT, P0)

**Outcome:** a repaired packaged runtime and the editor launch the same actual FastAPI server process through one deterministic startup contract.

**Constraints:** backend-local repaired venv remains authoritative; no PATH/system Python fallback; one canonical server-start contract for Repair/editor; loopback only; Repair must not falsely pass against an unrelated server; preserve process-tree containment, cancellation recovery, controlled data-root environment and diagnostics.

**Acceptance:** actual Uvicorn/FastAPI server reaches health; Repair validates the server instance/runtime it launched and expected health identity/version; editor uses the same launch contract; focused startup tests plus full exact-head build/Core/release-audit/package validation pass; MS-030 becomes FIXED - NEEDS USER VERIFICATION.

**Preemption:** new data-loss/runtime-corruption/release-safety evidence.

**Auto-proceed:** yes → Objective B.

### Objective B — MS-009 viewport resize/render authority convergence (P1)

**Outcome:** splitter/window resize no longer alters viewport/grid/model/background presentation.

**Constraints:** instrument before changing architecture; Godot remains presentation/input owner; keep Stretch as normal size owner unless evidence disproves it; no watchdog/timer, blind delayed full repair, or second manual SubViewport size owner; identify and retire the conflicting historical presentation/resize path.

**Acceptance:** root state transition identified and regression-covered; strongest Windows/Godot render validation plus exact-head CI/package gates pass; MS-009 becomes FIXED - NEEDS USER VERIFICATION.

**Preemption:** runtime/persistence/data-loss/blank-viewport regression.

**Auto-proceed:** yes → Objective C.

### Objective C — user-directed MS-027 compact workspace tranche (P2)

**Outcome:** deliver the four visible UI corrections requested by the user as one coherent workspace result.

Required scope:
- multi-line vertically scrollable AI command console/history with Run;
- real interactive orientation cube;
- compact viewport tool controls without permanent instruction text;
- remove workflow-panel explanatory prose and replace detailed help with small circular `i` hover help/tooltips.

**Constraints:** reuse authoritative action/command, viewport-tool, camera and selection owners; converge toward one current workspace composition owner rather than another parallel UI state machine; replacement controls must work before obsolete surfaces are retired; viewport remains dominant; no scene-tree/resource-graph/new-modeling/Rig-Pose expansion.

**Acceptance:** UI/layout regressions cover routing/ownership and persistence; supported resize/font scales remain usable; no Stage-C/save/load/transform/cancellation/export regression; exact-head validation/package gates green.

**Preemption:** new Critical correctness/runtime/persistence/viewport/release blocker.

**Auto-proceed:** yes → Objective D.

### Objective D — integrated v1.0.26 release-candidate hardening (P3)

**Outcome:** produce one coherent exact-HEAD candidate containing runtime, updater, viewport and user-directed UI acceptance work.

**Constraints:** no unrelated feature expansion; reconcile issue states honestly; document the immutable-v1.0.25 updater limitation; run strongest C#/Core/Python/runtime/geometry/execution/release-audit/Windows export/package/hash/installer checks; verify storage/data/runtime-cache/venv preservation and version identity; Dev does not publish.

**Acceptance:** exact current HEAD coherent; all available automated/package gates green; release-worthy checkpoint recorded; docs list target-machine checks accurately; HANDOFF marks `COORDINATOR REVIEW REQUESTED`.

**Preemption:** any failing gate/new serious defect.

**Auto-proceed:** no — release boundary/freeze is Coordinator-owned.

## 5. Dependencies and non-priorities

Dependency order:

`canonical backend start → stable viewport resize → compact workspace → integrated release hardening → Coordinator release decision → reference-machine Stage-C acceptance`

Do not resume mapped-object ground placement/MS-019, broad MS-020 expansion, full sculpt migration, provider expansion, Rig/Pose expansion, kitbash breadth, scene-tree redesign or resource-graph work before this release candidate is reviewed.

## 6. Issue priority

1. **MS-030 — Critical / IN PROGRESS:** backend server-start contract remains incomplete.
2. **MS-018 — Critical / IN PROGRESS:** end-to-end Stage-C acceptance; target retest waits for a fixed build.
3. **MS-009 — Critical / IN PROGRESS:** real renderer still changes/darkens after resize.
4. **MS-029 — Critical historical release-path regression / FIXED - NEEDS USER VERIFICATION:** current updater fix is CI-green; old v1.0.25 updater can still cause one unavoidable manual reopen.
5. **MS-027 — High / IN PROGRESS:** user-directed compact workspace tranche.
6. **MS-023 — Critical / FIXED - NEEDS USER VERIFICATION:** durable generation/apply/reload ownership.
7. **MS-013 / MS-024 / MS-004 / MS-025 — verification-dependent.**
8. **MS-022 — High / IN PROGRESS:** provider qualification after the thin slice runs.
9. **MS-019 / MS-020 — migration work:** resume after current acceptance/release tranche.

## 7. User / target-machine dependency

No product decision is needed.

Do not retry v1.0.25 Repair/3D generation, MS-009 resize reproduction or MS-027 UI reproduction again.

When the next fixed release is available, retest update/reopen, Repair health, Generate 3D reaching provider resolution/inference, resize invariance, the compact UI tranche, then continue Stage-C through candidate Apply, save/reopen, edit/sculpt, cleanup and exact STL export.
