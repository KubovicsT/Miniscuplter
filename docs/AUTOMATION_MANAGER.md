# Miniscuplter Automation Manager

> Current process/automation state. Historical checkpoints live in `docs/automation-manager-log/` and legacy reliability/history files.

Last manager review: 2026-09-12

## Role model
- User / PROJECT_CHARTER owns product intent and difficult-to-reverse product decisions.
- Coordinator owns architecture, roadmap, priority, release readiness and publication.
- Dev owns implementation, validation, HANDOFF execution state and release-candidate preparation; Dev never publishes.
- Automation Manager owns scheduler/process/GitHub operational reliability and bounded process-infrastructure repair.
- Daily Report is reporting-only.

## Active automations
- `Minisculpter Dev` — enabled, hourly on whole hours.
- `Minisculpter Coordination` — enabled, every 3 hours at :30.
- `Daily Minisculpter report` — enabled, daily 22:45 Europe/Budapest.
- `Minisculpter Automation` — enabled, twice daily.
- Older duplicate Dev/Coordinator/Daily/Manager tasks remain disabled.

## Current repository / release truth
- Latest stable: `v1.0.27` at `7d40d06cb4084403db3193ec77ab77b71baa24e2`.
- Writable development branch: `v1.0.28`; no release freeze.
- Current reviewed HEAD: `06d755e98fd87bc4dbc97685cb76f2ad47a7df4c`.
- Exact-head full build is blocked only by semantic-version identity because `ai_backend/app.py` and `tools/release_audit.py` still report 1.0.27; packaging/.NET and Core evidence are otherwise green.
- CROSS_AGENT_CONTEXT durably carries the Coordinator-evaluated P0 ordering: bootstrap → 3D generation runtime ownership → accepted-baseline reopen persistence → MS-009 grid → workspace corrections → integration review.

## Current process state
- Dev is enabled; no Coordinator resume action is currently required.
- GitHub connector and automation control are available in Coordinator runs after the routing correction.
- Coordinator twice attempted an essential `docs/HANDOFF.md` reconciliation after refetching and narrowing the mutation; both writes were rejected by the connector safety layer.
- Coordinator correctly stopped further planning-document mutations after the second rejection instead of cascading into ROADMAP/STATUS/ISSUES rewrites.
- `HANDOFF.md` remains stale in ordering, but its Preemption rule plus the newer CROSS_AGENT_CONTEXT P0 record gives Dev a safe durable signal to prioritize serious generation/persistence failures.

## Reliability policy / outcomes
- **AMP-006:** HARMFUL / RETIRED.
- **Wait-before-defer:** HELPED so far.
- **GitHub connector-first routing:** HELPED; the earlier generic-web `DisabledError` misclassification is corrected.
- **Narrow Coordinator Dev-resume authority:** HELPED; Dev is enabled and user pauses remain protected.
- **AMP-007B degraded mode:** HELPED; the Coordinator continued useful read-only reasoning after blocked persistence.
- **AMP-007C safety-block avoidance:** PARTIALLY HELPED; refetch + narrowed retry prevented retry loops, but the minimal HANDOFF rewrite was still rejected.
- **AMP-007D persistence discipline:** HELPED; no cascade of duplicate planning writes occurred.
- **AMP-007E small-file architecture:** HELPED; HANDOFF is already small, so this incident is not explained by file size.

## Open process risks
- Repeated safety-layer rejection appears specific to the Coordinator/HANDOFF mutation shape, not a general GitHub outage.
- Canonical HANDOFF can temporarily lag a newer Coordinator decision; CROSS_AGENT_CONTEXT currently mitigates this, but it should not become a permanent second planning authority.
- Connector false positives on ordinary current-state replacements remain the main operational reliability issue.

## Current verification focus
1. Confirm ordinary existing-file updates still work on Manager-owned files.
2. Determine whether HANDOFF rejection is path-specific, content-shape-specific, or Coordinator-run-specific without taking over Coordinator planning authority.
3. Ensure Dev consumes the P0 preemption before stale grid/layout ordering.
4. Have Coordinator reconcile HANDOFF/STATUS/ISSUES once a safe write shape succeeds.

Historical small-file adoption record: `docs/automation-manager-log/2026-09-12-small-file-adoption.md`.
