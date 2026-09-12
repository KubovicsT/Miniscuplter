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
- `Minisculpter Dev` — currently disabled by explicit user/manual pause; hourly schedule preserved.
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
- Stable: `v1.0.28` at `6ed31b98fe426e0ca0184530a279abce43b7031f`.
- Writable: `v1.0.29`; no release freeze/publication.
- Current v1.0.29 objective: semantic-version bootstrap.
- Remaining identity surfaces: `ai_backend/app.py` APP_VERSION and `tools/release_audit.py` EXPECTED still identify as 1.0.28.
- Python/runtime validation is blocked at the semantic-version identity gate until those two surfaces can be safely changed.

## Current process state
- Neutral-state migration removed HANDOFF, TECHNICAL_ROADMAP and monolithic ISSUES from the live write path.
- CURRENT_EXECUTION_STATE authority-header reconciliation succeeded at `ef074abe...`.
- ISSUE_STATE was created at `0a898202...` and carries MS-020 released-build evidence plus MS-031.
- Latest Dev run reported two essential large-file version-identity writes safety-blocked on v1.0.29. A transient ranged-read replacement truncated `ai_backend/app.py`; Dev restored the exact original content immediately and the live backend file is intact.
- Connector inspection shows no safe targeted line-edit/patch operation for ordinary repository files; available write path is whole-file replacement. Repeating the blocked large-file write or using low-level Git APIs to bypass safety is not allowed.
- User clarified pause provenance: Dev self-paused, user resumed it, then user manually paused it again. The present disabled state is an explicit user pause.

## Reliability outcomes
- AMP-006 global lease: HARMFUL / RETIRED.
- Connector-first routing: HELPED.
- Wait-before-defer: HELPED.
- Narrow Coordinator Dev-resume authority: HELPED and correctly refuses ambiguous/manual pauses.
- AMP-007B degraded mode: HELPED.
- AMP-007C/D/E neutral small-state persistence: HELPED materially.

## Verification focus
1. Resolve or safely redesign the two large-file v1.0.29 identity surfaces without bypassing connector safety.
2. Preserve the current explicit user pause until the user resumes Dev.
3. Keep current neutral state files small and writable.
4. Future automation replies start with AUTOMATION ISSUES and include LAST RUN time in Europe/Budapest.
