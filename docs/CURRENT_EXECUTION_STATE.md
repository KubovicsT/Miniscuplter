# Miniscuplter Current Execution State

This file is the authoritative current execution-state record shared by Coordinator and Dev. It stores project state only; behavioral rules remain in the automation prompts. `docs/HANDOFF.md` is legacy context.

## Release state
- stable_version: v1.0.27
- stable_sha: 7d40d06cb4084403db3193ec77ab77b71baa24e2
- writable_branch: v1.0.28
- release_freeze: false
- publication_active: false
- bootstrap_state: incomplete
- bootstrap_remaining: ai_backend/app.py; tools/release_audit.py

## Ordered objectives

### A — v1.0.28 bootstrap
- state: current
- outcome: remaining 1.0.27 identity surfaces changed to 1.0.28; exact-head Core/full-build green
- continuation: automatic

### B — Generate 3D runtime ownership
- state: next
- priority: P0
- outcome: heavyweight-runtime ownership conflict resolved
- verification: targeted regressions; exact-head validation; GTX 1080 user verification
- continuation: automatic

### C — accepted-baseline reopen persistence
- state: next
- priority: P0
- proposed_issue_id: MS-031
- outcome: accepted-baseline state restored after project reopen when retained image exists
- verification: save/reopen regression coverage; exact-head validation
- continuation: automatic

### D — viewport grid
- state: next
- issue_id: MS-009
- outcome: visible neutral non-occluding grid at launch and through resize
- continuation: automatic

### E — workspace corrections
- state: next
- issue_ids: MS-026; MS-027
- outcome: bottom-center multiline AI console; vertical contextual actions; 2x2 bottom-left resource telemetry; circular XYZ orientation gizmo without cube body
- continuation: automatic

### F — v1.0.28 integration checkpoint
- state: next
- outcome: strongest integrated validation complete; canonical state reconciled; release-worthy checkpoint available for Coordinator review
- continuation: coordinator_review_required

## Preemption classes
update; backend; generation; persistence; storage; cancellation; data-safety
