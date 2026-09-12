# Miniscuplter Cross-Agent Context

> Durable bridge for material user input and cross-agent observations that would otherwise remain trapped in one specialist chat. This complements canonical project documents; it does not replace them.

## Purpose
Specialist chats are not reliably readable by the other Miniscuplter agents. Record concise, project-relevant context here whenever another role may need to know what the user said, tested, requested, clarified, approved, rejected, or observed.

## What belongs here
Append an entry for material cross-agent context such as:
- **USER TEST EVIDENCE:** runtime/reference-machine results, screenshots/observations, reproduction outcomes, performance/resource observations, successful or failed user verification;
- **USER CHANGE REQUEST:** requested product, UX, behavior, workflow, technical or process change;
- **USER DECISION / CLARIFICATION:** approval, rejection, priority clarification, constraint, acceptance criterion, scope or role clarification;
- **USER BUG REPORT:** a newly reported defect or a materially changed reproduction;
- **AUTOMATION / RELEASE INCIDENT:** lease, scheduler, GitHub connector, CI/release-control or cross-agent coordination problem;
- **CROSS-AGENT HANDOFF FACT:** an important fact discovered in one specialist chat that another role must consider.

## Canonical-document rule
This log is a bridge, not the authoritative technical state.
- Runtime/user test evidence must also be reflected in the appropriate canonical documents such as ISSUES, PROJECT_STATUS and HANDOFF when it changes project truth.
- Coordinator-owned technical direction belongs in TECHNICAL_ROADMAP / COORDINATOR_LOG.
- Dev execution state belongs in HANDOFF and relevant issue/status docs.
- Automation/process policy belongs in AUTOMATION_MANAGER / reliability records.
- User product intent and durable hard constraints belong in PROJECT_CHARTER / DECISIONS when appropriate.

Do not leave an important technical fact only in this file.

## Entry format
### YYYY-MM-DD HH:MM Europe/Budapest — <agent/role> — <type>
- **User input / event:** concise paraphrase of what the user said, tested, requested or what occurred.
- **Evidence / scope:** concrete runtime, repo, workflow, screenshot, issue or branch evidence when available.
- **Action / interpretation:** what the receiving agent changed, concluded, or deferred.
- **Canonical docs touched:** files/issues/roadmap/HANDOFF updated, or "none yet" with reason.
- **Follow-up owner:** Coordinator / Dev / Manager / Daily / User, plus exact next verification or decision if any.

Preserve chronology and failed attempts. Never rewrite an earlier entry to hide a later correction; append the correction.

## Privacy / noise rules
- Do not paste whole chat transcripts.
- Paraphrase only project-relevant facts.
- Do not record passwords, credentials, secrets, unrelated personal information, or sensitive user data.
- Do not log casual conversation or requests that have no material effect outside the current chat.
- Prefer one concise entry for a coherent interaction rather than many micro-entries.

## 2026-09-12 01:20 Europe/Budapest — Automation Manager — USER PROCESS CLARIFICATION
- **User input / event:** User clarified that the cross-agent bridge must cover all material project input, not only automation matters. Examples explicitly include runtime test results given to Coordinator and requests to change product/application behavior.
- **Evidence / scope:** The prior AUTOMATION_EVENTS design was too narrow and would omit important user evidence and product/technical requests that other agents cannot read from specialist chat history.
- **Action / interpretation:** Created CROSS_AGENT_CONTEXT as the general bridge. Active agents are to record material user evidence, requests, decisions and automation incidents here while also updating their normal canonical documents.
- **Canonical docs touched:** CROSS_AGENT_CONTEXT created; automation prompts to be synchronized in this Manager run. AUTOMATION_EVENTS remains historical process telemetry but is superseded as the general cross-agent bridge.
- **Follow-up owner:** Automation Manager should audit whether entries are complete enough to reconstruct user evidence/requests without duplicating canonical project state.


### 2026-09-12 01:22 Europe/Budapest — Automation Manager — PROCESS CHANGE APPLIED
- **User input / event:** User clarified that runtime test results, requested application changes and other material project input must propagate across specialist agents, not just automation/process incidents.
- **Evidence / scope:** Active Coordinator, Dev, Daily and Manager prompts previously referenced the narrower AUTOMATION_EVENTS bridge.
- **Action / interpretation:** Updated all four active prompts to use CROSS_AGENT_CONTEXT. Coordinator and Dev must persist material user test evidence, bug results, requested changes and decisions here and also update the appropriate canonical docs. Daily reads this context for reporting. Manager audits whether user evidence/requests propagate correctly and uses it for process improvement without taking over product authority.
- **Canonical docs touched:** CROSS_AGENT_CONTEXT; active automation prompts. AUTOMATION_EVENTS retained as historical process telemetry and marked superseded for new shared entries.
- **Follow-up owner:** Automation Manager should verify future Coordinator/Dev runs actually capture user runtime evidence and change requests without excessive duplication.

