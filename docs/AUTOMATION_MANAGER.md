# Miniscuplter Automation Manager

> Durable supervisory memory for scheduled-task/process reviews. Read this file completely at the start of every manager run. Preserve failed/rejected history.

Last manager review/update: 2026-09-10

## Stable hierarchy

1. User / PROJECT_CHARTER — fundamental product intent, scope, hard constraints, difficult-to-reverse product decisions.
2. Project Coordinator — technical roadmap, architecture direction, milestone decomposition, issue priority, dependency ordering, next-version scope.
3. Dev Cycle — implementation worker, validation, execution-state docs, release readiness/publication.
4. Automation Manager — process/agent supervision and automation-change proposals, subject to user approval.
5. Daily Report — reporting only.

## Current task registry

### Miniscuplter Project Coordinator
- Status: enabled.
- Schedule: 04:30, 10:30, 16:30, 22:30 Europe/Budapest.
- Role: technical director / lead architect / roadmap owner.
- Durable outputs: `docs/TECHNICAL_ROADMAP.md`, `docs/COORDINATOR_LOG.md`.
- Current observation: first manually-triggered coordinator review respected the no-race gate and deferred because the preceding Dev Cycle/validation state was still moving. No roadmap/log files have been created yet on `v1.0.20`; this is appropriate for a deferred run, not a coordinator failure.

### Miniscuplter Dev Cycle
- Status: enabled.
- Schedule: hourly on whole hours.
- Role: implementation worker executing Coordinator roadmap + HANDOFF.
- AMP-001 release/version reconciliation gate remains applied.
- Release authority remains with Dev Cycle, but releases are readiness-based, not time-based.

### Miniscuplter Daily Report
- Status: enabled.
- Schedule: daily 20:00 Europe/Budapest.
- Role: reporting only; includes Coordinator and Automation Manager summaries.

### Miniscuplter Automation Manager
- Status: enabled; excluded from worker scoring.
- Role: process/automation supervision only.
- No-race and no-live-mutation rules remain in force.

## Management principles

1. Judge acceptance evidence and reduced risk, not commit volume.
2. Actual Git/release/CI state outranks stale docs.
3. Published releases are immutable; post-release work belongs on the next forward semantic-version branch.
4. GUI/GPU/user-observed defects are not resolved by compilation alone.
5. Prefer one reliable Stage-C end-to-end workflow over disconnected feature/provider accumulation.
6. Prompt/schedule changes require explicit user approval unless directly user-commanded in conversation.
7. Review completed agents only; defer rather than infer from partial HANDOFF/roadmap states.
8. Never mutate a target automation while it is running.
9. Coordinator owns technical direction; Manager audits the Coordinator's evidence, stability, and role adherence rather than replacing its roadmap.
10. Dev Cycle should follow Coordinator roadmap and HANDOFF, while retaining authority for ordinary implementation decisions and urgent regression containment.

## Current repository checkpoint

- Repository: `KubovicsT/Miniscuplter`.
- Latest published stable: `v1.0.19` at application commit `52f3b95fb6addc0f9f1e7123b75068da4ef1513c`.
- Current development branch: `v1.0.20`.
- Exact branch HEAD observed during this review: `2925f6e271fd3fc0e992ddc0b671cb0b4a223a24` (`Hand off Stage C editing-state migration`).
- Latest application/code commit named by HANDOFF: `dfedbf533e0adb3b7712fe8e18e0a7d901b7929b`.
- Acceptance-weighted completion: 57%.
- Current Stage-C state: accepted baseline → identity-bound generation → explicit Apply → save/reload → immutable cleanup child revision → explicitly-scoped validated STL export is integrated in code; normal transform/sculpt authority and target-machine qualification remain.
- Exact-head C# / broader build for documentation HEAD `2925f6e...` was still in progress when checked. The preceding application HEAD `dfedbf...` had Core cleanup tests passing and the previous HANDOFF reported the broader build's constituent gates passing before final workflow conclusion.

## Process findings

### PF-001 — Historical post-release development on published v1.0.19 branch
Severity: High
Status: MITIGATED

AMP-001 added a mandatory release/version reconciliation gate to the Dev Cycle. Subsequent evidence shows the worker correctly recognized `v1.0.19` as published/immutable, created/used forward branch `v1.0.20`, and repaired PROJECT_STATUS/HANDOFF release truth.

### PF-002 — Historical stale canonical stable/development state
Severity: High
Status: MITIGATED

