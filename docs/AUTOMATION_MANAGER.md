# Miniscuplter Automation Manager

> Current process/automation state. Historical checkpoints live in GitHub Issues, Prompt Ledger history, and Git history.

Last manager review: 2026-09-13 10:29 Europe/Budapest

## Role model
- User / PROJECT_CHARTER owns product intent and difficult-to-reverse decisions.
- Coordinator owns architecture, strategy, issue priority/critical path, release readiness/chunking/publication.
- Dev owns implementation, validation, current execution-state updates and release-worthy checkpoints; Dev never publishes.
- Automation Manager owns scheduler/process/GitHub operational reliability, bounded process-infrastructure repair and prompt governance.
- Daily Report is reporting-only and currently paused.

## Active automations
- `Minisculpter Dev` — enabled; `DEV-2026-09-12.005`; hourly at :00.
- `Minisculpter Coordination` — enabled; `COORD-2026-09-12.010`; every two hours at :30, offset one hour from Manager.
- `Minisculpter Automation` — enabled; `MGR-2026-09-12.012`; every two hours at :30.
- `Daily Minisculpter report` — disabled; `DAILY-2026-09-12.005`.
- Prompt Ledger and Technical Findings Ledger remain disabled inert storage/history only; legacy duplicate tasks remain disabled.
- All authoritative prompts retain the 30-minute run budget, all-agent no-race/wait rule, bounded retries, published-branch immutability, and Run-task alias semantics.
- Coordinator remains exclusive release owner and must observe initiated publication to terminal workflow state plus independently verify tag/release/candidate SHA/assets before declaring success.

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
- Published `v1.0.31` branch pointer exactly matches the release candidate and remains read-only execution history.
- Writable: `v1.0.32`; root `VERSION` is `1.0.32`; release freeze/publication inactive.
- Writable HEAD at review start: `8b52d64353ed54df57c87bad686ca090e34cdeab` (`state: advance E4 after resize fix`).
- Exact-head `core-foundation` and full `build` for `8b52d643...` completed successfully.
- CURRENT is coherent: E1/E2 complete, E3 implementation complete pending user verification, E4 in progress; no execution hold.
- E4 remaining work is the accepted workspace/direct-manipulation scope: MS-026/MS-027 telemetry + AI-command composition and MS-038 direct manipulation/rotation rings. E5 then integrates/regresses v1.0.32 and returns to Coordinator review.

## Finding / incident state
- No open `[AMF-xxx]` Issues.
- No open `[AUTO-INC-xxx]` Issues.
- `AMF-001` / Issue #2 was accepted, promoted to `MS-032`, implemented and validated; closed completed.
- `AUTO-INC-001` through `AUTO-INC-007` are recovered/closed.
- `AUTO-INC-007` / Issue #9 tracked a patch-control Actions runner queue stall. User manually cancelled the stuck run; Manager verified it terminal-cancelled with target `v1.0.32` unchanged, closed the incident completed, and Dev resumed safely.

## Reliability outcomes
- AMP-006 global lease: HARMFUL / RETIRED.
- Connector-first routing and wait-before-defer: HELPED.
- Small neutral canonical-state architecture: HELPED materially.
- Validated `patch-control`: HELPED overall; exact-head/blob and immutable-published fail-closed guards remain valuable. One external runner queue stall caused AUTO-INC-007, but cancellation/recovery preserved target safety and Dev continued afterward.
- GitHub-Issue AMF lifecycle: HELPED.
- AMP-009 Coordinator minimal-write discipline: HELPED.
- AMP-010 prompt-contract lint: HELPED.
- AUTO-INC GitHub-Issue protocol + bounded retries: HELPED.
- Published-branch immutability / writable-branch resolution guard: HELPED; v1.0.31 still exactly matches its release candidate while v1.0.32 advances.
- 30-minute cutoff + all-agent wait/no-race rule: HELPED; the patch-control stall was handed off rather than raced or bypassed, and later recovery did not corrupt target state.
- Coordinator/Manager stagger: HELPED; no cross-agent mutation race observed in this review.
- Release terminal-confirmation gate: INCONCLUSIVE until the next Coordinator-owned publication exercises it.

## Process assessment
- Coordinator direction is coherent and stable: `TECHNICAL_DIRECTION_STATE.md` now correctly identifies stable v1.0.31, writable v1.0.32 and the reference-machine regression-recovery critical path.
- Dev is aligned with CURRENT and is producing acceptance-oriented fixes rather than unrelated infrastructure. Smart Select contract, resize composition, generated-result insertion, mesh rendering and canonical placement/scale are all recorded as fixed in v1.0.32 but correctly remain user-verification pending where runtime/visual evidence is required.
- The latest Dev run completed a coherent state advance and exact-head CI is green; no active process blocker remains.
- Daily remains intentionally paused and no reporting-role drift is present.
- Prompt-contract lint passes at Coordinator `.010`, Dev `.005`, Manager `.012`, Daily `.005`; Prompt Ledger revision map agrees.

## Verification focus
1. Dev continues E4 then E5 under CURRENT continuation rules.
2. Keep target-machine-only claims as `NEEDS USER VERIFICATION` until packaged v1.0.32 is actually tested on the reference machine.
3. Watch patch-control for recurrence of prolonged runner queueing before classifying AUTO-INC-007 as a broader control-plane defect; one recovered queue stall is not enough evidence to redesign the workflow.
4. Coordinator remains exclusive release owner and must exercise the terminal workflow observation + independent release verification gate on the next publication.
5. Keep AUTO-INC and AMF lifecycle aligned with live evidence; none are open at this checkpoint.
