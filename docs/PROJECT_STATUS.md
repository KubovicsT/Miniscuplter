# Miniscuplter Project Status

> Fast-moving project dashboard. Inspect actual Git/release/CI state before trusting this file.

Last reconciled: 2026-09-12

## Current release / development state

- **Latest published stable at reconciliation time:** `v1.0.25`.
- **Frozen v1.0.26 candidate:** `a41e0419ba40fd1118775e8f516b1a31145f18d8`.
- **v1.0.26 release-control:** active; exact-SHA autonomous release workflow initiated by Coordinator.
- **Current writable development branch:** `v1.0.27`.
- **v1.0.27 ancestry:** fast-forwarded from the earlier premature/orphan branch to the actual frozen v1.0.26 candidate as part of the legitimate release transition.
- **v1.0.27 semantic identity:** mechanically bumped to 1.0.27 across audited release surfaces.
- **Overall completion:** approximately **64% acceptance-weighted**. Publication does not itself resolve reference-machine acceptance.

## v1.0.26 release contents

### MS-030 packaged backend health
Code/automated fix complete. Repair/editor share the backend-local repaired venv and canonical packaged `serve.py` Uvicorn entry point. Repair uses an isolated loopback smoke port and instance/version validation; editor uses the same server contract on production port.

**Status:** FIXED - NEEDS USER VERIFICATION.

### MS-009 viewport resize
Historical direct resize/world-repair/watchdog ownership was retired; native Stretch remains the normal render-target size owner with focused ownership regressions.

**Status:** FIXED - NEEDS USER VERIFICATION.

### MS-029 updater health protocol
Successful health-confirmed launcher processes are no longer killed by the fixed updater path. The immutable v1.0.25 updater may still require one manual reopen while installing v1.0.26.

**Status:** FIXED - NEEDS USER VERIFICATION.

### MS-027 user-directed compact workspace tranche
Implemented: multi-line scrollable AI command console/history, rendered interactive view cube, compact viewport tools and detailed help moved behind compact info/tooltips.

**Status:** current tranche implemented; target-machine UX verification pending. Broader MS-027 workspace work remains open.

## Release validation

Exact frozen v1.0.26 HEAD passed ordinary exact-head Core and build CI, including semantic identity, C#/Core, Python/runtime, backend lifecycle, geometry, release audit and packaging gates available in the branch workflow.

The autonomous release-control workflow is responsible for the remaining exact-SHA real Godot Windows export, generated installer/hash verification, silent smoke-install and publication guards.

## Current critical path

1. complete/verify v1.0.26 autonomous publication;
2. validate v1.0.27 bootstrap identity/CI on the forward branch;
3. consume v1.0.26 GTX 1080 reference-machine evidence;
4. if acceptance evidence does not preempt, continue bounded Core-authority migration beginning with mapped-object ground placement.

## User dependency

No product decision is needed. Once v1.0.26 publishes, test update/reopen, Repair AI Runtime, Generate 3D, viewport resize, compact UI and the complete Stage-C save/reopen/edit/cleanup/export flow.