Current `v1.0.20` HANDOFF and PROJECT_STATUS now agree with GitHub: `v1.0.19` is stable and `v1.0.20` is development.

### PF-003 — Historical red post-release C# CI
Severity: High
Status: CLOSED AS PROCESS FINDING

The worker did not mutate the published release. Subsequent `v1.0.20` work reports green C#/Core/build gates before continuing Stage-C migration. Current exact-head workflow was still running at this manager checkpoint, so no claim is made about its final conclusion.

### PF-004 — Manager schedule discrepancy
Severity: Medium
Status: USER-MANAGED

User stated timing was changed/saved separately. Do not alter unless new explicit user direction or a future proposal is approved.

### PF-005 — Coordinator bootstrap correctly deferred under no-race gate
Severity: Informational / positive

The first coordinator run encountered a moving Dev Cycle / active validation state and declined to create or rewrite the roadmap from a partial snapshot. This is the desired conservative behavior. Because `TECHNICAL_ROADMAP.md` and `COORDINATOR_LOG.md` do not yet exist on v1.0.20, the next completed Coordinator run should initialize them once it can review a stable completed worker state.

## Successful practices worth preserving

- Dev Cycle repaired branch/release discipline instead of rewriting published history.
- HANDOFF is specific about exact next work and retains failed CI/regression attempts.
- Stage-C work is converging vertically: Core identity/persistence → cleanup/export → normal editing-state migration, instead of broad feature accumulation.
- Cleanup regression caught an actual lineage-design defect; the worker fixed the root contract and preserved the failed run as evidence.
- User-observed viewport/storage issues remain pending target-machine verification rather than being falsely resolved.
- Coordinator respected the no-race gate on its first invocation.
- Daily Report prompt remains reporting-only and now has explicit Coordinator/Manager summary sections.

## Proposal registry

### AMP-001 — Harden Dev Cycle release/branch reconciliation
Status: **APPROVED / APPLIED — HELPED**

Evidence of outcome:
- GitHub latest release remains `v1.0.19`.
- Dev work now occurs on forward branch `v1.0.20`.
- HANDOFF/PROJECT_STATUS explicitly identify `v1.0.19` as immutable stable and `v1.0.20` as current development.
- No evidence in this review of published-release history being rewritten.

Verification conclusion: HELPED. Continue watching future release transitions (`v1.0.20` → `v1.0.21`) to ensure the behavior remains stable.

### Manager coordination/no-race rules
Status: **USER-DIRECTED / APPLIED — HELPED SO FAR**

Observed outcome:
- Coordinator's first review deferred rather than competing with an active/moving Dev Cycle state.
- No automation mutation occurred during the active worker interval.

## Active proposals awaiting user approval

None.

## Approved but pending application

None.

## Review history

### 2026-09-10 — Initial manager audit on v1.0.19
- Found published-v1.0.19 / stale-HANDOFF contradiction and post-release work on the published semantic-version branch.
- Proposed AMP-001.

### 2026-09-10 — AMP-001 applied + no-race policy added
- User approved AMP-001.
- Dev prompt received mandatory release/version reconciliation.
- Manager received no-race / no-live-mutation rules.

### 2026-09-10 — First v1.0.20 manager review
- Reviewed current Coordinator, Dev Cycle, Daily Report, and Manager definitions.
- Verified stable hierarchy is explicit across prompts.
- Verified `v1.0.19` remains latest published release and `v1.0.20` is the forward development branch.
- Verified AMP-001 outcome as HELPED.
- Reviewed current HANDOFF / PROJECT_STATUS and Stage-C cleanup/export trajectory.
- Observed Coordinator's first manual run correctly defer under no-race conditions; roadmap/log not yet initialized, appropriately.
- Exact-head CI for documentation HEAD was still in progress; no final CI conclusion claimed.
- No new automation prompt/schedule proposal warranted.

## Next review focus

1. Verify the next completed Coordinator run initializes `TECHNICAL_ROADMAP.md` and `COORDINATOR_LOG.md` from a stable completed Dev Cycle state.
2. Check whether Coordinator priorities remain stable and evidence-based rather than oscillating.
3. Check Dev Cycle alignment with the Coordinator roadmap once it exists.
4. Confirm v1.0.20 editing-state migration continues the Stage-C critical path rather than broadening into unrelated legacy cleanup.
5. Confirm exact-head CI completes successfully before release-readiness claims.
6. Monitor next release transition to verify AMP-001 remains effective beyond this first branch change.
