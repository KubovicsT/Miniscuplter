# Miniscuplter Coordinator Log

> Durable Project Coordinator memory. Read completely on every Coordinator run. Preserve strategic decisions, rejected directions, evidence, and outcomes so the project does not oscillate without new evidence.

## 2026-09-10 — Initial substantive Coordinator review

### Review checkpoint

- Stable release: `v1.0.19`
- Stable application commit: `52f3b95fb6addc0f9f1e7123b75068da4ef1513c`
- Development branch: `v1.0.20`
- Latest fully validated application commit reviewed: `dfedbf533e0adb3b7712fe8e18e0a7d901b7929b`
- Branch contained later documentation-only commits after the validated application commit.
- Acceptance-weighted completion: 57%.
- Prior manual Coordinator attempt had correctly deferred because the Dev Cycle/CI state was still moving; no roadmap changes were made from that partial state.

### Evidence reviewed

- actual GitHub latest release and branch state;
- `PROJECT_CHARTER.md`;
- `PROJECT_STATUS.md`;
- `ISSUES.md`;
- `DECISIONS.md`;
- `HANDOFF.md`;
- `REFACTOR_PLAN.md`;
- `AUTOMATION_MANAGER.md`;
- `Core/StageCCleanup.cs`;
- `Scripts/Main.V1020StageCCleanupExport.cs`;
- recent Stage-C cleanup/export validation and the documented failed-then-fixed candidate-lineage regression.

### Strategic assessment

**Preserve the current selective-migration direction.**

The recent development trajectory is converging on the intended architecture rather than creating disconnected infrastructure. The Stage-C vertical slice has moved project authority from temporary files/widgets toward stable IDs, immutable revisions, transactional history, fail-closed save recovery, cleanup lineage, and exact export scope.

No evidence justifies a ground-up rewrite or a new framework-level detour. Compatibility bridges remain acceptable only where they move the active Stage-C slice toward Core authority.

### Priority decision — finish the state-authority gap before broadening

The most important current architecture gap is no longer generation or cleanup. It is **dual authority during ordinary 3D editing**: a mapped Stage-C object can still be visibly transformed/sculpted in Godot while durable Core project state may not reflect that edit. Because Stage-C export intentionally trusts ProjectStore, this can make the visible model disagree with the saved/exported model.

Coordinator direction:

1. make Stage-C move/rotate/scale transactional and durable;
2. migrate one bounded committed mesh-edit/sculpt path to immutable child revisions;
3. regression-test save/reload/undo/stale cleanup/export interactions;
4. avoid broad sculpt/UI migration until this slice is proven.

### Release-scope decision — v1.0.20 must remain bounded

`v1.0.20` should prove the Stage-C foundation slice, not absorb all remaining Stage B work.

Required before release candidate:

- authoritative Stage-C transforms;
- one authoritative committed mesh-edit path;
- trustworthy exact revision/export artifact path;
- normal automated gates;
- real Windows export + hash/artifact verification + installer smoke-install.

Explicit non-requirements for v1.0.20:

- full sculpt migration;
- complete Job Broker durability/crash recovery;
- Rig & Pose migration;
- kitbash migration;
- provider proliferation;
- global `Main.V*.cs` removal;
- full declarative UI rewrite.

### Release/acceptance sequencing correction

The existing HANDOFF wording treated target-machine acceptance as a reason not to publish v1.0.20. This creates a potential circular dependency: the user needs an immutable installable release to test the exact build on the reference machine.

Coordinator decision:

- target-machine acceptance remains required before Stage-C can be called accepted;
- however, once the bounded v1.0.20 code scope and release gates are complete, publish v1.0.20 as the testable increment;
- then collect GTX 1080 / 16 GB acceptance evidence using released v1.0.20;
- any discovered defects are fixed forward in v1.0.21, never by mutating v1.0.20.

This is a sequencing clarification, not a relaxation of MD-017 or the product acceptance standard.

### Issue-priority decision

1. `MS-018` remains the milestone-level critical issue.
2. `MS-019` becomes immediate P0 only for the Stage-C transform + one mesh-edit authority gap.
3. `MS-009` remains critical but should not consume speculative autonomous work without new target-machine evidence; if the released viewport is still blank, it immediately becomes P0.
4. `MS-013` similarly waits for representative target verification unless a concrete new leak is discovered.
5. `MS-022` needs one real intended lightweight/default 3D route on the reference PC; do not expand provider count first.
6. `MS-020` durable broker/queue/crash recovery remains important but is sequenced after Stage-C release unless a concrete job-lifecycle defect blocks the slice.

### Architecture boundaries reaffirmed

- Core owns durable project identity/state/revisions/history/provenance.
- Godot owns presentation and interactive projection of Core state; for migrated objects it must not independently own durable model truth.
- Python owns inference/geometry execution, not project state.
- STL is interchange/export only.
- Launcher/updater owns delivery/runtime preservation, not editing state.

### Risks to monitor

- dual Core/Godot authority during migration;
- `Main.V1020...` compatibility bridges becoming permanent business-logic layers;
- false confidence from CI where rendering/CUDA/storage require real-machine evidence;
- provider-readiness work drifting into adapter breadth;
- release starvation caused by pulling unrelated Stage B work into v1.0.20;
- oscillating architecture without evidence.

### User input

No product-level decision is currently required. User involvement becomes materially useful after v1.0.20 is released for reference-machine acceptance: viewport/grid/model/gizmo, storage containment, full Stage-C flow, and one intended 3D provider with practical time/RAM/VRAM observations.

### Next Coordinator review focus

- verify the Dev Cycle follows the new roadmap rather than only the older HANDOFF wording;
- assess whether transform state becomes Core-authoritative without duplicating undo/state paths;
- ensure the chosen mesh-edit migration remains deliberately narrow;
- watch whether v1.0.20 scope stays bounded;
- confirm release occurs when the bounded increment is genuinely ready rather than being delayed by unrelated refactor work;
- after release, reprioritize from real GTX 1080 evidence.
