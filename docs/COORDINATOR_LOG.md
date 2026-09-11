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
