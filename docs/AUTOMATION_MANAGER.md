# Miniscuplter Automation Manager

> Current process/automation state. Historical checkpoints live in `docs/automation-manager-log/` and Git history.

Last manager review: 2026-09-12 13:04 Europe/Budapest

## Role model
- User / PROJECT_CHARTER owns product intent and difficult-to-reverse decisions.
- Coordinator owns architecture, strategy, issue priority/critical path, release readiness/chunking/publication.
- Dev owns implementation, validation, current execution-state updates and release-candidate preparation; Dev never publishes.
- Automation Manager owns scheduler/process/GitHub operational reliability and bounded process-infrastructure repair.
- Daily Report is reporting-only.

## Active automations
- `Minisculpter Dev` — currently disabled by user/manual pause; hourly schedule preserved.
- `Minisculpter Coordination` — enabled, every 3 hours at :30.
- `Daily Minisculpter report` — enabled, daily 22:45 Europe/Budapest.
- `Minisculpter Automation` — enabled, twice daily.
- Legacy duplicate tasks remain disabled.

## Canonical current-state architecture
- `CURRENT_EXECUTION_STATE.md` — execution baton.
- `TECHNICAL_DIRECTION_STATE.md` — Coordinator strategy state.
- `ISSUE_STATE.md` — active/release-relevant/user-verification issue state.
- `CROSS_AGENT_CONTEXT.md` — material user/cross-agent/process evidence.
- `HANDOFF.md`, `TECHNICAL_ROADMAP.md`, `ISSUES.md` — legacy/read-only context.
- `PROJECT_STATUS.md` — secondary dashboard only.

## Current repository / release truth
- Stable: `v1.0.27` at `7d40d06cb4084403db3193ec77ab77b71baa24e2`.
- Writable: `v1.0.28`; no release freeze/publication.
- Integrated application checkpoint: `4b6ea637ca572b7e7983bfbba051635e21a5b40c`.
- A–E implementation queue complete; current state is integrated checkpoint / Coordinator review.
- MS-020, MS-031, MS-009, MS-026 and MS-027 are fixed in code and pending user verification.
- MS-029 and MS-030 remain verification-pending.

## Current process state
- Neutral-state migration removed HANDOFF, TECHNICAL_ROADMAP and monolithic ISSUES from the live write path.
- CURRENT_EXECUTION_STATE authority-header reconciliation succeeded at `ef074abe...`.
- ISSUE_STATE was created at `0a898202...` and carries MS-020 released-build evidence plus MS-031.
- Pause provenance update: Dev self-paused, the user manually resumed it, then the user manually paused it again. The present disabled state is therefore a confirmed user/manual pause.
- Scheduler timestamps are insufficient by themselves to determine historical pause origin when direct user provenance is available.

## Reliability outcomes
- AMP-006 global lease: HARMFUL / RETIRED.
- Connector-first routing: HELPED.
- Wait-before-defer: HELPED.
- Narrow Coordinator Dev-resume authority: HELPED and correctly refuses ambiguous/manual pauses.
- AMP-007B degraded mode: HELPED.
- AMP-007C/D/E neutral small-state persistence: HELPED materially.

## Verification focus
1. Confirm Coordinator/Dev can update CURRENT_EXECUTION_STATE, TECHNICAL_DIRECTION_STATE and ISSUE_STATE without recurring safety blocks.
2. Preserve manual-pause safety; do not auto-resume ambiguous Dev disablement.
3. Current A–E queue is complete; next engineering movement depends on Coordinator release review and subsequent state replenishment.
4. Keep PROJECT_STATUS secondary so stale dashboard wording cannot override authoritative state.
