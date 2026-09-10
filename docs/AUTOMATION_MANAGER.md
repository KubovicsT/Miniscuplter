# Miniscuplter Automation Manager

> Durable supervisory memory for scheduled-task/process reviews. Read this file completely at the start of every manager run. Preserve failed/rejected history.

Last manager review/update: 2026-09-10 21:40 Europe/Budapest

## Stable hierarchy

1. User / PROJECT_CHARTER — fundamental product intent, scope, hard constraints, difficult-to-reverse product decisions.
2. Project Coordinator — technical roadmap, architecture direction, milestone decomposition, issue priority, dependency ordering, next-version scope, release readiness, and release publication.
3. Dev Cycle — implementation worker, validation, execution-state docs, HANDOFF, and preparation of exact release candidates for Coordinator review.
4. Automation Manager — process/agent supervision and automation-change proposals, subject to user approval.
5. Daily Report — reporting only.

## Current task registry

### Miniscuplter Project Coordinator
- Task ID: `6aa2d384de1c8191b1c8dc27c369e1d4`.
- Status: enabled.
- Schedule: 04:30, 10:30, 16:30, 22:30 Europe/Budapest.
- Last completed run visible to scheduler at this checkpoint: 2026-09-10 19:41:05 Europe/Budapest.
- Role: technical director / lead architect / roadmap owner.
- Durable outputs: `docs/TECHNICAL_ROADMAP.md`, `docs/COORDINATOR_LOG.md`.
- Prompt materially includes a Dev no-race gate, roadmap ownership, architecture/priority authority, and a prohibition on routine implementation/automation mutation.
- Current observation: strategic direction is evidence-driven and stable, including the v1.0.21/v1.0.22 acceptance corrections and the bounded MS-027 fallback. However, the main body/header of `TECHNICAL_ROADMAP.md` is stale relative to its own later addenda, HANDOFF, PROJECT_STATUS and actual v1.0.22/v1.0.23 state. This must be reconciled by the Coordinator rather than silently worked around long-term.

### Miniscuplter Dev Cycle
- Task ID: `6aa283cdd0648191be0ef0590591c59a`.
- Status: enabled.
- Schedule: hourly on whole hours.
- Last completed run visible to scheduler at this checkpoint: 2026-09-10 21:37:02 Europe/Budapest.
- Role: implementation worker executing Coordinator roadmap + HANDOFF.
- AMP-001 release/version reconciliation gate remains applied.
- Current observation: the latest completed cycle was aligned with the Coordinator fallback. With released-v1.0.22 acceptance externally blocked on target-machine testing, it implemented exactly one bounded MS-027 UI-preference slice, added focused ownership/storage regression guards, kept the acceptance path as P0, and did not submit a v1.0.23 release merely because CI was green.

### Miniscuplter Daily Report
- Task ID: `6aa283d6f8708191896518d6a62eec15`.
- Status: enabled.
- Schedule: daily 20:00 Europe/Budapest.
- Last completed run visible to scheduler at this checkpoint: 2026-09-10 20:00:48 Europe/Budapest.
- Role: reporting only; includes Coordinator, Dev Cycle and Automation Manager synthesis.
- Current observation: exact user-facing output from the separate report chat was not surfaced to this Manager review, so response accuracy is not invented. The run occurred before the 21:23 local publication of v1.0.22 and before the later v1.0.23 UI work, so omission of those later events would not itself indicate report staleness.

### Miniscuplter Automation Manager
- Task ID: `6aa2b5d15dbc81919417034c3741b419`.
- Status: enabled; excluded from worker scoring.
- Schedule definition: `RRULE:FREQ=HOURLY;INTERVAL=12;BYMINUTE=0`.
- Last completed scheduled run visible at this checkpoint: 2026-09-10 18:47:00 Europe/Budapest.
- Role: process/automation supervision only.
- Current scheduled prompt is an earlier Manager charter. The dedicated-chat charter supplied by the user on 2026-09-10 adds material cross-chat user-facing-response review, richer no-race/release-freeze checks, explicit MS-027 blocked-work context, and more detailed durable-memory requirements. This mismatch is tracked as AMP-002 and is not being silently applied.

## User-directed release ownership change — 2026-09-10

The user explicitly moved release ownership from Dev Cycle to Project Coordinator.

Effective operating model:
- Coordinator owns release readiness decisions, autonomous release-control request creation/update, release-freeze supervision, release-workflow outcome handling, publication verification, and post-release forward-version reconciliation.
- Dev Cycle owns implementation, validation, and preparation of an exact candidate SHA with evidence/risks in HANDOFF/PROJECT_STATUS. Dev must not create release requests, tags, or GitHub Releases unless the user changes this ownership again.
- Automation Manager audits whether the handoff between Dev candidate preparation and Coordinator release action is efficient and safe.
- Daily Report reports release decisions/publication as Coordinator outcomes.

