# Miniscuplter Automation Manager

> Durable supervisory memory for scheduled-task/process reviews. Read this file completely at the start of every manager run. Do not erase failed/rejected history.

Last manager review: 2026-09-10

## Current worker-task registry

### Miniscuplter Dev Cycle
- Status: enabled
- Schedule observed: hourly
- Role: autonomous senior engineering/product engineering implementation loop
- Last run observed by scheduler: 2026-09-10T13:30:32.781294Z
- Key contract: inspect repo/docs/CI, continue HANDOFF priority, validate strongly, maintain canonical docs, use immutable releases and move to next version branch after publication.

### Miniscuplter Daily Report
- Status: enabled
- Schedule observed: daily 20:00 Europe/Budapest
- Role: concise evidence-based project status report
- Last run observed: none yet

### Miniscuplter Automation Manager
- Status: enabled; excluded from worker scoring
- Intended user-approved schedule: 05:30 and 17:30 Europe/Budapest daily
- Scheduler state observed during this review: 12-hour interval aligned to minute 00 rather than the requested fixed 05:30/17:30 times. Do not silently alter this during a manager audit; surface it to the user.

## Management principles

1. Judge acceptance evidence and reduced risk, not commit volume.
2. Actual Git/release/CI state outranks stale canonical docs.
3. Published releases are immutable; after publication, application development belongs on the next version branch.
4. User-observed GUI/GPU bugs are not resolved by compilation alone.
5. Prefer one reliable Stage-C end-to-end workflow over disconnected feature/provider accumulation.
6. Prompt changes require explicit user approval and must be evaluated afterward as HELPED / NEUTRAL / HARMFUL / INCONCLUSIVE.

## Current checkpoint

- Repository: `KubovicsT/Miniscuplter`
- Branch reviewed: `v1.0.19`
- Branch HEAD observed: `e89fcbb0ad63bdad46ae7c8cb24f4b85e67d47bb` (`Update handoff after provider readiness preflight`)
- Latest application code commit named by HANDOFF: `2b5f848c51f5be6ead21a9e5e6bfa3e243a2a078`
- Actual latest GitHub release: `v1.0.19`, published 2026-09-10 at release target `52f3b95fb6addc0f9f1e7123b75068da4ef1513c`
- Canonical docs currently incorrectly state latest stable is `v1.0.18` and current development branch is `v1.0.19`.
- Current branch therefore contains commits after the published v1.0.19 release target.
- Latest observed `build` workflow for branch HEAD failed in the C# `dotnet` job; Python/core logic and packaging succeeded; full Windows release and publish jobs were skipped.
- Current acceptance-weighted completion documented as 56%; no evidence found in this audit to change it.

## Process findings

### PF-001 — Post-release development continued on the published version branch
Severity: High

Evidence:
- GitHub reports `v1.0.19` as the latest published release.
- The `v1.0.19` branch HEAD is newer than the release target and contains provider-readiness work plus documentation commits.
- `PROJECT_STATUS.md` and `HANDOFF.md` still identify `v1.0.18` as latest stable and `v1.0.19` as the active development branch.
- The accepted release discipline says published versions are immutable and subsequent application changes should move to the next version branch.

Assessment:
The Dev Cycle prompt already contains the correct high-level rule, but its bootstrap/current-branch language and reliance on stale HANDOFF allowed the worker to continue on `v1.0.19` after that version had already been published. This creates ambiguity about what code belongs to the released version, weakens reproducibility, and can cause release/version metadata churn or accidental mutation attempts.

### PF-002 — Canonical project state was not reconciled immediately after release
Severity: High

Evidence:
- Actual latest release is `v1.0.19`.
- `PROJECT_STATUS.md` and `HANDOFF.md` still say latest stable is `v1.0.18`.
- Their stated source-of-truth hierarchy says actual repository/release state wins and contradictions should be reconciled.

Assessment:
This stale handoff directly contributed to PF-001. A worker starting from zero memory is being told contradictory truths by GitHub and the canonical docs.

### PF-003 — Post-release branch HEAD CI is red
Severity: High

Evidence:
- Latest branch-head build workflow failed in the C# `dotnet` job.
- Python/core tests and packaging passed; full Windows release and publish jobs were skipped.
- HANDOFF explicitly says the next run must inspect CI and fix any regression before continuing.

