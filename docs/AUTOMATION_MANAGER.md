# Miniscuplter Automation Manager

> Current process/automation state. Historical checkpoints live in `docs/automation-manager-log/` and legacy reliability/history files.

Last manager review: 2026-09-12

## Role model
- User / PROJECT_CHARTER owns product intent and difficult-to-reverse product decisions.
- Coordinator owns architecture, strategy, priority, release readiness and publication.
- Dev owns implementation, validation, CURRENT_EXECUTION_STATE updates and release-candidate preparation; Dev never publishes.
- Automation Manager owns process/scheduler/GitHub operational reliability and bounded process-infrastructure repair.
- Daily Report is reporting-only.

## Active automations
- `Minisculpter Dev` — enabled, hourly on whole hours.
- `Minisculpter Coordination` — enabled, every 3 hours at :30.
- `Daily Minisculpter report` — enabled, daily 22:45 Europe/Budapest.
- `Minisculpter Automation` — enabled, twice daily.
- Older duplicate Dev/Coordinator/Daily/Manager tasks remain disabled.

## Authoritative persistence model
- `docs/CURRENT_EXECUTION_STATE.md` — current Coordinator↔Dev execution baton.
- `docs/TECHNICAL_DIRECTION_STATE.md` — Coordinator-owned medium/long-horizon strategy state.
- `docs/CROSS_AGENT_CONTEXT.md` — material user/cross-agent/process evidence bridge.
- `docs/HANDOFF.md` and `docs/TECHNICAL_ROADMAP.md` — legacy/read-only context only.
- Behavioral continuation/release/safety rules live in automation prompts, not repository state files.

## Current repository / release truth
- Stable: `v1.0.27` at `7d40d06cb4084403db3193ec77ab77b71baa24e2`.
- Writable: `v1.0.28`; no release freeze/publication active.
- Current execution order: bootstrap → P0 MS-020 generation runtime ownership → P0 MS-031 accepted-baseline persistence → MS-009 grid → MS-026/MS-027 workspace corrections → integrated validation/release review.
- `CURRENT_EXECUTION_STATE.md` was successfully reconciled by Coordinator after migration.
- `TECHNICAL_DIRECTION_STATE.md` creation and subsequent in-place replacement both succeeded; the old instruction-heavy TECHNICAL_ROADMAP path is no longer required for current strategy persistence.

## Reliability policy / outcomes
- **AMP-006:** HARMFUL / RETIRED.
- **Wait-before-defer:** HELPED.
- **GitHub connector-first routing:** HELPED.
- **Narrow Coordinator Dev-resume authority:** HELPED.
- **AMP-007B degraded mode:** HELPED.
- **AMP-007C safety-block avoidance:** HELPED by preventing retry loops and identifying instruction-heavy control files as the rejected mutation shape.
- **AMP-007D persistence discipline:** HELPED.
- **AMP-007E small-file architecture:** HELPED.
- **CURRENT_EXECUTION_STATE migration:** HELPED; Coordinator successfully updates the new baton.
- **TECHNICAL_DIRECTION_STATE migration:** VERIFIED WRITABLE; creation and ordinary replacement both succeeded.

## Open process risks
- Instruction-heavy repository documents can trigger connector safety rejection even when the same factual content is legitimate project state.
- Neutral factual state files should remain neutral; do not let them drift back into agent-instruction documents.
- PROJECT_STATUS / ISSUES may temporarily lag during transitions; owning roles should reconcile only when their distinct canonical truth materially changes.

## Verification focus
1. Dev reads CURRENT_EXECUTION_STATE + TECHNICAL_DIRECTION_STATE and advances the P0 queue without consulting legacy HANDOFF/ROADMAP as current truth.
2. Coordinator successfully performs future strategy updates through TECHNICAL_DIRECTION_STATE.
3. Daily reports from current state files rather than stale legacy planning files.
4. No active automation regresses into instruction-heavy repository control documents.

Historical small-file adoption record: `docs/automation-manager-log/2026-09-12-small-file-adoption.md`.
