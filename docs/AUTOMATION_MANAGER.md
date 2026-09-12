# Miniscuplter Automation Manager

> Current process/automation state. Historical checkpoints live in GitHub Issues, Prompt Ledger history, and Git history.

Last manager review: 2026-09-13 00:34 Europe/Budapest

## Role model
- User / PROJECT_CHARTER owns product intent and difficult-to-reverse decisions.
- Coordinator owns architecture, strategy, issue priority/critical path, release readiness/chunking/publication.
- Dev owns implementation, validation, current execution-state updates and release-worthy checkpoints; Dev never publishes.
- Automation Manager owns scheduler/process/GitHub operational reliability, bounded process-infrastructure repair and prompt governance.
- Daily Report is reporting-only and currently paused.

## Active automations
- `Minisculpter Dev` — enabled; `DEV-2026-09-12.005`; hourly at :00.
- `Minisculpter Coordination` — enabled; `COORD-2026-09-12.010`; every two hours from 19:30 Europe/Budapest.
- `Minisculpter Automation` — enabled; `MGR-2026-09-12.012`; every two hours from 20:30 Europe/Budapest, one hour offset from Coordinator.
- `Daily Minisculpter report` — disabled; `DAILY-2026-09-12.005`.
- Prompt Ledger and Technical Findings Ledger remain disabled inert storage/history only; legacy duplicate tasks remain disabled.
- All authoritative prompts enforce a 30-minute run budget and cross-agent wait/no-race checks. Enabled state alone is not treated as proof of active execution.
- Coordinator now has a release execution completion gate: an initiated release-control publication is monitored to terminal workflow state and independently verified against tag/release/candidate SHA/assets before success is declared; genuinely non-terminal publication near finalization is handed off as `PUBLICATION PENDING`.

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
- Stable release: `v1.0.31` at `7a2dcfd238c29639935f72de8ed635ca4efe3725`.
- Published `v1.0.31` branch pointer exactly matches the release candidate and is read-only execution history.
- Writable: `v1.0.32`; root `VERSION` is `1.0.32`; release freeze/publication inactive.
- Writable HEAD at review start: `003c32c93602c316ae0e62e81d115937c258533c` (`docs: record v1.0.32 history hardening progress`).
- `CURRENT_EXECUTION_STATE.md` is reconciled: stable v1.0.31, writable v1.0.32, no execution hold.
- Current objective: B — editing/history continuity hardening, IN PROGRESS; A remains a parallel target-machine verification gate for released v1.0.31.
- Next after B: C launcher/runtime recovery and update-path qualification, then D integrated v1.0.32 checkpoint and Coordinator review.
- Exact-head build run `34721740942` for `003c32c...` completed successfully; latest observed workflow activity is settled.

## Finding / incident state
- No open `[AMF-xxx]` Issues.
- `AMF-001` / Issue #2 was ACCEPTED, promoted to `MS-032`, implemented and validated; closed completed.
- `AUTO-INC-001` through `AUTO-INC-004` are recovered/closed.
- `AUTO-INC-005` / Issue #7 tracked the missing post-v1.0.31 forward/bootstrap state. Manager created and bootstrapped v1.0.32 through validated control planes; Coordinator subsequently reconciled CURRENT and Dev resumed safe work. Issue #7 was closed completed in this review.

## Reliability outcomes
- AMP-006 global lease: HARMFUL / RETIRED.
- Connector-first routing and wait-before-defer: HELPED.
- Small neutral canonical-state architecture: HELPED materially.
- Validated `patch-control`: HELPED; fail-closed guards preserved.
- GitHub-Issue AMF lifecycle: HELPED.
- AMP-009 Coordinator minimal-write discipline: HELPED.
- AMP-010 prompt-contract lint: HELPED.
- AUTO-INC GitHub-Issue protocol + bounded retries: HELPED.
- Published-branch immutability / writable-branch resolution guard: HELPED; v1.0.31 remains exactly at its published candidate while Dev advances v1.0.32.
- 30-minute cutoff + all-agent wait/no-race rule: HELPED so far; no overlap/race observed in this review and agents are finishing inside bounded runs.
- Coordinator/Manager two-hour stagger with one-hour offset: HELPED so far; no overlap problem observed.
- Release terminal-confirmation gate: INCONCLUSIVE for future publication because it was introduced after v1.0.31 publication; contract is present in Coordinator and Manager prompts and must be evaluated on the next Coordinator-owned release.

## Process risks / drift
- `TECHNICAL_DIRECTION_STATE.md` is stale after the v1.0.31 release: it still names stable v1.0.30 / release line v1.0.31 and the completed v1.0.31 thin-slice priority order. This is Coordinator-owned strategic state, not a Manager mutation target. CURRENT is correct and Dev is executing safely, so this is not presently blocking implementation, but the next Coordinator review should reconcile TECHNICAL_DIRECTION to v1.0.32 strategy before release-scope decisions depend on it.
- `ISSUE_STATE.md` still phrases several target-machine verification requests against v1.0.30 even though v1.0.31 is the latest release. Preserve those as historical verification wording unless Coordinator/user evidence determines which cases should move to v1.0.31; do not silently rewrite user-verification truth.

## Verification focus
1. Dev continues B on authoritative `v1.0.32` and advances automatically to C/D only under CURRENT continuation rules.
2. Coordinator reconciles stale `TECHNICAL_DIRECTION_STATE.md` to current v1.0.32 strategy/release line before consequential roadmap or publication decisions.
3. Released v1.0.31 target-machine gate A remains explicit; runtime claims are not upgraded from CI alone.
4. Coordinator remains exclusive release owner and, on its next publication, follows the terminal workflow observation + independent release verification gate end-to-end.
5. Keep AUTO-INC and AMF lifecycle aligned with live evidence; no open incident/finding remains at this checkpoint.
