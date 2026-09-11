# Miniscuplter Automation Manager

> Durable supervisory memory for scheduled-task/process reviews. Read this file completely at the start of every manager run. Preserve failed/rejected history.

Last manager review/update: 2026-09-11 22:32 Europe/Budapest

## Stable hierarchy

1. User / PROJECT_CHARTER — fundamental product intent, scope, hard constraints, difficult-to-reverse product decisions.
2. Project Coordinator — technical roadmap, architecture direction, milestone decomposition, issue priority, dependency ordering, next-version scope, release readiness, and release publication.
3. Dev Cycle — implementation worker, validation, execution-state docs, HANDOFF, and preparation of exact release candidates for Coordinator review.
4. Automation Manager — process/agent supervision and automation-change proposals, subject to user approval.
5. Daily Report — reporting only.

## Current task registry

### Miniscuplter Coordination
- Task ID: `6aa30b540870819184a402ac09186176`.
- Status: enabled.
- Schedule: 00:30, 03:30, 06:30, 09:30, 12:30, 15:30, 18:30, 21:30 Europe/Budapest.
- Latest visible run at this checkpoint: 2026-09-10 23:30 Europe/Budapest.
- Role: technical director / roadmap owner / exclusive release-readiness and release-publication owner.
- Current observation: the latest Coordinator review reconciled the roadmap to v1.0.22/v1.0.23, preserved the Stage-C acceptance priority, resolved the MS-028 planning-state inconsistency, and explicitly kept v1.0.23 NOT READY FOR COORDINATOR RELEASE REVIEW.

### Miniscuplter Dev
- Task ID: `6aa30b48fcd88191b7cb88cddfaa8b49`.
- Status: enabled.
- Schedule: hourly on whole hours.
- Latest visible run at this checkpoint: 2026-09-10 23:10 Europe/Budapest.
- Role: implementation worker; prepares and validates exact release candidates but does not publish them.
- Current observation: HANDOFF cleanly states the new candidate-handoff contract and current v1.0.23 state is NOT READY FOR COORDINATOR RELEASE REVIEW.

### Daily Minisculpter report
- Task ID: `6aa30a0e2a448191ba7b6263645b94f7`.
- Status: enabled.
- Schedule: daily 22:45 Europe/Budapest.
- Latest visible run at this checkpoint: 2026-09-10 23:27 Europe/Budapest.
- Role: reporting only; executive-summary format, with specialist-chat pointers for detail.
- Current observation: the new concise-report prompt is active. Timing still sits only 15 minutes after the 22:00 Dev start, so the no-race gate may often have to wait for Dev to finish.

### Minisculpter Automation
- Task ID: `6aa30b550bfc8191a5b885b413ebc2f4`.
- Status: enabled; excluded from worker scoring.
- Schedule: 05:30 and 17:30 Europe/Budapest.
- Latest visible scheduled run: none yet since the new task was created.
- Role: process/automation supervision only.
- Current observation: the active task already uses the dedicated Manager charter and the Coordinator-owned release model. The older disabled Manager automation is no longer the live task and should not drive current proposals.

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
- Current development branch: `v1.0.23`.
- Exact current branch HEAD reviewed: `5d6132ee0d8e13c40ff264261298aa65ee2d2206` (`coord: resolve validated MS-028 regression`).
- Exact-head `core-foundation` run `34532537710`: SUCCESS.
- Exact-head broader `build` run `34532537725`: SUCCESS.
- No `v1.0.23` release-control request exists.
- HANDOFF / PROJECT_STATUS / TECHNICAL_ROADMAP now agree that v1.0.23 is development-only and NOT READY FOR COORDINATOR RELEASE REVIEW.
- Acceptance-weighted completion remains **57%**.
- No-race checkpoint passed: the latest Coordinator repository write and exact-head CI completed before this Manager review; no active v1.0.23 release freeze exists.

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

