# Miniscuplter Automation Manager

> Current process/automation state. Historical checkpoints live in `docs/automation-manager-log/` and Git history.

Last manager review: 2026-09-12 14:27 Europe/Budapest

## Role model
- User / PROJECT_CHARTER owns product intent and difficult-to-reverse decisions.
- Coordinator owns architecture, strategy, issue priority/critical path, release readiness/chunking/publication.
- Dev owns implementation, validation, current execution-state updates and release-candidate preparation; Dev never publishes.
- Automation Manager owns scheduler/process/GitHub operational reliability and bounded process-infrastructure repair.
- Daily Report is reporting-only.

## Active automations
- `Minisculpter Dev` — enabled; current execution state records that the user explicitly resumed Dev. Last recorded run began about 14:24 Europe/Budapest.
- `Minisculpter Coordination` — enabled, every 3 hours at :30.
- `Daily Minisculpter report` — enabled, daily 22:45 Europe/Budapest.
- `Minisculpter Automation` — enabled, twice daily.
- Legacy duplicate tasks remain disabled.
- All four authoritative prompts support exact `Run task` as the manual-run alias and require retrieval of the latest saved task definition before execution.

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
- Branch HEAD before this Manager record: `fb59325b62a834d3a91e04043eb98491329918ed`.
- CURRENT objective D is COMPLETE — COORDINATOR REVIEW REQUESTED.
- Objectives A–C are complete: semantic bootstrap, durable generation job envelope, and cancellation/recovery closure.
- Validated application head `7b96524ae90fd7ecf237201121de2eaee0ff022f` passed integrated .NET/Core, Python/runtime/job, backend lifecycle, geometry, release-audit and packaging checks.
- Latest branch-head build/core workflows are green.

## Current process state
- `patch-control` is operational and was used repeatedly for Stage-C large-file edits. Two malformed requests failed closed and were followed by corrected successful requests; no target corruption occurred.
- Release-control has no active v1.0.29 publication request.
- CURRENT_EXECUTION_STATE is current. TECHNICAL_DIRECTION_STATE and PROJECT_STATUS still lag behind the completed A–C work and should be reconciled by the Coordinator review before further queue advancement.
- No active conflicting repository or release workflow was observed during this Manager review.

## Reliability outcomes
- AMP-006 global lease: HARMFUL / RETIRED.
- Connector-first routing: HELPED.
- Wait-before-defer: HELPED.
- Narrow Coordinator Dev-resume authority: HELPED.
- AMP-007B degraded mode: HELPED.
- AMP-007C/D/E neutral small-state persistence: HELPED materially.
- Validated `patch-control` large-file editing: HELPED; fail-closed behavior also worked on malformed requests.
- `Run task` latest-prompt manual alias: APPLIED; this Manager run successfully retrieved and executed the current saved definition.

## Verification focus
1. Coordinator reconciles TECHNICAL_DIRECTION_STATE / PROJECT_STATUS and decides v1.0.29 release-readiness/chunk or replenishes CURRENT before Dev advances.
2. Preserve patch-control fail-closed guards and exact-head/blob discipline.
3. Confirm Dev enablement remains intentional if future repository/task evidence becomes contradictory.
4. Keep mutable current-state files small and writable.
