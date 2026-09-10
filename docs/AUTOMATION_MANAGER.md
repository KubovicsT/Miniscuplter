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
- Current observation: initial substantive review completed successfully after the earlier deferred no-race run. It created a bounded Stage-C roadmap, preserved the selective-refactor direction, narrowed v1.0.20 scope, and separated release readiness from post-release target-machine acceptance.

### Miniscuplter Dev Cycle
- Status: enabled.
- Schedule: hourly on whole hours.
- Role: implementation worker executing Coordinator roadmap + HANDOFF.
- AMP-001 release/version reconciliation gate remains applied.
- Release authority remains with Dev Cycle, but releases are readiness-based, not time-based.
- Current observation: latest completed cycle followed Coordinator P0 directly, implemented authoritative mapped-object transforms plus one bounded sculpt/edit path, retained failed-attempt evidence, and prepared a coherent v1.0.20 release candidate instead of broadening scope.

### Miniscuplter Daily Report
- Status: enabled.
- Schedule: daily 20:00 Europe/Budapest.
- Role: reporting only; includes Coordinator and Automation Manager summaries.
- No completed report had run yet at this checkpoint.

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
- Exact branch HEAD reviewed: `08fcf8d710fd3b5100321028d6063c9b7da11c4d` (`Finalize v1.0.20 release handoff`).
- Release-candidate application commit: `bc3106d606b4450aa5cb9d4395b77a5d6e78f11a`.
- Acceptance-weighted completion: 58%.
- Coordinator roadmap and log are now present and coherent with HANDOFF.
- Exact-head `core-foundation` run `34503646237`: SUCCESS.
- Exact-head broader `build` run `34503646371`: SUCCESS.
- v1.0.20 is not yet published because the established release workflow is tag-triggered and the currently exposed GitHub connector actions do not include tag creation.

## Process findings

### PF-001 — Historical post-release development on published v1.0.19 branch
Severity: High
Status: MITIGATED

AMP-001 added a mandatory release/version reconciliation gate to the Dev Cycle. Subsequent evidence shows the worker correctly recognized `v1.0.19` as published/immutable, created/used forward branch `v1.0.20`, and repaired PROJECT_STATUS/HANDOFF release truth.

### PF-002 — Historical stale canonical stable/development state
Severity: High
Status: MITIGATED

Current `v1.0.20` HANDOFF and PROJECT_STATUS agree with Git/release truth.

### PF-003 — Historical red post-release C# CI
Severity: High
Status: CLOSED AS PROCESS FINDING

The worker did not mutate the published release. Subsequent `v1.0.20` work reached green Core and broader build gates.

### PF-004 — Manager schedule discrepancy
Severity: Medium
Status: USER-MANAGED

User stated timing was changed/saved separately. Do not alter unless new explicit user direction or a future proposal is approved.

### PF-005 — Coordinator no-race behavior
Severity: Informational / positive
Status: WORKING AS INTENDED

The first coordinator attempt deferred while Dev/CI state was still moving. The next clean review initialized the roadmap/log from a completed state and did not compete with implementation work.

### PF-006 — Release publication blocked by connector capability, not process failure
Severity: Medium operational constraint
Status: OPEN / TOOLING-LIMITED

Evidence:
- The Dev Cycle reached a coherent v1.0.20 release-candidate state and exact-head branch CI is green.
- The established release workflow is intentionally triggered from a semantic-version tag.
- Current GitHub connector discovery exposes branch/ref movement but no tag-creation or release-creation action.

Assessment:
This is not evidence of a bad Dev prompt or bad release architecture. Do not weaken the tag-gated release process or repurpose a branch as a fake tag. If the connector remains unchanged, user-side tag creation may be required to trigger the existing release pipeline.

## Successful practices worth preserving

- Dev Cycle repaired branch/release discipline instead of rewriting published history.
- Coordinator set a bounded milestone instead of reopening architecture globally.
- Dev Cycle followed Coordinator P0 directly: mapped transforms became Core-authoritative and exactly one bounded sculpt/edit path moved to immutable revision semantics.
- HANDOFF preserves failed release-audit and accidental backend-edit history rather than hiding it.
- Self-review caught an accidental `ai_backend/app.py` truncation before release and restored it before proceeding.
- v1.0.20 scope did not expand into full sculpt migration, full Job Broker work, Rig/Pose, kitbash, provider expansion, or UI rewrite.
- Exact-head Core and broader CI are green before release publication.
- User-observed viewport/storage issues remain pending real-machine verification rather than being falsely resolved.
- No-race behavior has worked for both Coordinator and Manager interactions so far.

## Proposal registry

### AMP-001 — Harden Dev Cycle release/branch reconciliation
Status: **APPROVED / APPLIED — HELPED**

Evidence of outcome:
- `v1.0.19` remains the published stable release.
- Development moved forward to `v1.0.20`.
- HANDOFF/PROJECT_STATUS track the correct release/development split.
- No evidence of published-release history being rewritten.
- Current v1.0.20 release-candidate work stayed on the forward branch and preserved semantic-version discipline.

Verification conclusion: HELPED. Continue watching the actual v1.0.20 → v1.0.21 transition after publication.

### Manager coordination/no-race rules
Status: **USER-DIRECTED / APPLIED — HELPED**

Observed outcome:
- Coordinator deferred a partial-state review once, then completed a later stable review.
- Manager avoided changing active worker tasks.
- No automation race has been observed in this checkpoint.

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
- Verified hierarchy across Coordinator, Dev Cycle, Daily Report and Manager.
- Classified AMP-001 as HELPED.
- Confirmed Coordinator's first manual run correctly deferred under no-race conditions.
- No new automation proposal warranted.

### 2026-09-10 — Coordinator/Dev alignment and release-candidate audit
- Verified Coordinator created `TECHNICAL_ROADMAP.md` and `COORDINATOR_LOG.md` from a stable completed Dev state.
- Coordinator preserved the selective-refactor direction, narrowed v1.0.20 to Stage-C state authority, and explicitly prevented unrelated architecture work from delaying the version.
- Dev Cycle followed that roadmap: authoritative transforms, one bounded immutable sculpt/edit path, regression coverage, release-version reconciliation and release-candidate preparation.
- Exact branch HEAD `08fcf8d...` passed Core and broader build CI.
- Release publication is now blocked only by the absence of a tag-creation action in the exposed connector, not by unfinished roadmap work or red CI.
- No prompt/schedule change is justified from current evidence.

## Next review focus

1. Observe whether v1.0.20 is tagged/released through the existing gated workflow without bypassing release safeguards.
2. Verify the post-release branch transition to v1.0.21; this is the next strong AMP-001 durability test.
3. Review the Daily Report after its first completed run for faithful Coordinator/Dev/Manager synthesis.
4. After v1.0.20 target-machine testing, check whether Coordinator reprioritizes from actual MS-009/MS-013/MS-018/MS-022 evidence rather than speculative work.
5. Continue watching Coordinator stability: preserve direction unless new evidence justifies change.
6. Continue watching Dev Cycle scope discipline after release, especially avoiding premature broad legacy migration before real acceptance evidence.