### PF-006 — Historical release-publication connector limitation
Severity: Medium
Status: CLOSED / SUPERSEDED

Earlier connector limitations no longer represent the live release path. The repository's autonomous exact-SHA release-control workflow successfully published later releases including v1.0.22. Release publication is now Coordinator-owned and should continue through that verified mechanism.

### PF-007 — TECHNICAL_ROADMAP main body lagged current release/acceptance state
Severity: Medium
Status: CLOSED

The latest Coordinator review reconciled the roadmap main body to stable v1.0.22, development v1.0.23, 57% acceptance-weighted completion, Stage-C reference-machine acceptance as P0, bounded MS-027 fallback sequencing, and the new Coordinator-owned release model. No prompt change is needed for this finding.

### PF-008 — Scheduled Manager prompt lagged the dedicated-chat operating charter
Severity: Medium
Status: CLOSED / SUPERSEDED

The user created the active `Minisculpter Automation` task with the dedicated Manager charter and later synchronized it to the Coordinator-owned release model. The older Manager task is disabled. AMP-002 is therefore obsolete and requires no user action.

### PF-009 — Exact cross-chat response text unavailable at this checkpoint
Severity: Informational
Status: TOOL/CONTEXT-LIMITED

The Manager attempted to recover current Dev Cycle, Coordinator and Daily Report user-facing outputs from project context, but no exact response text was surfaced. Per policy, absence is not treated as evidence that no response occurred. Repository/scheduler evidence is used instead, and response wording/verbosity claims are left unscored for this checkpoint.

### PF-010 — Coordinator release-review latency after ownership transfer
Severity: Medium
Status: MITIGATION APPLIED — AMP-003

The Coordinator is now the exclusive release owner but still runs only every six hours. Once Dev marks a candidate READY FOR COORDINATOR RELEASE REVIEW and stops candidate-invalidating work, a candidate can sit blocked for nearly six hours before the next release decision. This is a process-design consequence of the ownership transfer, not a current release failure.

### PF-011 — Daily Report timing is close to hourly Dev start
Severity: Low/Medium
Status: MITIGATION APPLIED — AMP-004

The executive Daily Report runs at 22:15 while Dev starts at 22:00. Because Dev cycles can legitimately exceed 15 minutes, the report's no-race gate may repeatedly wait or risk reporting an in-progress day state. The shorter executive format makes a later :45 slot practical.

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

### AMP-002 — Synchronize the scheduled Automation Manager with the new dedicated-chat charter
Status: **SUPERSEDED / NO ACTION REQUIRED**

The active `Minisculpter Automation` task was created with the dedicated Manager charter and has since been synchronized to the Coordinator-owned release model. The previous target task is disabled. No user approval is needed and no further mutation should be made for AMP-002.

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

### AMP-003 — Increase Coordinator cadence for release-owner responsiveness
Status: **APPROVED / APPLIED — INCONCLUSIVE PENDING VERIFICATION**

- **Target task:** Minisculpter Coordination
- **Task ID:** `6aa30b540870819184a402ac09186176`
- **Evidence:** Coordinator is now the exclusive release owner; Dev must stop candidate-invalidating work once a candidate is marked READY FOR COORDINATOR RELEASE REVIEW. Current Coordinator cadence is every six hours.
- **Root cause:** release ownership changed, but the strategic-review schedule was left at its prior six-hour cadence.
- **Exact proposed change:** change Coordinator schedule from 00:30/06:30/12:30/18:30 to every three hours at :30 (00:30/03:30/06:30/09:30/12:30/15:30/18:30/21:30). Preserve prompt, title and enablement.
- **Expected benefit:** cuts worst-case release-candidate decision latency roughly in half and reduces time a ready candidate can block Dev.
- **Possible downside:** doubles Coordinator runs and may increase no-race waits/strategic-review overhead.
- **Verification plan:** over the next two release-candidate handoffs, measure candidate-ready → Coordinator decision latency and watch for priority churn or repeated scheduler collisions.

