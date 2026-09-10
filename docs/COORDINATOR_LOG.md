# Miniscuplter Coordinator Log

> Durable Project Coordinator memory. Read completely on every Coordinator run. Preserve strategic decisions, rejected directions, evidence, and outcomes so the project does not oscillate without new evidence.

## 2026-09-10 — Initial substantive Coordinator review

### Review checkpoint

- Stable release: `v1.0.19`
- Stable application commit: `52f3b95fb6addc0f9f1e7123b75068da4ef1513c`
- Development branch: `v1.0.20`
- Latest fully validated application commit reviewed: `dfedbf533e0adb3b7712fe8e18e0a7d901b7929b`
- Acceptance-weighted completion: 57%.

### Strategic assessment

Preserve the selective-migration direction. Finish the Stage-C state-authority gap before broadening: transactional mapped-object transforms, one bounded immutable mesh-edit path, and regression coverage through save/reload/undo/cleanup/export. Keep v1.0.20 bounded and publish it as the testable increment before target-machine acceptance. No product-level user decision was required.

### Durable decisions

- Core owns durable project state; Godot projects migrated state; Python executes inference/geometry; STL is interchange/export only.
- v1.0.20 must not absorb full sculpt migration, full Job Broker durability, Rig/Pose migration, kitbash migration, provider proliferation, global legacy removal, or a full UI rewrite.
- Target-machine acceptance remains required for Stage-C acceptance, but publication of a verified installable build may precede that evidence so the user can test the exact immutable build.
- Any target-machine defect is fixed forward in the next semantic version.

---

## 2026-09-10 — v1.0.20 release-orchestration checkpoint

### No-race / repository checkpoint

- Latest Dev Cycle had completed before this review.
- Latest published stable remained `v1.0.19`.
- Development branch remained `v1.0.20`.
- Live v1.0.20 HEAD observed before Coordinator documentation changes: `6ead6ae2f3c2823b7b3b93f2fae805e6808f6f57`.
- Validated application/release-candidate code: `bc3106d606b4450aa5cb9d4395b77a5d6e78f11a`.
- Acceptance-weighted completion: **58%**.

### Dev Cycle trajectory assessment

The Dev Cycle followed the Coordinator roadmap successfully. It completed the bounded v1.0.20 application target rather than broadening:

- mapped move/rotate/scale/ground and gizmo commits now update durable Core transform state;
- save/reload and export use durable transform state;
- Stage-C undo/redo uses project transactions;
- one bounded sculpt/edit path creates immutable child mesh revisions with stale-parent protection;
- restore, cleanup and export remain aligned to object/revision authority;
- regression coverage was added for transform persistence, sculpt lineage, undo/redo, save/reload and export interactions.

This is evidence of architectural convergence, not churn. The previous P0 state-authority gap is sufficiently complete for the bounded release candidate.

### Autonomous release-control assessment

The permanent `release-control` mechanism is strategically acceptable. It preserves separation between a Dev Cycle release decision and an independently gated exact-SHA Windows build/export/smoke/publication process. Keep the historical explicit-tag workflow as fallback; do not weaken release safety merely for automation convenience.

Two bootstrap defects were observed:

1. request discovery initially examined only the triggering commit rather than the full push range;
2. an expected failing `gh release view` probe left `$LASTEXITCODE` nonzero after otherwise-successful validation.

Both were classified as release-orchestration defects rather than source-candidate defects. The Dev Cycle was directed to repair them without reopening v1.0.20 application scope or weakening gates.

### Priority change

Previous P0 (`MS-019` bounded transform + one mesh-edit authority) was complete enough for the release candidate. Release orchestration became immediate P0 in service of `MS-018` publication/qualification.

---

## 2026-09-10 — v1.0.20 published / target-acceptance checkpoint

### No-race and repository checkpoint

