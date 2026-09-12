# Miniscuplter Automation Manager

> Current process/automation state. Historical checkpoints live in `docs/automation-manager-log/`, GitHub Issues, and Git history.

Last manager review: 2026-09-12 18:16 Europe/Budapest

## Role model
- User / PROJECT_CHARTER owns product intent and difficult-to-reverse decisions.
- Coordinator owns architecture, strategy, issue priority/critical path, release readiness/chunking/publication.
- Dev owns implementation, validation, current execution-state updates and release-worthy checkpoints; Dev never publishes.
- Automation Manager owns scheduler/process/GitHub operational reliability, bounded process-infrastructure repair and prompt governance.
- Daily Report is reporting-only.

## Active automations
- `Minisculpter Dev` — enabled; prompt revision `DEV-2026-09-12.002` at this review.
- `Minisculpter Coordination` — disabled at this review; task control exposes no actor/origin, so pause provenance is unresolved and must not be inferred or overridden.
- `Minisculpter Automation` — enabled; prompt revision `MGR-2026-09-12.008`.
- `Daily Minisculpter report` — enabled; prompt revision `DAILY-2026-09-12.003`.
- Coordinator prompt revision remains `COORD-2026-09-12.006` while disabled.
- Prompt Ledger and Technical Findings Ledger remain disabled inert storage/history only; legacy duplicate tasks remain disabled.
- All four authoritative prompts support exact `Run task` as the manual-run alias and retrieve the latest saved task definition before execution.

## Canonical current-state architecture
- `CURRENT_EXECUTION_STATE.md` — current Coordinator↔Dev execution baton.
- `TECHNICAL_DIRECTION_STATE.md` — Coordinator medium/long strategy state.
- `ISSUE_STATE.md` — active/release-relevant/user-verification issue state.
- `CROSS_AGENT_CONTEXT.md` — material user/cross-agent evidence bridge; not the primary automation-incident ledger.
- `[AMF-xxx]` GitHub Issues — primary Manager→Coordinator technical-finding review/disposition channel during the current pilot.
- `[AUTO-INC-xxx]` GitHub Issues — primary durable automation/process incident records.
- `HANDOFF.md`, `TECHNICAL_ROADMAP.md`, `ISSUES.md` — legacy/read-only context.
- `PROJECT_STATUS.md` — secondary dashboard only.

## Current repository / release truth
- Stable: `v1.0.30` at `3a349a38836641caee2a7c9c68d9b0e15febe5cd`.
- Writable: `v1.0.31`; root `VERSION` is `1.0.31`; no release freeze/publication.
- Branch HEAD before this Manager record: `e823ccb958fd101c2183a84804ce5a4411799421` (`coord: prioritize MS-032 before Stage-C rehydration`).
- Current objective A0 is `MS-032` terminal non-cancelled generation-failure envelope retirement; state READY.
- Ordered continuation after A0: generated-object rehydration/edit continuity → cleanup/export integrity → storage/offline containment → integrated v1.0.31 checkpoint / Coordinator review.
- Latest visible exact-head `core-foundation` workflow for `e823ccb...` completed successfully.

## Finding / incident state
- `AMF-001` / GitHub Issue #2 was independently ACCEPTED by Coordinator, promoted to canonical `MS-032`, and closed `completed` after promotion.
- `AUTO-INC-001` / GitHub Issue #3 records the initial incident-persistence safety block. Persistence succeeded on this review; an explicit state-reason close was safety-blocked, then a narrowed close retry succeeded and GitHub recorded the issue as `completed`.
- `MS-032` remains active in product code at current HEAD; implementation belongs to Dev, not Manager.

## Reliability outcomes
- AMP-006 global lease: HARMFUL / RETIRED.
- Connector-first routing and wait-before-defer: HELPED.
- Small neutral canonical-state architecture: HELPED materially.
- Validated `patch-control`: HELPED; fail-closed guards preserved.
- GitHub-Issue AMF pilot + AMP-008 lifecycle: HELPED.
- AMP-009 Coordinator minimal-write discipline: HELPED; execution reprioritization no longer requires unnecessary strategy-state rewrites.
- AMP-010 prompt-contract lint: HELPED; continue checking live prompt agreement each review.
- AUTO-INC GitHub-Issue protocol: HELPED after the initial persistence block; first incident is now durably recorded and closed.
- Manager bounded incident-persistence retry policy: HELPED; one narrowed retry recovered the close operation without blind looping.

## Verification focus
1. Do not infer or override Coordinator disablement provenance; only resume under the established authorization rules.
2. Dev advances A0 / MS-032 before rehydration when safe to run.
3. Keep AMF and AUTO-INC lifecycle/status aligned with canonical state and live evidence.
4. Preserve patch/release/forward/version-control fail-closed guards and exact-head discipline.
5. Keep this Manager state concise and reconcile it when release/branch/process truth materially changes.
