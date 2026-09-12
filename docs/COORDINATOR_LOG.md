# Miniscuplter Coordinator Log

> Durable Project Coordinator memory. Preserve strategic decisions, rejected/failed approaches, evidence and outcomes so direction does not oscillate without new evidence.

## Durable architecture and release rules

- **2026-09-10 — Selective-refactor direction preserved.** Core owns durable state; Godot owns presentation/input; Python owns inference/geometry. Stable IDs, immutable revisions and transactional history replace widget/name authority incrementally. STL is interchange/export. Migration is proven before legacy authority is removed.
- **2026-09-10 — Autonomous release-control validated.** Exact-SHA publication independently validates C#/Core/Python/geometry, real Godot Windows export, package/hash verification and installer smoke test. Published releases are immutable; failures fix forward.
- **2026-09-10 — Reference-machine evidence outranks CI.** User-observed runtime/UI/GPU issues remain FIXED - NEEDS USER VERIFICATION until real-machine retest passes.
- **2026-09-10 — Continuous-development Coordinator release model.** Coordinator exclusively owns release readiness, chunk size and publication. Dev records release-worthy checkpoints but continues developing. Coordinator freezes only exact current branch HEAD, creates the next forward semantic-version branch from that boundary, then initiates release-control. Never rewind a moving branch to an older checkpoint.
- **2026-09-10 — MS-027 bounded fallback.** UI modernization is opportunistic only while higher-priority acceptance/correctness work is blocked; serious blockers preempt it.

## Recent release trajectory

### 2026-09-11 — v1.0.23
MS-027 work accumulated into a coherent release. Release identity initially diverged across surfaces; strict audit correctly blocked publication until launcher/installer/updater/Godot/backend/editor/audit identity was reconciled. Lesson: release version identity is one coordinated contract.

### 2026-09-11 — v1.0.24
Dev completed a bounded MS-020 reliability seam and retired historical Generate-3D authority into the canonical path. Coordinator froze exact HEAD `784f408efba8a876b889fd704e051f22229de368`; release-control passed exact-SHA validation/export/package/hash/installer smoke and published v1.0.24.

### 2026-09-11 — v1.0.25
Mapped Move, Rotate and Scale command authority accumulated into a coherent MS-019 release chunk. Coordinator froze exact HEAD `a7fc4bcf5f771c18060e5aee7c98026131731c2a`, created v1.0.26 forward, and autonomous release-control published v1.0.25 successfully with verified Windows artifacts.

## 2026-09-11 — v1.0.26 viewport-drag authority checkpoint review

### No-race / repository truth
Latest stable is v1.0.25 at `a7fc4bcf5f771c18060e5aee7c98026131731c2a`. Current writable branch is v1.0.26. Reviewed pre-Coordinator HEAD `f4bc5a5daf8633b4078db02969d044caaf730f01`; its build and core-foundation workflows completed successfully. No newer application commit or active publication was observed, so planning mutation was safe.

### Dev trajectory
Dev completed the single previously authorized MS-019 viewport-drag transform authority seam. Validated implementation checkpoint `d4aef5d55170ef45298e9db942cb250d78e911d2` captures durable Core state plus presentation start state, persists only Move/Rotate/Scale gesture deltas/ratio through Core, rejects stale state, and reprojects/restores presentation from durable Core truth. Focused regressions and exact-head validation are green.

### Release chunk decision
**KEEP v1.0.26 ACCUMULATING.** The checkpoint is coherent and release-worthy evidence, but one bounded post-v1.0.25 authority seam is still too small for another Coordinator-curated release. No freeze or release request is justified.

### Next sequencing decision
Stage-C reference-machine acceptance on released v1.0.25 remains P0. If evidence remains unavailable, the next approved bounded MS-019 seam is mapped-object **ground placement transform authority**, following the same durable-Core-first transactional pattern. Do not combine it with selection retirement, broad persistence cleanup, sculpt architecture, MS-020 expansion or MS-027. Stop and validate after that seam; a later Coordinator review decides whether v1.0.26 has become release-sized or should accumulate further.