The active scheduled prompts for Minisculpter Coordination, Minisculpter Dev, Minisculpter Automation, and Daily Minisculpter report were synchronized to this user-directed model on 2026-09-10. Schedules and enablement were preserved.

Verification focus:
1. Dev stops cleanly at READY FOR COORDINATOR RELEASE REVIEW.
2. Coordinator independently verifies candidate scope/gates before release.
3. Coordinator initiates and monitors release-control without racing Dev.
4. Frozen release branches remain immutable.
5. After publication, Coordinator reconciles the next forward semantic-version branch before Dev resumes ordinary application work.

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
- Latest published stable: `v1.0.22`.
- Stable release target: `c1ba2d01517cc6bca5a6e6cde3b1e85f853dd0d7`.
- v1.0.22 published: 2026-09-10 21:23:03 Europe/Budapest through the autonomous verified release path.
- Current development branch: `v1.0.23`.
- Exact branch HEAD reviewed: `39c0ed92ec4a0b2f64f13e3d0d2cb9ba176eae83` (`docs: finalize v1.0.23 UI handoff validation`).
- Latest bounded application/test candidate before documentation-only commits: `74ec73a14645071bb2742fb68f778bedd656aabd`.
- Acceptance-weighted completion: **57%**.
- Exact-head `core-foundation` run `34521310884`: SUCCESS.
- Exact-head broader `build` run `34521310943`: SUCCESS.
- No v1.0.23 release request is recorded in HANDOFF; v1.0.23 remains development-only.
- No-race checkpoint: the latest Dev Cycle and its exact-head CI had completed before this Manager write; the next Coordinator run is scheduled for 22:30 local.
- Canonical execution state in HANDOFF/PROJECT_STATUS is coherent with actual release/branch truth.
- Coordinator planning state is only partially coherent: latest addenda and HANDOFF reflect v1.0.22/v1.0.23 and MS-027 fallback, but the main roadmap header/current-objective/priority sections still describe v1.0.21/MS-009 as the immediate live state.

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

### PF-007 — TECHNICAL_ROADMAP main body lags current release/acceptance state
Severity: Medium
Status: OPEN — COORDINATOR DOCUMENT-COHERENCE ISSUE

Evidence:
- Actual stable is v1.0.22 and development is v1.0.23.
- HANDOFF/PROJECT_STATUS correctly say released-v1.0.22 acceptance is awaiting user/reference-machine verification and permit bounded MS-027 fallback work.
- ROADMAP addenda contain the later MS-023/MS-024/MS-025 and MS-027 decisions, but the main header/current technical objective/ordered priorities still describe v1.0.21 and MS-009 as the immediate live release objective.

Assessment:
This has not caused current Dev drift because HANDOFF and the later roadmap addenda are explicit, but it weakens the roadmap's role as one coherent medium/long-horizon source. The Coordinator should reconcile the main body on its next completed review. No automation prompt change is proposed yet because the existing Coordinator prompt already requires a current coherent roadmap; first observe whether the next run corrects it.

### PF-008 — Scheduled Manager prompt lags the new dedicated-chat operating charter
Severity: Medium
Status: OPEN — AMP-002 PROPOSED

Evidence:
- The live scheduled Manager task still uses the earlier prompt.
- The user supplied a materially expanded Manager charter on 2026-09-10 covering cross-chat user-facing-response review, stronger no-race/release-freeze evidence, MS-027 blocked-work interpretation, richer task-registry requirements and explicit response-quality auditing.

Assessment:
The current chat follows the new charter, but future scheduled Manager runs are not guaranteed to. This is a prompt-state mismatch, not a worker failure.

### PF-009 — Exact cross-chat response text unavailable at this checkpoint
Severity: Informational
Status: TOOL/CONTEXT-LIMITED

The Manager attempted to recover current Dev Cycle, Coordinator and Daily Report user-facing outputs from project context, but no exact response text was surfaced. Per policy, absence is not treated as evidence that no response occurred. Repository/scheduler evidence is used instead, and response wording/verbosity claims are left unscored for this checkpoint.

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
- Published release branches have remained immutable through later forward fixes.
- v1.0.22 was published at exact candidate `c1ba2d01517cc6bca5a6e6cde3b1e85f853dd0d7` through the autonomous exact-SHA release workflow.
- Development then moved forward to v1.0.23 from the published v1.0.22 target.
- Current v1.0.23 application work is confined to the forward branch; there is no evidence of published-release history being rewritten.
- The worker did not publish v1.0.23 merely because branch CI became green.

Verification conclusion: **HELPED**. The release/version reconciliation behavior is now surviving multiple release transitions, not only the original v1.0.19 → v1.0.20 correction.

### Manager coordination/no-race rules
Status: **USER-DIRECTED / APPLIED — HELPED**

Observed outcome:
- Coordinator deferred a partial-state review once, then completed a later stable review.
- Manager avoided changing active worker tasks.
- No automation race has been observed in this checkpoint.

## Active proposals awaiting user approval