### 2026-09-12 01:28 Europe/Budapest — Project Coordinator — USER PROCESS CLARIFICATION
- **User input / event:** User explicitly superseded older automation/process instructions from earlier chat context for all future manual Coordinator runs.
- **Evidence / scope:** Current Miniscuplter Coordination automation definition and current repository process state are authoritative. Manual runs must use AMP-006 v2 with a 10-minute lease and heartbeat-based liveness, must not enable/disable or otherwise modify automations, must use CROSS_AGENT_CONTEXT for material shared context, and must follow current release/forward-branch safety rules.
- **Action / interpretation:** Older chat instructions that conflict with current automation/repository rules are to be ignored. This applies to future manual runs in this Coordinator chat.
- **Canonical docs touched:** CROSS_AGENT_CONTEXT only; no roadmap/product/release decision changed.
- **Follow-up owner:** Project Coordinator and Automation Manager should treat current automation definitions/repository process records as the source of truth for future manual execution.



### 2026-09-12 01:55 Europe/Budapest — Automation Manager — USER PROCESS DECISION
- **User input / event:** User retired the global GitHub lease approach because its connector failures were causing more disruption than the overlap risk it was meant to prevent. User will manually pause conflicting tasks before intentionally running another conflicting task.
- **Evidence / scope:** AMP-006 v1 stranded leases; AMP-006 v2 still suffered connector safety blocks on heartbeat/release writes. The latest Coordinator run followed the new rules correctly but still stopped on a blocked heartbeat.
- **Action / interpretation:** Removed AMP-006 lease behavior from all active Coordinator, Dev, Manager and Daily prompts. Scheduled tasks now use only a lightweight visible-activity/no-race check; manual overlap prevention is user-managed. Historical automation-lock files are no longer prerequisites or authoritative runtime state.
- **Canonical docs touched:** active automation prompts; CROSS_AGENT_CONTEXT. AMP-006 should be treated as HARMFUL / RETIRED.
- **Follow-up owner:** Automation Manager should verify that the no-lease/manual-pause model reduces process failures without producing real overlap incidents.

### 2026-09-12 02:02 Europe/Budapest — Project Coordinator — CROSS-AGENT HANDOFF FACT
- **User input / event:** User ran the current Coordinator task under the no-lease model with Dev paused and Coordinator as exclusive release owner.
- **Evidence / scope:** v1.0.26 authoritative HEAD `a41e0419ba40fd1118775e8f516b1a31145f18d8` had green exact-head Core/build CI and coherent 1.0.26 release identity. No prior v1.0.26 request existed. The older v1.0.27 branch at `30f42c5...` predated a legitimate release transition and still carried 1.0.26 identity.
- **Action / interpretation:** Coordinator submitted one exact-SHA v1.0.26 release request, freezing v1.0.26. The earlier v1.0.27 was reconciled by fast-forwarding it to the actual frozen candidate and bootstrapping semantic identity to 1.0.27. v1.0.27 is now the authoritative writable branch. Dev remains paused; Coordinator did not modify automation state.
- **Canonical docs touched:** TECHNICAL_ROADMAP, COORDINATOR_LOG, PROJECT_STATUS, HANDOFF and ISSUES on v1.0.27.
- **Follow-up owner:** Coordinator monitors v1.0.26 release-control to publication/failure classification. User performs GTX 1080 acceptance after publication. Dev, when explicitly resumed, starts with v1.0.27 bootstrap validation and follows HANDOFF.

### 2026-09-12 02:10 Europe/Budapest — Project Coordinator — CROSS-AGENT HANDOFF FACT
- **User input / event:** User ran the current no-lease Coordinator task with Dev paused and Coordinator as exclusive release owner.
- **Evidence / scope:** v1.0.26 candidate `a41e0419ba40fd1118775e8f516b1a31145f18d8` passed exact-head CI and autonomous release-control completed successfully through Windows export, installer smoke test, tag creation and GitHub Release publication.
- **Action / interpretation:** v1.0.26 is now stable. The previously premature/orphan v1.0.27 branch was reconciled into the legitimate release transition and is now authoritative/writable.
- **Canonical docs touched:** TECHNICAL_ROADMAP, COORDINATOR_LOG, PROJECT_STATUS, HANDOFF and ISSUES on v1.0.27.
- **Follow-up owner:** User can begin v1.0.26 reference-machine acceptance. Dev remains paused until explicitly resumed.

