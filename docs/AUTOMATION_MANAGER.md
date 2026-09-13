# Miniscuplter Automation Manager

> Current process/automation state. Historical checkpoints live in GitHub Issues, Prompt Ledger history, and Git history.

Last manager review: 2026-09-13 18:42 Europe/Budapest

## Role model
- User / PROJECT_CHARTER owns product intent and difficult-to-reverse decisions.
- Coordinator owns architecture, strategy, issue priority/critical path, release readiness/chunking/publication.
- Dev owns implementation, validation, current execution-state updates and release-worthy checkpoints; Dev never publishes.
- Automation Manager owns scheduler/process/GitHub operational reliability, bounded process-infrastructure repair and prompt governance.
- Daily Report is reporting-only and currently paused.

## Active automations
- `Minisculpter Dev` — enabled; `DEV-2026-09-13.007`; hourly at :00.
- `Minisculpter Coordination` — enabled; `COORD-2026-09-13.012`; every two hours at :30, offset one hour from Manager.
- `Minisculpter Automation` — enabled; `MGR-2026-09-13.014`; every two hours at :30.
- `Daily Minisculpter report` — disabled; `DAILY-2026-09-13.006`.
- Prompt Ledger and Technical Findings Ledger remain disabled inert storage/history only; legacy duplicate tasks remain disabled.
- All authoritative prompts retain the 30-minute run budget, all-agent no-race/wait rule, bounded retries, published-branch immutability, simplified five-field replies, and Run-task alias semantics.
- Coordinator remains exclusive release owner; Dev never publishes.

## Canonical current-state architecture
- `CURRENT_EXECUTION_STATE.md` — current Coordinator↔Dev execution baton on the authoritative writable branch.
- `TECHNICAL_DIRECTION_STATE.md` — Coordinator medium/long strategy state.
- `ISSUE_STATE.md` — active/release-relevant/user-verification issue state.
- `CROSS_AGENT_CONTEXT.md` — material user/cross-agent evidence bridge; not the primary automation-incident ledger.
- `[AMF-xxx]` GitHub Issues — primary Manager→Coordinator technical-finding review/disposition channel.
- `[AUTO-INC-xxx]` GitHub Issues — primary durable automation/process incident records.
- Legacy HANDOFF/TECHNICAL_ROADMAP/ISSUES remain read-only context; PROJECT_STATUS is secondary.

## Current repository / release truth
- Stable release: `v1.0.32`; its semantic-version branch still matches the published release candidate and remains immutable execution history.
- Writable: `v1.0.33`; root `VERSION` is `1.0.33`; release freeze/publication inactive.
- CURRENT is coherent and explicitly keeps v1.0.32 reference-machine verification non-blocking while v1.0.33 independent foundation work continues.
- Latest v1.0.33 work has green exact-head Core and full build/package validation.
- Core attachment authority and revision-bound refinement/selection persistence are advancing without choosing the deferred user-facing Refinement/Kitbash UI.

## Finding / incident state
- No open `[AMF-xxx]` Issues.
- `AUTO-INC-009` is recovered/closed: ordinary connector new-file creation was reproducibly safety-blocked, so Manager extended patch-control with a guarded exact-HEAD `create_text_file` route; Dev exercised it successfully on the intended Core source and exact-head validation passed.
- `AUTO-INC-008` remains open/monitoring: two old v1.0.32 GitHub Actions runs are still stuck queued and cannot be cancelled, while later workflows continue to execute normally. This is treated as a non-blocking GitHub-side anomaly and is not a reason to alter product CI or stop development.

## Reliability outcomes
- AMP-006 global lease: HARMFUL / RETIRED.
- Connector-first routing and wait-before-defer: HELPED.
- Small neutral canonical-state architecture: HELPED materially.
- Validated patch-control: HELPED materially; exact-head/blob and published-branch guards remain effective, and the new guarded text-file creation path recovered AUTO-INC-009 without weakening fail-closed behavior.
- GitHub-Issue AMF lifecycle: HELPED.
- AMP-009 Coordinator minimal-write discipline: HELPED.
- AMP-010 prompt-contract lint: HELPED.
- AUTO-INC GitHub-Issue protocol + bounded retries: HELPED.
- Published-branch immutability / writable-branch resolution guard: HELPED.
- 30-minute cutoff + all-agent wait/no-race rule: HELPED; no shared-state race was observed in this review.
- Coordinator/Manager stagger: HELPED.
- User-verification non-blocking policy: HELPED; Dev continued independent v1.0.33 work while v1.0.32 runtime verification remains pending.
- Release terminal-confirmation gate: HELPED on the v1.0.32 publication; release/tag/assets and forward-version bootstrap were independently verified before continuing.

## Process assessment
- Coordinator direction remains coherent and stable: v1.0.32 is the runtime verification lane, while v1.0.33 advances UI-neutral durable-state foundations.
- Dev is aligned with CURRENT and is producing acceptance-oriented Core/persistence work rather than waiting on user runtime availability.
- The guarded patch-control recovery was used successfully in real development and did not create a duplicate Godot durable authority.
- Daily remains intentionally paused and no reporting-role drift is present.
- Prompt-contract lint passes at Coordinator `.012`, Dev `.007`, Manager `.014`, Daily `.006`; Prompt Ledger revision map agrees.

## Verification focus
1. Dev continues the attachment revision contract and revision-safe Refinement dependency foundation under CURRENT.
2. Keep v1.0.32 target-machine-only claims as `NEEDS USER VERIFICATION`, but never use ordinary user unavailability as a global development stop.
3. Keep AUTO-INC-008 monitoring-only unless the zombie Actions entries begin blocking new workflows or otherwise materially affect CI.
4. Reopen/new incident only if the recovered new-file path materially fails again; otherwise treat AUTO-INC-009 as completed recovery.
5. Preserve Coordinator exclusive release ownership and fail-closed release/patch/version control-plane guards.