### User dependency
No product decision is required. Reference-machine v1.0.25 end-to-end Stage-C, viewport/resize, storage containment, cancellation/recovery and resource-behavior evidence remains the principal acceptance dependency.

## 2026-09-11 — MS-029 self-update launcher termination preemption

### Reference-machine evidence
User reported that after an application update the Miniscuplter launcher opens briefly and then closes instead of remaining open on the new version. This is accepted as higher-authority runtime evidence and immediately preempts the planned MS-019 ground-placement fallback.

### Root cause confirmed from repository
The released updater's `VerifyLauncherStartup` starts the updated launcher with `--update-health-token`, waits for the token, and then kills that process from `finally` even on the successful-health path. The transaction then records launcher health, commits, cleans the update package/work directory and exits without a normal launcher restart. `Launcher/Program.cs` only writes the token on `Shown`; it does not close or relaunch itself. The code behavior directly matches the user's observation.

### Priority / release decision
Created MS-029 as Critical / IN PROGRESS. v1.0.26 stays writable and must **not** freeze before this regression is fixed. Ground-placement MS-019 work is preempted. After a bounded MS-029 fix plus regression and Windows update-path evidence, Coordinator should immediately reassess release readiness; the existing viewport-drag authority work plus the release-reliability fix is likely release-sized.

### Transition risk
The updater that installs a new release is copied from the previously installed version. Therefore a fix packaged in the next version does not automatically change the v1.0.25 updater performing the v1.0.25 → next-version transition. Dev must explicitly account for this compatibility edge. A one-time manual reopen may remain unavoidable for that transition unless a safe bridge is possible; published v1.0.25 remains immutable.

### User dependency
No product decision is required. A single manual launcher reopen can distinguish the identified one-time post-update kill from any separate startup crash. Autonomous hotfix work can otherwise continue.

## 2026-09-11 — v1.0.25 UI acceptance feedback raises MS-027 from fallback to explicit scope

### Reference-machine evidence
User supplied a v1.0.25 screenshot and explicit corrections. The top AI interaction remains a one-line prompt plus a large separate action row rather than the intended scrollable multi-line command console. The viewport's upper-right orientation control is a flat box of letter buttons, not a real 3D view cube. The upper-left viewport tool overlay still contains letter controls plus permanent instructions. The right-side workflow tabs still devote substantial space to explanatory prose.

### Product-direction decision
This is explicit user-owned UX direction, not optional polish. MS-027 is therefore no longer merely opportunistic fallback. It becomes the next product-facing v1.0.26 tranche after the Critical MS-029 updater regression is fixed, unless a new serious correctness/data/release blocker appears.

### Bounded tranche
1. Multi-line, vertically scrollable AI command console/history with explicit Run and one existing dispatch owner; remove the oversized top action band.
2. Actual interactive 3D orientation cube in the viewport corner, using existing camera/view authority.
3. Compact viewport tool controls with active state; remove permanent instructional paragraph.
4. Remove right-panel explanatory prose and expose detailed descriptions through small circular `i` hover affordances/tooltips. Preserve direct visibility for live state, errors, progress and warnings.

Do not expand this tranche into scene-tree redesign, resource graphs, new modeling functionality, or duplicate state ownership.

### Release implication
v1.0.26 remains writable. Do not freeze before MS-029 is fixed and this user-directed MS-027 tranche is reviewed. The combination of viewport-authority work, release-reliability hotfix and visible UI acceptance corrections is expected to form a coherent substantial release candidate once validated.

## 2026-09-11 — MS-009 re-opened by v1.0.25 splitter-resize evidence

### Reference-machine evidence
User supplied a second v1.0.25 screenshot showing that after panel/splitter resize the 3D viewport/grid becomes materially darker. The UI still fills the client area, so this is not evidence that MS-024's black-seam/client-fill symptom returned; it is the already-tracked MS-009 renderer/presentation instability.