### AMP-002 — Synchronize the scheduled Automation Manager with the new dedicated-chat charter
Status: **PROPOSED - AWAITING USER APPROVAL**

- **Target task:** Miniscuplter Automation Manager
- **Task ID:** `6aa2b5d15dbc81919417034c3741b419`
- **Evidence:** the live scheduled task still contains the earlier Manager prompt, while the user supplied a materially expanded dedicated-chat charter on 2026-09-10.
- **Root cause:** moving the Automation Manager into its own Project chat and expanding cross-chat/process responsibilities did not automatically rewrite the existing scheduled task definition.
- **Exact proposed change:** replace only the scheduled Manager task prompt with the user's new dedicated-chat Automation Manager charter from 2026-09-10. Preserve task ID, enablement state and current 12-hour schedule. Do not modify Coordinator, Dev Cycle or Daily Report automations.
- **Expected benefit:** scheduled reviews use the same hierarchy/no-race rules as this chat; review user-facing cross-chat responses when available; correctly treat MS-027 fallback work; maintain the richer task registry and prior-change verification; avoid the scheduled Manager silently operating under older rules.
- **Possible downside:** longer prompt and additional repository/context checks may increase scheduled-run tool usage and runtime.
- **Verification plan:** over the next two completed Manager runs, verify that task IDs/schedules are recorded, exact cross-chat outputs are reviewed only when actually surfaced, missing chat output is not invented, MS-027 is not falsely labeled drift, previous AMP outcomes are classified, and no unapproved automation mutation occurs.

## Approved but pending application

None.

## Review history

### 2026-09-10 — Release ownership moved to Coordinator
- User explicitly reassigned release ownership from Dev Cycle to Project Coordinator.
- Updated active Dev prompt so it prepares/validates exact release candidates and stops before publication.
- Updated active Coordinator prompt so it owns release readiness, release-control initiation/freeze supervision, workflow outcome handling, publication verification and post-release version transition.
- Updated Daily Report and Automation Manager prompts to evaluate/report against the new ownership model.
- No schedules or enablement states were changed.
- Next Manager reviews should verify the Dev → Coordinator release handoff works without duplicate authority or ready-candidate delay.

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

### 2026-09-10 21:40 — v1.0.22 release / v1.0.23 UI-fallback manager checkpoint
- Scheduler registry refreshed with current task IDs, definitions, schedules and last-run times.
- No-race gate passed: latest Dev run and exact-head CI completed before audit; Coordinator was idle.
- Verified v1.0.22 published at exact candidate `c1ba2d01517cc6bca5a6e6cde3b1e85f853dd0d7` and forward development moved to v1.0.23.
- Verified Dev implemented exactly one bounded Coordinator-approved MS-027 preference/layout slice while acceptance was blocked on user testing; no v1.0.23 release request was submitted merely for green CI.
- Classified AMP-001 as HELPED with stronger multi-release evidence.
- Found PF-007: ROADMAP main-body/header staleness despite current addenda/HANDOFF; no Coordinator prompt change proposed yet.
- Cross-chat exact response text was not surfaced, so user-facing response wording was not invented or scored.
- Proposed AMP-002 to synchronize the scheduled Manager prompt with the new dedicated-chat charter; no automation was modified.

### 2026-09-10 — Daily Report executive-summary prompt change
- User explicitly requested that the Daily Report be much shorter and more executive-focused.
- Applied directly to enabled task `Daily Minisculpter report` (task ID `6aa30a0e2a448191ba7b6263645b94f7`).
- Schedule unchanged: daily at 22:15 Europe/Budapest.
- Prompt now targets roughly 200–350 words, at most four compact sections, no per-workstream matrix/commit list/detailed CI dump, and points to Dev Cycle / Project Coordinator / Automation Manager dialogues for deeper detail.
- Verification plan: review the next two completed Daily Report outputs for brevity, executive usefulness, correct escalation of blockers/user actions, and whether detail is appropriately delegated to the specialist dialogues.

## Next review focus

1. Check whether the next Coordinator run reconciles the stale main body/header of TECHNICAL_ROADMAP with v1.0.22/v1.0.23 and the current acceptance/MS-027 state.
2. Review released-v1.0.22 target-machine evidence as soon as it exists; verify Coordinator reprioritizes from that evidence and Dev immediately preempts UI fallback for any reproduced high-severity blocker.
3. Verify the next Dev Cycle takes only the next bounded MS-027 slice if acceptance remains externally blocked, and does not let UI work become a monolithic rewrite.
4. Continue AMP-001 durability monitoring across v1.0.23 release/no-release decisions.
5. Re-attempt cross-chat user-facing-response review when exact Dev/Coordinator/Daily Report outputs are surfaced; do not infer wording from repository state.
6. If AMP-002 is approved, apply only the scheduled Manager prompt delta in an idle window and verify its effect on subsequent reviews.
7. Watch the Coordinator :30 / hourly Dev schedule for repeated real race/defer behavior before proposing a schedule change; one handled overlap is not yet enough evidence for another automation mutation.
