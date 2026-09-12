# Miniscuplter Automation Manager

> Current process/automation state. Historical checkpoints live in `docs/automation-manager-log/` and legacy reliability/history files.

Last manager review: 2026-09-12

## Role model
- User / PROJECT_CHARTER owns product intent and difficult-to-reverse product decisions.
- Coordinator owns architecture, roadmap, priority, release readiness and publication.
- Dev owns implementation, validation, HANDOFF and release-candidate preparation; Dev never publishes.
- Automation Manager owns process/scheduler/GitHub operational reliability and bounded process-infrastructure repair.
- Daily Report is reporting-only.

## Active automations
- `Minisculpter Dev` — enabled, hourly on whole hours.
- `Minisculpter Coordination` — enabled, every 3 hours at :30.
- `Daily Minisculpter report` — enabled, daily 22:45 Europe/Budapest.
- `Minisculpter Automation` — enabled, twice daily; excluded from worker scoring.
- Older duplicate Dev/Coordinator/Daily/Manager tasks remain disabled and must not drive current behavior.

## Current repository / release truth
- Latest stable: `v1.0.26` at `a41e0419ba40fd1118775e8f516b1a31145f18d8`.
- Writable development branch: `v1.0.27`.
- Latest non-Manager project HEAD reviewed before this checkpoint: `2c3205b0c42697a0a75ffd3814f23a452cc24866`; latest engineering checkpoint is `4d924e948647022dce4bd2f7682140a005edebc5`.
- Core and full build workflows for `2c3205b0...` both completed successfully.
- Manager documentation commits after that project checkpoint are process-only and do not change engineering acceptance.
- No active v1.0.27 release request/freeze was observed at this review.
- Released v1.0.26 target-machine acceptance remains P0 where CI cannot prove GUI/GPU/runtime behavior.

## Current process state
- Dev completed the authorized D/E/F Stage-D queue: revision-bound sculpt/edit, revision-dependent protected-selection semantics, and transactional edit-history closure. Dev correctly stopped at the explicit `Auto-proceed: NO` / Coordinator-review boundary.
- Coordinator's previous Stage-D sequencing was coherent and is preserved in `docs/coordinator-log/2026-09-12-v1.0.27-stage-d-sequencing.md`.
- The completed D/E/F queue finished after the previous Coordinator review; waiting for the next Coordinator review is expected and is not queue starvation.
- `PROJECT_STATUS.md` / `TECHNICAL_ROADMAP.md` still lag the completed D/E/F execution state and should be reconciled by Coordinator/Dev under their canonical ownership; Manager does not take over technical planning.

## Reliability policy / outcomes
- **AMP-006:** HARMFUL / RETIRED. Never use the old global lease protocol.
- **Wait-before-defer:** HELPED so far; no unresolved automation race observed in this review.
- **AMP-001 release/version reconciliation:** HELPED across multiple forward-version transitions.
- **AMP-003 Coordinator 3-hour cadence:** APPLIED; verification still ongoing, but the current D/E/F review boundary is being reached within the intended cadence.
- **AMP-004 Daily :45 timing:** APPLIED; continue observing report/run overlap.
- **AMP-007B degraded mode:** active; non-essential safety-blocked persistence must not kill useful work.
- **AMP-007C safety-block avoidance:** active; small, single-purpose, verified mutations only.
- **AMP-007D persistence discipline:** active; write only canonical truth that actually changed.
- **AMP-007E small-file architecture:** active and beginning to help; Coordinator history has already moved to small immutable event files.

## Small-file architecture
- Keep frequently edited current-state files concise; target `HANDOFF.md` around 1–2 KB when practical.
- Keep `TECHNICAL_ROADMAP.md` stable and strategy-focused, not a run log.
- Use `docs/coordinator-log/`, `docs/automation-manager-log/`, and similar small immutable event/evidence files for durable history.
- Existing large `COORDINATOR_LOG.md`, prior reliability files and Git history remain legacy context rather than preferred write targets.
- `CROSS_AGENT_CONTEXT.md` (~12 KB) and `ISSUES.md` (~45 KB) are now notable growing monoliths. Their owning roles should compact/split them when next materially edited; no emergency rewrite is required.

## Open process risks
- Connector safety false positives remain possible for compound/high-impact writes; permissions are not the known root cause.
- Canonical planning lag can occur when execution finishes between Coordinator runs; Git/CI + HANDOFF remain the immediate truth until reconciliation.
- Reference-machine acceptance is still the key evidence gap for GUI/GPU/runtime claims.

## Current proposals
No proposal is awaiting user approval.

## Verification focus
1. Coordinator consumes the completed D/E/F queue without inventing stale Stage-D work and decides release chunk/readiness or the next authorized objectives.
2. Dev does not cross explicit no-auto-proceed boundaries while waiting.
3. AMP-007C/D/E reduce safety-block incidents and documentation churn without losing continuity.
4. Daily continues to report repository-supported truth and remains reporting-only.

Historical detail for this review: `docs/automation-manager-log/2026-09-12-small-file-adoption.md`.