### Acceptance consequence
MS-009 moves from FIXED - NEEDS USER VERIFICATION back to **IN PROGRESS**. The v1.0.22 presentation guard did not solve the real reference-machine behavior. Automated green status did not constitute acceptance and must not be used to re-close the issue without target-machine evidence.

### Technical direction
After MS-029, Dev must investigate MS-009 before the MS-027 UI tranche. The current resize handler already reapplies studio lighting, hides the opaque grid ground, updates the gizmo and arms a render probe, yet the failure persists. Therefore the next attempt must compare pre/post-resize SubViewport, World3D/environment, camera, grid/material and render-target state and identify the actual changed owner/state. Avoid another blind timer/full-repair layer and avoid restoring competing manual `SubViewport.Size` ownership unless instrumentation proves Stretch itself is the problem.

### Release consequence
v1.0.26 remains writable and is not release-ready. Do not freeze it while MS-029 or MS-009 is unresolved. The user-directed MS-027 tranche remains in scope after those critical regressions are cleared.

## 2026-09-11 — MS-030 backend health blocker

Reference-machine v1.0.25 fails Stage-C 3D generation at the local backend health check even after Repair AI Runtime succeeds. This makes MS-030 Critical/P0 and blocks the main 2D-to-3D path.

Repository inspection shows Repair validates the backend-directory virtual environment, while the editor backend launcher can select a different Python executable first. Repair also does not currently prove that the packaged backend can start and answer its health endpoint. Dev must confirm the actual selected interpreter/process-exit evidence, unify the runtime ownership contract, add a real startup/health smoke to Repair, and improve startup diagnostics.

MS-030 preempts MS-029, MS-009 and MS-027 until backend health works. v1.0.26 remains writable and is not release-ready.

## 2026-09-11 — AMP-005 first end-to-end review and rolling execution plan

### No-race / repository truth
Latest stable remains v1.0.25 at `a7fc4bcf5f771c18060e5aee7c98026131731c2a`. v1.0.26 is writable with no release request. Dev's latest implementation checkpoint `1e6ed354...` and the pre-review docs HEAD `845e76a9...` both had green Core/build Actions. The last Dev run completed before this review and no release workflow was active, so planning mutation was safe.

### Whole-system assessment
The selective-refactor architecture remains correct. Recent failures share a common seam problem: historical layers can each be locally reasonable while no single end-to-end owner proves the full runtime/render/UI contract. The response is authority convergence plus acceptance evidence, not another broad rewrite.

### MS-030 deeper diagnosis
Dev correctly unified Repair/editor interpreter selection and added stronger diagnostics, but Coordinator inspection found both paths still execute `python app.py`. The Python module defines FastAPI `app` and endpoints but no executable server entry point, so this command does not actually start Uvicorn. This invalidates any claim that MS-030 is fixed merely because source regressions/builds are green. Repair also probes fixed production port 7868, which can validate an unrelated already-running backend. MS-030 therefore stays IN PROGRESS and P0 until one canonical real server-start contract plus process-bound/isolated health smoke is proven.

### MS-029 release-path judgment
The updater health-probe fix is coherent and CI-green. Published v1.0.25 cannot be retroactively changed, so its updater may still close the launcher once while installing v1.0.26. Accept a manual reopen for that immutable one-transition edge; do not spend architecture complexity trying to retrofit an old published updater. MS-029 moves to FIXED - NEEDS USER VERIFICATION for future fixed-updater transitions.

### Viewport/UI architecture risk
MS-009 should be treated as a presentation-authority conflict until disproven. Do not add another resize watchdog/reassertion layer; instrument and retire the changing owner. For MS-027, converge toward one current workspace composition owner while reusing action/state owners; avoid another independent versioned UI state machine.

### AMP-005 execution plan
HANDOFF now carries four substantial objectives: canonical backend startup, viewport resize authority, the user-directed compact UI tranche, then integrated v1.0.26 release-candidate hardening. Dev may auto-proceed A→B→C→D. The queue intentionally stops after D because release/freeze becomes a Coordinator-owned strategic boundary. This provides multi-run depth without speculative unrelated work.

