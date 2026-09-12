# Miniscuplter Automation Manager

> Current process/automation state. Historical checkpoints live in GitHub Issues, Prompt Ledger history, and Git history.

Last manager review: 2026-09-12 20:32 Europe/Budapest

## Role model
- User / PROJECT_CHARTER owns product intent and difficult-to-reverse decisions.
- Coordinator owns architecture, strategy, issue priority/critical path, release readiness/chunking/publication.
- Dev owns implementation, validation, current execution-state updates and release-worthy checkpoints; Dev never publishes.
- Automation Manager owns scheduler/process/GitHub operational reliability, bounded process-infrastructure repair and prompt governance.
- Daily Report is reporting-only and currently paused.

## Active automations
- `Minisculpter Dev` — enabled; `DEV-2026-09-12.005`; hourly at :00.
- `Minisculpter Coordination` — enabled; `COORD-2026-09-12.009`; every two hours from 19:30 Europe/Budapest.
- `Minisculpter Automation` — enabled; `MGR-2026-09-12.011`; every two hours from 20:30 Europe/Budapest, one hour offset from Coordinator.
- `Daily Minisculpter report` — disabled; `DAILY-2026-09-12.005`.
- Prompt Ledger and Technical Findings Ledger remain disabled inert storage/history only; legacy duplicate tasks remain disabled.
- All authoritative prompts enforce a 30-minute run budget and cross-agent wait/no-race checks. Enabled state alone is not treated as proof of active execution.

## Canonical current-state architecture
- `CURRENT_EXECUTION_STATE.md` — current Coordinator↔Dev execution baton on the authoritative writable branch.
- `TECHNICAL_DIRECTION_STATE.md` — Coordinator medium/long strategy state.
- `ISSUE_STATE.md` — active/release-relevant/user-verification issue state.
- `CROSS_AGENT_CONTEXT.md` — material user/cross-agent evidence bridge; not the primary automation-incident ledger.
- `[AMF-xxx]` GitHub Issues — primary Manager→Coordinator technical-finding review/disposition channel.
- `[AUTO-INC-xxx]` GitHub Issues — primary durable automation/process incident records.
- `HANDOFF.md`, `TECHNICAL_ROADMAP.md`, `ISSUES.md` — legacy/read-only context.
- `PROJECT_STATUS.md` — secondary dashboard only.

## Current repository / release truth
- Stable release: `v1.0.30` at `3a349a38836641caee2a7c9c68d9b0e15febe5cd`.
- Published `v1.0.30` branch pointer remains drifted at `47a95ea1f855147d8d71d3e775c594326a725638`; the release tag/candidate SHA remains authoritative and the branch is quarantined from execution truth.
- Writable: `v1.0.31`; root `VERSION` is `1.0.31`; release freeze/publication inactive.
- Writable HEAD at review start: `7f7c04b65791d25b4eec68373f80ed18fef3594b` (`docs: advance v1.0.31 to storage containment audit`).
- Current objective: C — Stage-C storage/offline containment audit, READY.
- Completed in v1.0.31: A0/MS-032 terminal generation failure cleanup, A generated-object rehydration/edit continuity, B cleanup/export transaction and exact-scope integrity.
- Next after C: D — integrated v1.0.31 thin-slice checkpoint, then Coordinator review.
- Latest exact-head `core-foundation` and build workflows at `7f7c04b...` completed successfully.

## Finding / incident state
- `AMF-001` / Issue #2 was ACCEPTED, promoted to `MS-032`, implemented, validated and remains closed completed.
- `AUTO-INC-001` / #3 recovered and closed completed.
- `AUTO-INC-002` / #4 recovered; published-branch drift is contained by writable-branch resolution and immutability prompt guards.
- `AUTO-INC-003` / #5 recovered and closed completed; bounded patch-control retry worked as designed.
- `AUTO-INC-004` / #6 tracked the earlier safety-blocked Manager state-file refresh. This review successfully reconciled the state file; close the incident as recovered/completed after write verification.

## Reliability outcomes
- AMP-006 global lease: HARMFUL / RETIRED.
- Connector-first routing and wait-before-defer: HELPED.
- Small neutral canonical-state architecture: HELPED materially.
- Validated `patch-control`: HELPED; fail-closed guards preserved.
- GitHub-Issue AMF lifecycle: HELPED.
- AMP-009 Coordinator minimal-write discipline: HELPED.
- AMP-010 prompt-contract lint: HELPED.
- AUTO-INC GitHub-Issue protocol + bounded retries: HELPED.
- Published-branch immutability / writable-branch resolution guard: HELPED; no repeat stale-branch write observed after rollout.
- 30-minute cutoff + all-agent wait/no-race rule: INCONCLUSIVE but correctly present in all current authoritative prompts; continue observing.
- Coordinator/Manager two-hour stagger with one-hour offset: newly active; no overlap problem observed in this review.

## Verification focus
1. Dev continues objective C from authoritative `v1.0.31` and does not use drifted published `v1.0.30` as execution truth.
2. Coordinator reviews only after Dev/current workflow activity is settled and preserves the one-hour stagger.
3. Objective D becomes the next integrated checkpoint after C; Coordinator remains exclusive release owner.
4. Keep AUTO-INC and AMF lifecycle aligned with live evidence.
5. Re-evaluate the 30-minute cutoff and cross-agent wait rule after enough scheduled runs to judge whether they reduce overlap without truncating useful work.
