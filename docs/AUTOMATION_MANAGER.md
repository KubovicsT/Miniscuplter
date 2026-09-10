# Miniscuplter Automation Manager

> Durable supervisory memory for scheduled-task/process reviews. Read this file completely at the start of every manager run. Do not erase failed/rejected history.

Last manager review/update: 2026-09-10

## Current worker-task registry

### Miniscuplter Dev Cycle
- Automation ID: `6aa283cdd0648191be0ef0590591c59a`
- Status: enabled
- Schedule: whole-hour cadence (`06:00`, `07:00`, `08:00`, etc. according to the user's configured schedule/time zone)
- Role: autonomous senior engineering/product engineering implementation loop
- Key contract: inspect repo/docs/CI, continue HANDOFF priority, validate strongly, maintain canonical docs, use immutable releases and move to the next version branch after publication.
- 2026-09-10 prompt change: AMP-001 applied. The worker now has a mandatory release/version reconciliation gate before application edits and no longer hard-codes `v1.0.19` as bootstrap truth.

### Miniscuplter Daily Report
- Automation ID: `6aa283d6f8708191896518d6a62eec15`
- Status: enabled
- Schedule: daily 20:00 Europe/Budapest
- Role: concise evidence-based project status report

### Miniscuplter Automation Manager
- Automation ID: `6aa2b5d15dbc81919417034c3741b419`
- Status: enabled; excluded from worker scoring
- User reports its timing has been corrected/saved separately.
- 2026-09-10 prompt change: coordination/no-race rules added. The manager must review completed Dev Cycle output rather than racing an active worker, and approved worker-task mutations must wait until the affected worker is verifiably idle.

## Management principles

1. Judge acceptance evidence and reduced risk, not commit volume.
2. Actual Git/release/CI state outranks stale canonical docs.
3. Published releases are immutable; after publication, application development belongs on the next version branch.
4. User-observed GUI/GPU bugs are not resolved by compilation alone.
5. Prefer one reliable Stage-C end-to-end workflow over disconnected feature/provider accumulation.
6. Prompt changes require explicit user approval and must be evaluated afterward as HELPED / NEUTRAL / HARMFUL / INCONCLUSIVE.
7. **No-race rule:** do not start a substantive manager review against a Dev Cycle that is still running. Review its completed repository/docs/CI state. If completion cannot be reliably established, defer rather than judge partial work.
8. **No live mutation rule:** never modify a worker automation while that worker is running, even when the change has already been approved. Mark it `APPROVED - PENDING APPLICATION` until the worker is verifiably idle.
9. Immediately before applying an approved worker change, re-read the current automation definition and apply only the approved delta so intervening user/worker changes are preserved.

## Current checkpoint

- Repository: `KubovicsT/Miniscuplter`
- Branch last audited: `v1.0.19`
- Branch HEAD at initial audit: `e89fcbb0ad63bdad46ae7c8cb24f4b85e67d47bb` (`Update handoff after provider readiness preflight`), followed by manager documentation commits.
- Latest application code commit named by HANDOFF at initial audit: `2b5f848c51f5be6ead21a9e5e6bfa3e243a2a078`.
- Actual latest GitHub release at initial audit: `v1.0.19`, published 2026-09-10 at release target `52f3b95fb6addc0f9f1e7123b75068da4ef1513c`.
- Canonical docs at initial audit incorrectly stated latest stable was `v1.0.18` and current development branch was `v1.0.19`.
- The `v1.0.19` branch therefore contained commits after the published v1.0.19 release target.
- Latest branch-head `build` workflow observed during the initial audit failed in the C# `dotnet` job; Python/core logic and packaging succeeded; full Windows release and publish jobs were skipped.
- Acceptance-weighted completion was 56%; no evidence from the manager audit justified changing it.

## Process findings

### PF-001 — Post-release development continued on the published version branch
Severity: High

Evidence:
- GitHub reported `v1.0.19` as the latest published release.
- The `v1.0.19` branch HEAD was newer than the release target and contained provider-readiness work plus documentation commits.
- `PROJECT_STATUS.md` and `HANDOFF.md` still identified `v1.0.18` as latest stable and `v1.0.19` as active development.
- Accepted release discipline says published versions are immutable and subsequent application changes should move to the next version branch.

Assessment:
The old Dev Cycle prompt contained the correct high-level rule but did not make release/branch mismatch a mandatory pre-edit stop condition. Stale HANDOFF state therefore allowed post-release work to continue on the already-published semantic-version branch.

Current management status:
- Mitigation AMP-001 approved and applied 2026-09-10.
- Outcome: `INCONCLUSIVE` until subsequent Dev Cycle runs prove that the worker reconciles v1.0.19 correctly and moves development forward without rewriting published history.

### PF-002 — Canonical project state was not reconciled immediately after release
Severity: High

Evidence:
- Actual latest release was `v1.0.19`.
- `PROJECT_STATUS.md` and `HANDOFF.md` still said latest stable was `v1.0.18`.
- Their source-of-truth hierarchy says actual repository/release state wins and contradictions should be reconciled.

Assessment:
This stale handoff directly contributed to PF-001. AMP-001 now instructs the worker to repair this contradiction before ordinary application development.

### PF-003 — Post-release branch HEAD CI was red
Severity: High

Evidence:
- Latest branch-head build workflow failed in the C# `dotnet` job.
- Python/core tests and packaging passed; full Windows release and publish jobs were skipped.
- HANDOFF explicitly said the next run must inspect CI and fix any regression before continuing.

Assessment:
The worker correctly documented CI verification as the immediate next action. The next manager audit should confirm the failure was handled on a correct forward development branch after release-state reconciliation.

### PF-004 — Manager schedule discrepancy observed during initial audit
Severity: Medium

Initial evidence:
- Earlier scheduler state did not appear to match the user's requested fixed manager times.

Update 2026-09-10:
- User reports the timing has now been changed and saved separately.
- No schedule mutation was performed as part of AMP-001 or the no-race prompt update.
- Treat this finding as user-corrected unless future scheduler evidence shows a material mismatch again.

## Successful practices worth preserving

- The Dev Cycle maintains a clear HANDOFF with an exact next action rather than declaring success prematurely.
- It keeps MS-009/MS-013 and similar user-observed runtime issues open pending real-machine verification.
- Recent provider-readiness work aligns with MD-011/MD-012 and MS-022 rather than adding arbitrary new providers.
- Completion remained at 56% because the work was primarily code/static evidence, consistent with acceptance-weighted accounting.
- Python/core logic, geometry regressions, release audit, and packaging were exercised in CI; the failed C# gate prevented downstream release jobs, which is appropriate gate behavior.
- Dev Cycle scheduling is now aligned to whole hours, giving a clearer coordination boundary for management review.

## Proposal registry

### AMP-001 — Harden Dev Cycle release/branch reconciliation
Status: **APPROVED / APPLIED — OUTCOME INCONCLUSIVE**
Target: `Miniscuplter Dev Cycle` (`6aa283cdd0648191be0ef0590591c59a`)
Approved by user: 2026-09-10
Applied: 2026-09-10, while the Dev Cycle was not in an active scheduled run window.

Observed evidence:
The automation continued application work on `v1.0.19` after `v1.0.19` had already been published, while canonical docs still called v1.0.18 stable.

Root cause:
The old worker prompt told the agent to resolve branch/release state but did not make mismatch an explicit mandatory stop condition before application edits. It also contained bootstrap wording centered on `v1.0.19`.

Applied change:
1. Replaced the hard-coded bootstrap sentence with:

> The development branch changes over time. Never hard-code a bootstrap version as current truth; resolve it from actual Git/release state plus HANDOFF, and repair HANDOFF when those disagree.

2. Added a mandatory pre-edit gate requiring the worker to compare the actual latest published release/tag and target commit against HANDOFF/current branch. If the supposed development branch is already published or contains post-release application work, the worker must reconcile docs/release state and move to a new forward semantic-version branch before normal implementation. Published release/tag contents must remain unchanged; ambiguous ancestry must not be resolved with destructive history rewriting.

Expected benefit:
Prevents future work from accumulating on an already-published version branch and makes zero-memory runs robust to stale HANDOFF data.

Possible downside:
A run may spend part of its engineering window on branch/docs reconciliation instead of feature work, but that cost is small compared with release/version ambiguity.

Verification plan:
Inspect the next 2–4 completed Dev Cycle runs. Success means:
- the worker recognizes `v1.0.19` as already published;
- actual release state and canonical docs become consistent;
- development continues on the correct next-version branch;
- no new application work is incorrectly treated as mutable published v1.0.19 content;
- release tags/history remain untouched;
- the red C# regression is handled in the correct forward branch.

### Manager coordination policy change — no proposal ID required
Status: **USER-DIRECTED / APPLIED**
Applied: 2026-09-10

The user directly requested two manager-behavior changes:
1. The manager should wait for the Dev Cycle to finish before beginning its own substantive review.
2. Even an approved automation change must never be applied while the affected Dev Cycle is running.

The manager prompt now includes a `COORDINATION / NO-RACE GATE` and requires `APPROVED - PENDING APPLICATION` state when approval arrives during an active worker run. It must re-read the latest automation definition immediately before applying any approved delta.

## Prior proposal outcomes

- AMP-001: `INCONCLUSIVE` immediately after application; verification requires later completed Dev Cycle runs.

## Review history

### 2026-09-10 — Initial manager audit
- Reviewed enabled Miniscuplter Dev Cycle and Daily Report definitions plus manager configuration.
- Read PROJECT_CHARTER, PROJECT_STATUS, ISSUES, DECISIONS, HANDOFF, and REFACTOR_PLAN.
- Inspected actual `v1.0.19` branch HEAD, latest release, recent branch history, and latest branch-head CI.
- Found release/docs/branch contradiction (PF-001/PF-002), red C# CI on post-release branch HEAD (PF-003), and manager scheduling discrepancy (PF-004).
- Proposed AMP-001; no worker automation was modified before approval.

### 2026-09-10 — AMP-001 approval/application and coordination hardening
- User approved AMP-001.
- Confirmed Dev Cycle was not in its active scheduled whole-hour run window before mutation.
- Applied the release/version reconciliation gate to the Dev Cycle prompt and removed hard-coded v1.0.19 bootstrap truth.
- User separately reported scheduler timing had been corrected/saved and that Dev Cycle now runs at whole hours.
- Added manager no-race rules: review completed Dev Cycle output; do not race active engineering work; defer if completion cannot be established; never mutate an active worker; re-read current task definition before applying an approved delta.
- AMP-001 outcome remains INCONCLUSIVE pending subsequent worker runs.

## Next review focus

1. Verify AMP-001 against the next 2–4 completed Dev Cycle runs and classify as HELPED / NEUTRAL / HARMFUL / INCONCLUSIVE.
2. Confirm the worker reconciles actual stable `v1.0.19` state and moves ongoing engineering to the proper forward semantic-version branch.
3. Confirm canonical PROJECT_STATUS/HANDOFF state is repaired to match the actual release.
4. Inspect the C# build failure and whether it is fixed without mutating published release history.
5. Assess whether provider qualification continues to serve MS-018 rather than becoming provider-framework work detached from the Stage-C thin slice.
6. Enforce the no-race gate before every future manager audit or approved scheduler mutation.
