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