### AMP-004 — Move Daily Report from :15 to :45
Status: **APPROVED / APPLIED — INCONCLUSIVE PENDING VERIFICATION**

- **Target task:** Daily Minisculpter report
- **Task ID:** `6aa30a0e2a448191ba7b6263645b94f7`
- **Evidence:** Daily Report is scheduled 22:15, only 15 minutes after the hourly 22:00 Dev start, while its own no-race rule requires completed state.
- **Root cause:** report timing predates the current whole-hour Dev cadence and the executive-report simplification.
- **Exact proposed change:** move the report from 22:15 to 22:45 Europe/Budapest. Preserve prompt, title and enablement.
- **Expected benefit:** gives the 22:00 Dev run substantially more time to finish while retaining a same-evening executive report.
- **Possible downside:** report arrives 30 minutes later.
- **Verification plan:** review the next two reports for fewer in-progress/deferred snapshots and confirm the report still completes before the next meaningful coordination boundary.

## Approved but pending application

None.

## Review history

### 2026-09-10 — AMP-003 and AMP-004 approved/applied
- User explicitly approved both proposals.
- AMP-003: changed `Minisculpter Coordination` from every six hours to every three hours at :30: 00:30, 03:30, 06:30, 09:30, 12:30, 15:30, 18:30, 21:30 Europe/Budapest.
- AMP-004: changed `Daily Minisculpter report` from 22:15 to 22:45 Europe/Budapest.
- Prompts, titles and enablement were preserved.
- Verification remains pending: measure release-candidate-ready → Coordinator-decision latency, watch for Coordinator churn/races, and check whether Daily Report avoids in-progress snapshots.


### 2026-09-10 23:39 — Manual Manager review after release-ownership transition
- No-race gate passed after the 23:30 Coordinator write and exact-head CI completed.
- Verified stable v1.0.22, development v1.0.23, HEAD `5d6132ee...`, exact-head Core/build green, and no v1.0.23 release request.
- Verified ROADMAP, STATUS and HANDOFF now consistently encode Coordinator-owned release readiness/publication and Dev-only candidate preparation.
- Verified Coordinator explicitly decided v1.0.23 is NOT READY FOR COORDINATOR RELEASE REVIEW; no premature release request was created.
- Closed PF-006/PF-007/PF-008 as obsolete or corrected.
- Closed AMP-002 as SUPERSEDED because the new active Manager task already carries the dedicated charter.
- Proposed AMP-003 to reduce release-owner decision latency and AMP-004 to reduce Daily Report/Dev timing overlap.
- No automation was changed.


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

1. Verify the next Dev cycle continues to obey the new release boundary and does not publish.
2. When the first READY FOR COORDINATOR RELEASE REVIEW candidate appears, measure handoff-to-decision latency and validate the freeze.
3. Verify Coordinator either publishes or returns a concrete blocking reason without duplicate authority.
4. Review the next two executive Daily Reports for brevity and snapshot stability.
5. If AMP-003 or AMP-004 is approved, apply only the approved schedule delta after re-reading the live target task and confirming it is idle.
6. Continue to prioritize v1.0.22 reference-machine acceptance evidence over UI fallback work.

---

## User-directed process override — continuous development between releases (2026-09-10)

This section supersedes older Manager wording that expected Dev to stop after marking READY FOR COORDINATOR RELEASE REVIEW.

Current process contract:
- Coordinator remains exclusive release owner and controls release chunk size.
- Dev records useful release-worthy checkpoint SHAs/evidence but continues roadmap work afterward.
- A checkpoint is not a freeze and does not block Dev.
- Only an actual Coordinator release request freezes that semantic-version source branch.
- Coordinator must create/use the next forward semantic-version branch from the frozen SHA before or as release-control starts, so Dev can continue while publication runs.
- Manager should flag unnecessary Dev idle time, mutation of a frozen source branch, missing forward-branch creation, excessively tiny releases, or excessive delay that causes version scope to become incoherent.

