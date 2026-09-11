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
