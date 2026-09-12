# Miniscuplter Project Status

> Fast-moving dashboard; actual Git/release/CI state is authoritative.

Last reconciled: 2026-09-12

## Current state
- **Latest published stable:** `v1.0.27` at `7d40d06cb4084403db3193ec77ab77b71baa24e2`.
- Publication passed exact-SHA C#/Core, Python/runtime, geometry and release-audit validation, real Godot Windows export, ZIP/hash verification, installer smoke-install and immutable tag/release publication.
- **Forward branch:** `v1.0.28`; bootstrap-only until semantic identity and exact-head CI are green.
- Bootstrap checkpoint `3f6d791a1211f51ae10ca35b16435c53f725348d` still fails the semantic-version gate because `ai_backend/app.py` and `tools/release_audit.py` retain 1.0.27 identity. Other audited release surfaces and backend lifecycle expectation are already 1.0.28.
- **Overall completion:** approximately **64% acceptance-weighted**; real GTX 1080 runtime/UI acceptance remains unresolved evidence.

## v1.0.27 delivered engineering tranche
- mapped-object ground placement commits through durable Core transform authority;
- viewport picking binds stable object + mesh-revision identity;
- one production sculpt path commits against exact revision identity and advances immutable history;
- protected/revision-dependent selections persist durably and invalidate/transfer explicitly across revision change;
- focused undo/redo history restores dependent selection state and save/reopen/export lineage.

## Current critical path
1. finish v1.0.28 mechanical identity and return exact-head CI to green;
2. consume released v1.0.27 reference-machine evidence as P0 when available;
3. absent a target-machine blocker, continue bounded MS-020 work: durable job envelope → heavyweight runtime ownership → truthful cancellation/recovery.

## User dependency
No product decision is required. Highest-value testing is v1.0.27 on the GTX 1080 machine: update/reopen → Repair AI Runtime → Generate 3D → repeated resize → compact workspace → Apply/save-reopen/edit-sculpt/cleanup/exact-STL, including storage and cancellation behavior.