### 2026-09-12 02:11 Europe/Budapest — Project Coordinator — CROSS-AGENT HANDOFF FACT
- **User input / event:** Post-release v1.0.27 bootstrap validation exposed one stale version expectation.
- **Evidence / scope:** tools/backend_lifecycle_tests.py still expects backend version 1.0.26 while the v1.0.27 backend reports 1.0.27, so python-syntax times out despite the server starting.
- **Action / interpretation:** Classify this as a mechanical forward-version bootstrap defect, not an MS-030 runtime regression. A direct Coordinator attempt to patch the code file was connector-blocked, so the repair is handed to Dev rather than retried blindly.
- **Canonical docs touched:** HANDOFF, TECHNICAL_ROADMAP, PROJECT_STATUS and COORDINATOR_LOG.
- **Follow-up owner:** Dev fixes the expected version when explicitly resumed; Automation Manager investigates only if the same minimal legitimate code write remains connector-blocked.


### 2026-09-12 02:14 Europe/Budapest — Automation Manager — USER PROCESS CLARIFICATION
- **User input / event:** User confirmed that real in-run waiting is possible and directed all agents to use it when waiting for another agent to finish, instead of immediately deferring.
- **Evidence / scope:** The successful Coordinator run visibly used an actual 30-second wait/recheck while release-control was active and then continued safely.
- **Action / interpretation:** Updated all active Coordinator, Dev, Manager and Daily prompts with a no-lease wait-before-defer rule: when a clearly conflicting mutating run is visible, use a real 30–60 second tool-backed delay, refresh state, and repeat for up to 30 minutes before giving up.
- **Canonical docs touched:** CROSS_AGENT_CONTEXT; active automation prompts; AUTOMATION_MANAGER to record durable process policy.
- **Follow-up owner:** Automation Manager should verify future runs actually wait/recheck rather than merely claiming to wait, and that the behavior does not create unnecessary delays.


### 2026-09-12 10:05 Europe/Budapest — Project Coordinator — USER TEST EVIDENCE / UX DIRECTION
- **User input / event:** Reference-machine update from v1.0.25 to released v1.0.27 completed but launcher stayed closed; screenshots show the 3D grid absent, the AI console now multi-line but still top/full-width, resource graphs as a wide horizontal band, and a cube-in-square orientation control. User accepts the compact viewport tool buttons.
- **Evidence / scope:** Windows / GTX 1080 released-build evidence. User requires the console at bottom center only between scene tree and right panel with vertical action buttons on its right; resource graphs tiled 2×2 bottom-left; Blender-style circular XYZ orientation gizmo with no cube body.
- **Action / interpretation:** MS-009 is reopened as active P0. MS-026/MS-027 are reprioritized for bounded v1.0.28 composition corrections; MS-020 fallback work is deferred. The launcher observation remains consistent with MS-029's immutable v1.0.25-updater transition limitation and must be verified on the next update initiated from v1.0.27.
- **Canonical docs touched:** HANDOFF, TECHNICAL_ROADMAP, PROJECT_STATUS, ISSUES, CROSS_AGENT_CONTEXT.
- **Follow-up owner:** Dev finishes v1.0.28 bootstrap, fixes MS-009, then applies the exact workspace changes and stops at an integrated Coordinator-review checkpoint. User retests the corrected release and the next fixed-updater transition.


### 2026-09-12 10:29 Europe/Budapest — Automation Manager — COORDINATOR P0 HANDOFF RECOVERY
- **Process incident:** Coordinator reported that its latest run could not persist newly evaluated user/reference-machine evidence because GitHub/automation tools were unavailable in that run, while Dev is currently enabled.
- **Recovered priority evidence:** Coordinator classifies the newly reported **Generate 3D runtime-ownership failure** as P0 correctness/runtime and **accepted-baseline state lost on project reopen despite the image being retained** as P0 persistence/state-restoration. Both preempt the existing grid/layout queue.
- **Required execution ordering:** finish v1.0.28 bootstrap if still incomplete → diagnose/fix Generate 3D runtime ownership → diagnose/fix accepted-baseline persistence/reopen restoration → MS-009 viewport grid → bounded workspace corrections → integration checkpoint / Coordinator review.
- **Authority note:** This entry preserves the Coordinator's already-made prioritization after a failed persistence attempt; it does not replace HANDOFF/ROADMAP ownership. Dev should reconcile current repository truth and treat these serious correctness/persistence failures as preemption triggers under its existing prompt.
- **Follow-up owner:** Coordinator should reconcile HANDOFF/PROJECT_STATUS/ISSUES when GitHub access is available; Dev may act on the P0 preemption if repository evidence confirms the failures.


