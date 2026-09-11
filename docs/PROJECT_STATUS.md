# Miniscuplter Project Status

> Fast-moving project dashboard. Inspect actual Git/release/CI state before trusting this file.

Last reconciled: 2026-09-11

## Current release / development state

- **Latest published stable release:** `v1.0.25`.
- **Stable release target:** `a7fc4bcf5f771c18060e5aee7c98026131731c2a`.
- **Current writable development branch:** `v1.0.26`.
- **Latest exact-head CI-green implementation checkpoint before docs reconciliation:** `4418d74fde5c6cbb72237946d73ad2f960abfd73`.
- **Current objective:** integrated v1.0.26 release-candidate hardening (Objective D in HANDOFF).
- **Overall completion:** **approximately 64% acceptance-weighted**; the current release chunk is implementation-complete for Objectives A–C but still needs exact-candidate Windows export/installer hardening and later target-machine verification.

v1.0.25 is published and immutable. All new development belongs on v1.0.26 unless Coordinator establishes a newer forward branch or freezes v1.0.26.

## v1.0.26 completed implementation outcomes

### Canonical backend startup / MS-030

The repaired runtime and editor now launch a real FastAPI/Uvicorn server through packaged `ai_backend/serve.py` using the backend-local repaired `.venv`. Repair uses an isolated loopback smoke port plus a unique instance token and validates the expected `1.0.26` health identity before success. Editor production startup uses the same server entry point, loopback-only binding, production port and instance-bound readiness validation. The old import-only `python app.py` gap is no longer present.

Automated exact-head evidence includes the canonical backend lifecycle test, C# runtime-ownership tests, Python/runtime tests, release audit and green Windows C#/packaging jobs.

**Acceptance status:** code/automated fix complete; MS-030 still requires reference-machine Repair + Generate 3D verification before RESOLVED.

### Viewport resize authority / MS-009

The native viewport pipeline now explicitly retires historical resize owners rather than stacking another repaint/watchdog layer. `SubViewportContainer.Stretch` remains the normal size owner; the legacy direct resize handler and resize-triggered world repair are unsubscribed, and the old size watchdog is stopped. Focused regression coverage guards these ownership invariants.

**Acceptance status:** implementation/automated regression work complete; MS-009 still requires target-machine splitter-resize verification because the symptom is renderer/GPU-sensitive.

### Compact workspace tranche / MS-027

The user-directed visible tranche is implemented: multi-line scrollable AI command console/history with Run, rendered interactive orientation cube, compact viewport tool controls without permanent instruction prose, and workflow explanation text moved behind compact info/tooltips. The existing authoritative action/camera/tool owners remain in use.

**Acceptance status:** automated workspace acceptance and packaging gates are green; target-machine UX verification remains after publication.

### Updater / MS-029

The v1.0.26 updater-side health behavior remains fixed and CI-green. The immutable v1.0.25 updater can still cause a one-time manual launcher reopen while installing v1.0.26; that transition limitation remains accepted and must not be "fixed" by rewriting published history.

## Exact-head validation evidence

At implementation checkpoint `4418d74fde5c6cbb72237946d73ad2f960abfd73`, GitHub Actions passed:

- editor, launcher, updater and Core C# build/tests;
- semantic-version identity checks;
- Python compilation and dependency resolution;
- core logic, execution foundation and durable job journal tests;
- compact workspace regressions;
- canonical backend lifecycle test;
- real geometry regressions;
- strict release audit;
- portable package build/layout validation;
- ZIP SHA-256 sidecar verification;
- installer-definition compilation.

The ordinary branch workflow correctly skipped publication and the tag-only full Windows release job. Therefore a **real Godot Windows export, generated installer build from that export, and silent installer smoke-install are not yet recorded for the exact v1.0.26 candidate** and remain Objective D work.

## Critical path

`Objective D — integrated v1.0.26 release-candidate hardening`

Remaining work is bounded release-candidate validation and documentation reconciliation, not new product breadth. The next release-worthy checkpoint must include the strongest available Windows/Godot/export/installer evidence without Dev creating a release-control request, tag or GitHub Release.

Once Objective D's stop condition is met, HANDOFF must mark `COORDINATOR REVIEW REQUESTED`; release/freeze/publication then belongs to Coordinator.

## User dependency

No product/design decision or further v1.0.25 retry is required now. Continue using the published v1.0.25 only for already-useful paths; do not repeatedly retry the known backend-health, resize or UI acceptance failures.

After a fixed release is published, target-machine acceptance should cover update/reopen, Repair health, Generate 3D reaching provider inference, splitter resize invariance, compact workspace behavior, storage containment/cancellation, and the full Stage-C save/reopen/edit/cleanup/export path.
