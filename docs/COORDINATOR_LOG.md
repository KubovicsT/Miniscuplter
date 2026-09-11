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

## 2026-09-11 — v1.0.24 structural trajectory

While Stage-C reference-machine evidence remained unavailable, Dev completed a bounded MS-020 reliability seam:
- heavyweight runtime/component ownership;
- truthful cancellation ownership;
- compact lifecycle/tombstone persistence;
- restart reconciliation;
- fail-closed corrupt/unsupported state;
- bounded journal reads.

Coordinator then ordered exactly one MS-019 authority-retirement seam. Dev retired historical `V109Generate3DAsync` authority by delegating to canonical `V1020Generate3DAsync`, added focused regression coverage, and repaired strict release-audit expectations to follow actual migrated authority rather than requiring the legacy implementation. Exact-head v1.0.24 CI returned green.

This is architectural convergence: fewer competing owners, not another bridge.

## 2026-09-11 — v1.0.24 release boundary / v1.0.25 forward branch

### Release chunk decision

**FREEZE v1.0.24 FOR RELEASE.**

Exact boundary: `784f408efba8a876b889fd704e051f22229de368`.

Rationale: the version now combines two coherent, related reliability/migration increments—bounded MS-020 job lifecycle ownership/recovery plus one proven MS-019 Stage-C authority retirement. This is materially larger and more useful than the earlier MS-020-only checkpoint, while further accumulation would unnecessarily delay a meaningful structural increment. Branch CI at exact boundary is green; release-control remains responsible for full Windows/Godot/package/installer proof.

### Continuous development

Created v1.0.25 from the exact v1.0.24 boundary before release initiation. v1.0.25 is writable; v1.0.24 is frozen once its release request is submitted. Dev must continue on v1.0.25 rather than wait for publication.

### Next fallback direction

Stage-C acceptance remains P0. If user evidence is still unavailable, v1.0.25 may retire one next smallest proven duplicate authority under MS-019, preferably transform/selection/persistence where the replacement owner is already regression-testable. Do not broaden MS-020 or resume opportunistic MS-027 UI expansion merely because acceptance is external.

### User dependency

No product decision required. Test published v1.0.23 end-to-end on the reference machine; any reproduced serious blocker immediately preempts fallback work.