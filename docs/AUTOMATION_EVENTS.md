# Miniscuplter Automation Events

> Shared cross-agent process log for automation incidents and user-directed automation/process requests. This exists because specialist chat transcripts are not reliably visible across agents. Keep entries concise, factual, and repository/process-related.

## What belongs here
Append an entry when any active Miniscuplter agent encounters or receives:
- automation lease / scheduler / manual-run overlap problems;
- GitHub connector read/write or safety-block failures;
- release-control / CI orchestration failures that are process infrastructure rather than application defects;
- stale or contradictory automation/process metadata;
- a user instruction that materially changes automation behavior, role boundaries, reliability expectations, scheduling intent, or operational process;
- a workaround, failed approach, or repair that the Automation Manager should evaluate later.

Do **not** copy whole chat transcripts. Paraphrase only the project-relevant operational fact. Do not record passwords, credentials, secrets, unrelated personal information, or sensitive user data.

## Entry format
### YYYY-MM-DD HH:MM Europe/Budapest — <agent/role> — <type>
- **Trigger / user direction:** concise factual summary.
- **Observed evidence:** exact task/repo/workflow evidence when available.
- **Action taken:** what changed or what was attempted.
- **Result:** success / blocked / partial / needs verification.
- **Manager follow-up:** what the Automation Manager should inspect, improve, or close.

Entries should be appended chronologically and preserve failed attempts.

## 2026-09-12 01:15 Europe/Budapest — Automation Manager — USER PROCESS REQUEST
- **Trigger / user direction:** User requested a durable repo-level bridge because specialist agent chat history is not reliably available cross-agent. Agents should document automation-related incidents and relevant user process requests so the Automation Manager can inspect them later and improve the system.
- **Observed evidence:** Recent lease, GitHub connector and release-control investigations required reconstructing failures from repository/scheduler state because exact specialist-chat history was unavailable.
- **Action taken:** Created this shared event log and prepared prompt updates so active agents record material automation/process incidents here.
- **Result:** Applied; agent prompt synchronization pending in the same Manager run.
- **Manager follow-up:** Read this file every Manager audit, reconcile unresolved entries, transfer durable conclusions to AUTOMATION_MANAGER / reliability records, and mark outcomes in later entries.

### 2026-09-12 01:18 Europe/Budapest — Automation Manager — PROCESS CHANGE APPLIED
- **Trigger / user direction:** User requested that specialist agents persist automation incidents and relevant user process requests because cross-agent chat history is not reliably available.
- **Observed evidence:** Coordinator, Dev, Daily and Manager prompts previously relied mainly on their own chat plus repository state; exact specialist-chat dialogue could be unavailable to Manager.
- **Action taken:** Added AUTOMATION_EVENTS logging rules to Coordinator, Dev, Daily and Manager prompts. Manager must read/reconcile the log every completed audit; other agents append concise entries only for material automation/process incidents or user process directions.
- **Result:** Applied. Schedules and role ownership unchanged; Dev remains intentionally paused.
- **Manager follow-up:** Evaluate whether the log improves incident reconstruction without becoming noisy or duplicating ordinary HANDOFF/ROADMAP content.