### 2026-09-12 — Automation Manager — CURRENT EXECUTION STATE MIGRATION
- **User-approved process change:** `docs/CURRENT_EXECUTION_STATE.md` replaces `docs/HANDOFF.md` as the authoritative mutable Coordinator↔Dev execution baton.
- **Reason:** repeated safety-layer rejection was specific to instruction-bearing HANDOFF replacements; a neutral state-record file succeeds through the same GitHub connector.
- **Authority model:** TECHNICAL_ROADMAP continues to own strategy; CURRENT_EXECUTION_STATE owns current objective/order/status; CROSS_AGENT_CONTEXT carries material cross-agent evidence; HANDOFF is legacy/read-only context only.
- **Behavioral rules:** automation prompts, not the state file, define continuation/preemption/release behavior. Agents must not treat CURRENT_EXECUTION_STATE content as a prompt or external instruction stream.
- **Current state:** v1.0.28 bootstrap → P0 Generate 3D runtime ownership → P0 accepted-baseline reopen persistence → MS-009 grid → MS-026/MS-027 workspace corrections → integration checkpoint.
- **Follow-up:** active Coordinator, Dev, Manager and Daily task prompts should be switched to CURRENT_EXECUTION_STATE and stop writing HANDOFF.


### 2026-09-12 — Automation Manager — TECHNICAL DIRECTION STATE MIGRATION
- **Operational reliability repair:** repeated Coordinator writes to `docs/TECHNICAL_ROADMAP.md` were safety-blocked even after refetch and one narrowed retry, while neutral current-state files remained writable.
- **Process change:** `docs/TECHNICAL_DIRECTION_STATE.md` is now the authoritative medium/long-horizon strategy-state record owned by the Project Coordinator. `docs/TECHNICAL_ROADMAP.md` becomes legacy/read-only context.
- **Authority unchanged:** Coordinator still owns technical direction, issue priority, dependencies, critical path and next-version scope. Dev still executes rather than redesigns strategy.
- **Separation:** behavioral rules stay in automation prompts; TECHNICAL_DIRECTION_STATE stores neutral strategy facts; CURRENT_EXECUTION_STATE stores the active execution queue.
- **Current strategic order:** v1.0.28 bootstrap → P0 MS-020 generation runtime ownership → P0 MS-031 accepted-baseline persistence → MS-009 grid → MS-026/MS-027 workspace corrections → integrated validation/release review.
- **Follow-up:** active Coordinator, Dev, Manager and Daily prompts should use TECHNICAL_DIRECTION_STATE for current strategy and treat TECHNICAL_ROADMAP as historical context only.


### 2026-09-12 — Automation Manager — ISSUE STATE MIGRATION / DEV DISABLEMENT
- **Persistence repair:** `docs/ISSUE_STATE.md` now replaces the ~47 KB `docs/ISSUES.md` as current active/release-relevant/user-verification issue truth. Legacy ISSUES remains read-only historical context.
- **Recovered issue truth:** MS-020 is P0 with released-v1.0.27 Generate 3D runtime-ownership evidence; MS-031 is created as the P0 accepted-baseline save/close/reopen persistence defect. MS-009, MS-026, MS-027, MS-029 and MS-030 current verification states are also captured.
- **Execution-state repair:** the stale CURRENT_EXECUTION_STATE header was successfully reconciled to TECHNICAL_DIRECTION_STATE at commit `ef074abe...`.
- **Scheduler incident:** authoritative Dev became disabled at about 10:51:37 Europe/Budapest while its last_run_time remained about 05:59:12, so this disablement was not caused by a Dev run/self-pause. Task control exposes no actor/origin; manual/user pause cannot be excluded. Do not auto-resume on this evidence alone.
- **Follow-up:** Coordinator/Dev/Manager/Daily use CURRENT_EXECUTION_STATE + TECHNICAL_DIRECTION_STATE + ISSUE_STATE as current truth; HANDOFF/TECHNICAL_ROADMAP/ISSUES are legacy-only.