The active Dev, Coordinator, Automation Manager, and Daily Report prompts were updated to this model. No schedules changed as part of this policy update.

AMP-003 remains applied at the every-three-hours Coordinator cadence. Its original anti-wait rationale is reduced by this new model, but the tighter cadence remains useful for roadmap supervision and release-chunk decisions; evaluate it on those outcomes rather than candidate-blocking time.


---

## AMP-005 — Long-horizon Coordinator engineering decomposition (2026-09-11)

Status: **APPROVED / APPLIED — INCONCLUSIVE PENDING VERIFICATION**

User intent:
- increase useful engineering throughput between Coordinator reviews;
- do **not** manufacture smaller per-Dev-cycle tasks or optimize for run count;
- preserve Coordinator as end-to-end technical/architectural owner of the whole application;
- let Dev execute substantial authorized objectives continuously without waiting after each completed slice.

Applied automation changes:
- **Minisculpter Coordination** now explicitly owns end-to-end conceptual engineering understanding of the whole application, periodic broad architecture review, system decomposition, and a rolling long-horizon Dev execution plan.
- Coordinator should normally maintain roughly **3–6 hours** of safely pre-authorized engineering work, usually **3–5 substantial ordered objectives** when dependencies allow.
- Objectives must be meaningful engineering outcomes, not artificial hourly/per-run tickets. One objective may span multiple Dev runs.
- Each queued objective should define outcome, architectural boundaries/constraints, dependencies, acceptance/stop condition, preemption conditions, and whether Dev may automatically proceed.
- Coordinator must plan farther ahead than Dev executes while preserving Dev's ordinary implementation freedom.
- **Minisculpter Dev** now treats a scheduled run ending as a continuation point, not a task boundary; it continues CURRENT OBJECTIVE across runs and automatically advances through valid NEXT OBJECTIVES.
- Dev uses **COORDINATOR REVIEW REQUESTED** only when the queue is exhausted/invalidated, strategic authority is required, a user decision is required, or release/freeze state leaves no safe work.
- **Automation Manager** now audits whole-system Coordinator quality, execution-plan depth, artificial fragmentation, queue starvation, Dev idle time, and architectural coherence.
- **Daily Report** reports engineering trajectory/current objective and queue health only when material; it does not dump the full execution queue.
- No schedules, titles or enablement states changed.

HANDOFF operating contract expected on subsequent Coordinator/Dev runs:
- CURRENT OBJECTIVE
- ordered NEXT OBJECTIVES
- meaningful outcome and acceptance/stop conditions
- important constraints/dependencies/preemption conditions
- auto-proceed permission
- current writable branch/release state/latest useful checkpoint
- COORDINATOR REVIEW REQUESTED only under the explicit escalation conditions above

Verification plan:
1. Compare the next several Coordinator intervals with prior behavior for Dev runs that perform no useful implementation solely because direction ran out.
2. Measure whether meaningful engineering outcomes completed per Coordinator interval increase without increasing artificial task fragmentation.
3. Confirm substantial objectives can span multiple Dev runs and Dev advances automatically when acceptance conditions are met.
4. Confirm Coordinator continues broad architecture/system review rather than degrading into a ticket generator.
5. Confirm the queue is shortened when dependencies genuinely prevent safe look-ahead rather than filled with speculative work.
6. Classify AMP-005 as HELPED / NEUTRAL / HARMFUL / INCONCLUSIVE from those concrete outcomes.

### Review-history entry — 2026-09-11 22:32 Europe/Budapest
- User explicitly approved AMP-005 after clarifying that the goal is more work between Coordinator cycles, not more/smaller Dev-cycle tasks.
- User further clarified Coordinator's job includes end-to-end engineering/architecture ownership of the whole application, review of completed work, system decomposition and technical direction.
- Updated the four active automation prompts accordingly.
- Schedules remain unchanged: Dev hourly at :00; Coordinator every three hours at :30; Manager 05:30/17:30; Daily Report 22:45 Europe/Budapest.


