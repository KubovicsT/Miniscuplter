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
- Previously blocked backend, release-audit and backend-lifecycle identity surfaces now identify as 1.0.29 through validated patch-control commits.
- Exact-head build validation at `f83d9fd0e17b83c345a52a79f057bdd5632bf304` is green for .NET, Python/runtime, backend lifecycle, packaging/hash and installer-definition compilation.

## Current process state
- Neutral-state migration removed HANDOFF, TECHNICAL_ROADMAP and monolithic ISSUES from the live write path.
- CURRENT_EXECUTION_STATE authority-header reconciliation succeeded at `ef074abe...`.
- ISSUE_STATE was created at `0a898202...` and carries MS-020 released-build evidence plus MS-031.
- `patch-control` now provides exact-HEAD/exact-blob validated one-file unified-diff application for large existing text files on writable semantic-version branches, followed by explicit exact-head `build.yml` dispatch.
- Live acceptance repaired the two original blocked identity writes plus the stale backend-lifecycle test identity without whole-file replacement; the earlier transient `ai_backend/app.py` truncation remains fully repaired.
- User clarified pause provenance: Dev self-paused, user resumed it, then user manually paused it again. The present disabled state is an explicit user pause.

## Reliability outcomes
- AMP-006 global lease: HARMFUL / RETIRED.
- Connector-first routing: HELPED.
- Wait-before-defer: HELPED.
- Narrow Coordinator Dev-resume authority: HELPED and correctly refuses ambiguous/manual pauses.
- AMP-007B degraded mode: HELPED.
- AMP-007C/D/E neutral small-state persistence: HELPED materially.
- Validated `patch-control` large-file editing: HELPED in live v1.0.29 acceptance tests.

## Verification focus
1. Preserve patch-control fail-closed guards and use it only when ordinary whole-file writes are unsafe or blocked.
2. Preserve the current explicit user pause until the user resumes Dev.
3. Keep current neutral state files small and writable.
4. Future automation replies start with AUTOMATION ISSUES and include LAST RUN time in Europe/Budapest.
