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

The permanent `release-control` mechanism is strategically acceptable. It preserves the important separation between a Dev Cycle release decision and an independently gated exact-SHA Windows build/export/smoke/publication process. Keep the historical explicit-tag workflow as fallback; do not weaken release safety merely for automation convenience.

The autonomous release path has nevertheless exposed orchestration defects during bootstrap:

1. The first release request failed because request discovery examined only the triggering commit rather than the full push range. The Dev Cycle repaired that without weakening release gates.
2. Latest autonomous-release run `34506900865` successfully parsed and validated the exact request for v1.0.20 at `6ead6ae2f3c2823b7b3b93f2fae805e6808f6f57`, then the same PowerShell step exited with code 1 before build/export gates ran.

The second failure is explained by the release-existence probe: `gh release view` is expected to return nonzero when the release does not exist, but that native exit code remains in `$LASTEXITCODE`. The script continues, emits its successful validation message and outputs, yet the step ends as failed because the native exit code was not neutralized/structured into an explicit success path.

### Coordinator conclusion

This is a **release-orchestration defect**, not a v1.0.20 application defect and not evidence that the selective-refactor direction is wrong.

Do not reopen application implementation on v1.0.20. The immediate critical path is:

1. Dev Cycle fixes only the release-control validation exit handling while preserving the existing-release immutability guard and all exact-SHA/build/export/hash/smoke gates.
2. Update/reissue the existing v1.0.20 request against the exact final branch HEAD and freeze v1.0.20 while it runs.
3. Drive the release workflow to a definitive success or a genuine application/release gate failure.
4. On publication, verify v1.0.20 and immediately move application development to v1.0.21.
5. Obtain user/reference-machine evidence for MS-009, MS-013, MS-018 and MS-022.
6. Any target-machine regression outranks planned post-release architecture work; otherwise resume broader MS-019/MS-020 work after acceptance evidence.

### Priority change

Previous P0 (`MS-019` bounded transform + one mesh-edit authority) is complete enough for the release candidate. Current P0 is release orchestration in service of `MS-018` publication/qualification. This is a sequencing change, not a product-architecture change.

### Risks

- release-controller debugging creating unnecessary application commits;
- weakening safety checks after repeated orchestration failures;
- moving the source branch after submitting an exact-SHA release request;
- interpreting CI/release success as target-machine acceptance;
- resuming broad migration before actual Stage-C acceptance evidence.

### User input

No product/design decision and no manual release action is required at this checkpoint. User input becomes necessary after v1.0.20 publication for real GTX 1080 / 16 GB acceptance testing.