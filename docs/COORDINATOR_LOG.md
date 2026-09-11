# Miniscuplter Coordinator Log

> Durable Project Coordinator memory. Preserve strategic decisions, rejected/failed approaches, evidence and outcomes so direction does not oscillate without new evidence.

## Durable architecture / sequencing history

### 2026-09-10 — Selective-refactor direction preserved
Core owns durable state; Godot owns presentation/input; Python owns inference/geometry. Stable IDs, immutable revisions and transactional history replace widget/name authority incrementally. STL is interchange/export. Migration is proven before legacy authority is removed.

### 2026-09-10 — Autonomous release-control validated
Exact-SHA publication independently validates C#/Core/Python/geometry, real Godot Windows export, package/hash verification, installer smoke test and immutable target. Published releases are immutable; failures fix forward.

### 2026-09-10 — Reference-machine evidence outranks CI
Target-machine evidence reopened viewport and Stage-C ownership issues despite green CI. User-observed runtime/UI/GPU issues remain FIXED - NEEDS USER VERIFICATION until real-machine retest passes.

### 2026-09-10 — v1.0.22 acceptance fixes
Duplicate Generate-3D ownership caused visible-but-nondurable output. Final composition established one Stage-C generation owner and preserved candidate/Apply persistence. Resize/client-fill and starter-scene guards were also shipped.

### 2026-09-10 — MS-027 bounded UI fallback
While acceptance was externally blocked, bounded modernization advanced workspace preferences, direct viewport tools, synchronized hierarchy, view cube/orbit, unified AI command/history, resource telemetry and density/polish. New presentation work should prefer stable version-neutral components.

## Release ownership history

### 2026-09-10 — Continuous-development Coordinator release model
Coordinator exclusively owns release readiness, chunk size and publication. Dev records release-worthy checkpoints but continues developing. Checkpoints are evidence, not freezes. Coordinator freezes only exact current branch HEAD, creates the next forward semantic-version branch from that boundary, then initiates release-control. Never rewind a moving branch to an older checkpoint.

## 2026-09-11 — v1.0.23 publication
v1.0.23 accumulated the planned MS-027 sequence and became a coherent release chunk. Release identity initially proved inconsistent across launcher/installer/updater/Godot/backend/editor/audit surfaces; strict release gates correctly blocked publication. The full identity set was repaired without weakening audit. Final candidate `bda683264448fc8b51c7c538db61f8c0487a699a` passed autonomous release run `34575505010` and published as immutable v1.0.23.

Lesson: release version identity is one coordinated contract across all user/tool-visible surfaces.

## 2026-09-11 — v1.0.24 structural trajectory and publication

While Stage-C reference-machine evidence remained unavailable, Dev completed a bounded MS-020 reliability seam covering heavyweight runtime/component ownership, truthful cancellation ownership, compact lifecycle/tombstone persistence, restart reconciliation, fail-closed corrupt/unsupported state and bounded journal reads.

Coordinator then ordered exactly one MS-019 authority-retirement seam. Dev retired historical `V109Generate3DAsync` authority by delegating to canonical `V1020Generate3DAsync`, added focused regression coverage, and repaired strict release-audit expectations to follow actual migrated authority rather than requiring the legacy implementation.

Coordinator froze v1.0.24 at exact boundary `784f408efba8a876b889fd704e051f22229de368` and created v1.0.25 from that boundary for continuous development. Autonomous release-control subsequently passed the exact-SHA C#/Core/Python/job/geometry checks, strict audit, real Godot Windows export, package/hash verification and installer smoke install. v1.0.24 published on 2026-09-11 at that exact target with `Miniscuplter-Setup-1.0.24.exe` and verified release artifacts.

This is architectural convergence: fewer competing owners, not another bridge.

## 2026-09-11 — v1.0.25 Rotate authority checkpoint review

### No-race / repository truth

Latest stable is v1.0.24 at `784f408efba8a876b889fd704e051f22229de368`. Current writable branch is v1.0.25. Reviewed branch HEAD before Coordinator documentation was `1808de62d3bdb2b26c26e95cdd33ec01b54d4eaa`; exact-head build/Core workflows completed successfully. No active release publication is pending for v1.0.24.

### Dev trajectory

Dev followed the bounded MS-019 fallback policy and migrated only mapped Rotate Y ±5° authority. The implementation derives requested rotation from durable Core state, commits through the established transactional path, projects the durable result back to Godot, and restores durable presentation on failure. Focused regression coverage prevents return to the generic scene-observed transform hook. Validated implementation checkpoint: `e0666bbd7e56d340f112238f78c836b958a64675`.

### Release chunk decision

**KEEP v1.0.25 ACCUMULATING.**

One additional bounded transform-authority seam after v1.0.24 is useful but too small for another Coordinator-curated release. No freeze or release request is justified. Continuous Dev remains preferable.

### Next sequencing decision

Stage-C reference-machine acceptance on released v1.0.24 remains P0. If evidence remains unavailable, the next approved bounded MS-019 seam is mapped Scale ±5% authority, using the same proven durable-Core-first transactional pattern. Do not combine it with ground placement, viewport-drag persistence, selection retirement or broad cleanup. Stop and validate after the scale seam, then continue only under the next available roadmap direction.

### User dependency

No product decision is required. Test released v1.0.24 end-to-end on the reference machine; any serious reproduced blocker immediately preempts fallback migration work.