- The latest Dev Cycle completed before substantive review.
- `v1.0.20` is now the published stable release.
- Published target: `e63cb0601cdbb91af5be191458ecdbcb7c0b7944`.
- Autonomous-release run `34508060099` completed successfully through exact-SHA validation, C#/Core, Python/runtime, job/geometry regressions, release audit, verified Godot 4.7.2 Windows export, output/hash verification, installer creation, silent installer smoke-install, immutable-target recheck, tag creation and GitHub Release publication.
- Forward-only development branch `v1.0.21` exists from the exact published target.
- v1.0.21 HEAD reviewed before Coordinator roadmap update: `214b890b3c46be4ef7451c5c9a8fd60f03eee10f`.
- Exact-head v1.0.21 Core, C#, Python/geometry/release-audit and packaging jobs are green. Full Windows-release/publication jobs were correctly skipped for an ordinary branch push.
- Acceptance-weighted completion remains **58%** because release publication does not substitute for target-machine acceptance.

### Outcome of previous direction

The Dev Cycle followed the roadmap successfully:

- it fixed the release-controller `$LASTEXITCODE` defect at the release-control boundary;
- it did not reopen or broaden v1.0.20 application scope;
- it preserved exact-SHA, build/export/hash/smoke and immutability gates;
- it drove the controller to a successful real Windows release;
- it preserved v1.0.20 immutability and moved forward to v1.0.21.

This validates both the bounded-release sequencing and the permanent autonomous release-control architecture. The explicit-tag release path remains fallback only.

### Strategic assessment

**Preserve the selective-refactor direction.** No architectural reset is warranted.

The critical path has now moved from architecture implementation and release orchestration to **real reference-machine acceptance**. The correct response is not to immediately begin another broad migration. The project needs runtime evidence on the intended GTX 1080 / 8 GB VRAM / 16 GB RAM machine before deciding which structural work should follow.

### Priority decision

Current order:

1. `MS-018` — complete the released v1.0.20 Stage-C flow on the reference machine.
2. `MS-009` — verify viewport/grid/model/gizmo; reproduced blank viewport becomes immediate P0.
3. `MS-013` — verify storage containment; severe leakage/safety regression becomes P0.
4. `MS-022` — qualify one intended lightweight/default 3D provider with elapsed time and RAM/VRAM evidence.
5. `MS-004` — cancellation/recovery during a real job where practical.
6. broader `MS-019` / `MS-020` work waits behind acceptance unless a concrete runtime blocker requires it.

### v1.0.21 scope decision

Until target-machine evidence arrives, v1.0.21 is primarily a forward-fix/acceptance-support branch.

Allowed work:
- fixes for reproduced v1.0.20 defects;
- diagnostics directly needed to establish acceptance evidence;
- regressions for observed failures;
- accurate acceptance documentation.

Do not use scheduled-cycle availability as a reason to start full sculpt migration, broad legacy removal, full Job Broker reconstruction, Rig/Pose, kitbash, provider proliferation, UI rewrite, or optional cleanup breadth.

If target-machine acceptance is green, the next Coordinator review should sequence the next structural milestone. Current default ordering after acceptance is `MS-020` durable job ownership/queue/crash recovery, then `MS-019` outward migration from the proven Stage-C seam, provider-tier decisions from measured `MS-022` evidence, then broader Stage-D practical editing work.

### Risks to monitor

- treating CI/release success as real-machine acceptance;
- Dev Cycles drifting into generalized infrastructure while waiting for user evidence;
- v1.0.21 becoming a broad migration branch before acceptance;
- compatibility bridges becoming long-term duplicate authority;
- provider policy being based on theoretical support instead of measured reference-machine behavior.

### User dependency

No product-level decision is required. The important dependency is now real testing of released v1.0.20 on the reference machine. Concrete pass/fail observations should drive v1.0.21 fixes. If testing exposes a difficult-to-reverse product tradeoff, escalate that decision to the user rather than embedding it in an implementation fix.