Assessment:
The worker correctly documented CI verification as the immediate next action, but branch/version discipline must be corrected at the same time so the fix is not developed as unversioned post-release `v1.0.19` application work.

### PF-004 — Manager schedule state does not match the user's approved fixed times
Severity: Medium

Evidence:
- User explicitly requested 05:30 and 17:30 daily.
- Current scheduler state observed by this manager is a 12-hour interval aligned to minute 00.

Assessment:
This is a scheduler configuration drift, not a repo engineering failure. It should be corrected outside this audit run because this run is operating under a no-scheduler-change instruction.

## Successful practices worth preserving

- The Dev Cycle maintains a clear HANDOFF with an exact next action rather than declaring success prematurely.
- It correctly keeps MS-009/MS-013 and similar user-observed runtime issues open pending real-machine verification.
- Recent provider-readiness work is aligned with MD-011/MD-012 and MS-022 rather than adding arbitrary new providers.
- Current completion remains at 56% because the new work is largely code/static evidence, consistent with acceptance-weighted accounting.
- Python/core logic, geometry regressions, release audit, and packaging were exercised in CI; the failed C# gate prevented downstream release jobs, which is appropriate gate behavior.

## Active proposals awaiting user approval

### AMP-001 — Harden Dev Cycle release/branch reconciliation
Status: PROPOSED - AWAITING USER APPROVAL
Target: `Miniscuplter Dev Cycle`

Observed evidence:
The automation continued application work on `v1.0.19` after `v1.0.19` had already been published, while canonical docs still called v1.0.18 stable.

Root cause:
The worker prompt tells the agent to resolve branch/release state, but does not make the mismatch an explicit mandatory stop condition before application edits. It also contains bootstrap wording centered on `v1.0.19`, which can reinforce stale state.

Exact proposed prompt change:
Add the following immediately after the initial repository/release/branch inspection paragraph:

> **MANDATORY VERSION/RELEASE RECONCILIATION BEFORE EDITING:** Compare the actual latest published GitHub release/tag and its target commit with the branch named by HANDOFF. If the supposed development branch is already the published release version, or if that branch contains application commits after the published release target, do not continue ordinary application development on that version branch. First reconcile the canonical docs to the actual release state and create/use the next semantic-version development branch from the intended post-release HEAD (or from the published target if the post-release commits must be preserved separately). Preserve already-published release/tag contents unchanged. If branch ancestry/state is ambiguous, avoid destructive ref changes; document the discrepancy and use a new forward-only branch rather than rewriting history. Only after version/release state is coherent may normal implementation continue.

Also replace the sentence `The current development branch at bootstrap is v1.0.19...` with:

> The development branch changes over time. Never hard-code a bootstrap version as current truth; resolve it from actual Git/release state plus HANDOFF, and repair HANDOFF when those disagree.

Expected benefit:
Prevents future work from accumulating on an already-published version branch and makes zero-memory runs robust to stale HANDOFF data.

Possible downside:
A run may spend part of its engineering window on branch/docs reconciliation instead of feature work, but this cost is small compared with version/release ambiguity.

Verification plan:
After application, inspect the next 2–4 Dev Cycle runs. Success means the worker moves forward onto the correct next-version branch, canonical docs match the actual latest release, no application commits are added to an already-published version branch, and release tags remain untouched.

## Prior proposal outcomes

None yet; this is the first manager checkpoint.

## Review history

### 2026-09-10 — Initial manager audit
- Reviewed enabled Miniscuplter Dev Cycle and Daily Report definitions plus manager configuration.
- Read PROJECT_CHARTER, PROJECT_STATUS, ISSUES, DECISIONS, HANDOFF, and REFACTOR_PLAN.
- Inspected actual `v1.0.19` branch HEAD, latest release, recent branch history, and latest branch-head CI.
- Found release/docs/branch contradiction (PF-001/PF-002), red C# CI on post-release branch HEAD (PF-003), and manager scheduling drift (PF-004).
- Proposed AMP-001; no worker automation was modified because user approval is required.

## Next review focus

1. Check whether AMP-001 was approved/rejected/modified.
2. Verify whether canonical stable/development version state was repaired.
3. Inspect exact C# build failure and whether it was fixed on a correct forward development branch.
4. Assess whether provider qualification continues to serve MS-018 rather than becoming provider-framework work detached from the Stage-C thin slice.
5. Re-check manager fixed-time schedule configuration.