### Manager review — 2026-09-11 23:05 Europe/Budapest
- No-race gate passed; Dev and Coordinator were not actively writing when reviewed.
- Stable remains v1.0.25; v1.0.26 remains writable with no release request/freeze.
- AMP-005 shows strong early positive evidence: Coordinator produced four substantial ordered objectives with acceptance and auto-proceed rules, and Dev advanced through multiple authorized objectives instead of waiting after one slice.
- Latest reviewed exact head: de040f6248cdab55b5f638c79cdd1eeef910fd98; exact-head core-foundation and build workflows were green.
- Keep AMP-005 formally INCONCLUSIVE pending several more Coordinator intervals.
- Watch HANDOFF freshness after rapid multi-objective Dev progress; the next pass should reconcile objective completion cleanly.
- No new automation proposal warranted.


---

## AMP-006 — Global manual/scheduled run lease (2026-09-11)

Status: **USER-DIRECTED / APPLIED — PENDING VERIFICATION**

User intent:
- manual runs of Dev, Coordinator, Manager or Daily Report must be safe without pausing schedules;
- scheduled invocations must not overlap a manually triggered run;
- cross-role races must also be prevented, not only duplicate same-role runs.

Applied design:
- created dedicated branch `automation-locks`;
- added `.automation-locks/global.json` as the single global compare-and-swap lease;
- all four active automations must acquire and verify this lease before substantive work;
- a live unexpired lease causes later manual/scheduled invocations to WAIT for their turn for up to 30 minutes without mutating project state;
- lease duration is 90 minutes with renewal near 60 minutes;
- crashed runs recover by lease expiry; no run may clear another live token;
- normal completion releases the lease before final user-facing output whenever possible;
- lock-branch commits are process metadata only and do not count as project/release activity;
- existing role-specific no-race checks remain as a second layer.

Implementation:
- updated active Miniscuplter Dev, Coordination, Automation Manager and Daily Report prompts;
- schedules/titles/enablement unchanged;
- live CAS acquire/release test on `automation-locks/.automation-locks/global.json` succeeded.

Verification plan:
1. Manually start a task near its scheduled boundary and confirm the later invocation waits, then proceeds after lease release.
2. Confirm a manual run of one role also blocks a different Miniscuplter role from mutating concurrently.
3. Confirm waiting runs leave project/HANDOFF/roadmap/release state untouched until they acquire the lease.
4. Confirm normal runs release the lease and crashed runs recover after expiry.
5. Watch whether serialization causes material throughput loss; adjust only with user approval if needed.


### AMP-006 amendment — wait instead of skip (2026-09-11)

User correction:
- contention must not immediately skip/drop a run;
- a contending manual or scheduled invocation should wait for the current Miniscuplter run to release the global lease, then execute;
- maximum contention wait is 30 minutes from that invocation's original wait-start time.

Updated behavior:
- all four active tasks now re-check the global lease at low frequency (target roughly every 1–2 minutes when the execution environment permits waiting);
- when the lease frees within 30 minutes, the waiting invocation atomically acquires it and proceeds normally;
- CAS conflicts return the invocation to the same wait loop without resetting the original 30-minute clock;
- only an actual 30-minute contention timeout may end without running the task;
- project/release/planning/report state remains untouched while waiting;
- schedules, titles and enablement remain unchanged.

Verification focus:
1. Manual-vs-scheduled overlap should serialize rather than skip.
2. Cross-role overlap should serialize the same way.
3. Successful waiters should proceed after lease release.
4. 30-minute timeout should be rare and clearly reported as LEASE WAIT TIMEOUT.
5. Observe whether the task execution environment can sustain the requested low-frequency re-checking without premature run termination; the scheduler does not expose a native blocking mutex/sleep primitive.


Diagnostic branch append.