### Release decision
KEEP v1.0.26 ACCUMULATING. Do not freeze while MS-030/MS-009 or the defined MS-027 tranche are incomplete. After Objective D, review exact current HEAD; if coherent and green, create the forward version branch and initiate release-control from that exact boundary.

## 2026-09-12 — v1.0.26 release initiation and forward-branch reconciliation

### Repository / no-race truth
Dev automation is paused. No conflicting application-mutating worker was active. v1.0.26 authoritative HEAD was `a41e0419ba40fd1118775e8f516b1a31145f18d8`; exact-head Core and build CI both passed. Latest stable remained v1.0.25 and no v1.0.26 release request existed before this run.

### Release preflight
The current v1.0.26 HEAD is the intended coherent release boundary. Release identity is consistent across launcher, updater/editor assembly, installer, Windows export metadata, backend API, editor display and release audit. The release chunk contains backend startup/Repair health hardening, updater health-probe correction, viewport resize-owner convergence, Core-authoritative viewport drag persistence and the explicit compact-workspace tranche. User-observed runtime/GUI issues remain verification-dependent rather than being treated as resolved by CI.

### Release decision and execution
Coordinator judged v1.0.26 release-sized and initiated exactly one autonomous release-control request for `a41e0419ba40fd1118775e8f516b1a31145f18d8`. That request is the v1.0.26 freeze boundary; no later app/docs mutations belong on v1.0.26 while publication is pending.

### Premature v1.0.27 reconciliation
A prior v1.0.27 branch existed at older SHA `30f42c5...` before any legitimate v1.0.26 freeze and still carried 1.0.26 identity. It was therefore classified PREMATURE / ORPHAN and was not treated as current. During this real release transition it was safely fast-forwarded to the actual frozen candidate, then mechanically bootstrapped to 1.0.27 identity. v1.0.27 is now the authoritative writable branch.

### Technical trajectory
Selective refactor remains correct. The immediate product critical path after publication is reference-machine acceptance of the repaired runtime, viewport and workspace followed by full Stage-C thin-slice validation. Forward migration is deliberately bounded to mapped-object ground placement and one stable-ID selection seam, both preempted by any new reference-machine blocker.

### Queue
HANDOFF now carries three substantial objectives: v1.0.27 bootstrap validation, bounded ground-placement authority, and one bounded stable-ID selection/picking seam. Queue depth is intentionally limited because imminent v1.0.26 target-machine evidence can legitimately change Stage-D priority.

## 2026-09-12 — v1.0.26 publication completed

Autonomous release-control completed successfully for exact frozen candidate `a41e0419ba40fd1118775e8f516b1a31145f18d8`. The workflow passed exact-SHA validation, real Godot 4.7.2 Windows export, versioned output verification, silent installer smoke-install, asset staging/upload, immutable tag creation and GitHub Release publication. Latest stable is now v1.0.26.

Verified published assets:
- `Miniscuplter-Setup-1.0.26.exe`
- `Miniscuplter-win-x64.zip`
- `Miniscuplter-win-x64.zip.sha256`

The reconciled v1.0.27 forward branch is now authoritative and writable. Its first exact-head build exposed one mechanical bootstrap defect: `tools/backend_lifecycle_tests.py` still expects backend version 1.0.26. The backend starts as 1.0.27, so the test never accepts the otherwise healthy response and times out. This is a version-identity bootstrap defect, not evidence that MS-030 regressed.

Coordinator attempted the minimal one-line test expectation correction, but that code-file write was blocked by the GitHub connector safety layer. Per role boundaries, the repair is left in HANDOFF as Objective A rather than repeatedly hammering connector writes. Dev remains paused; Coordinator did not modify automation state.

Release success moves the product critical path to reference-machine acceptance of v1.0.26. Any reproduced Critical backend/viewport/update/persistence issue preempts v1.0.27 migration work.

