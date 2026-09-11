# Miniscuplter Handoff

> Immediate and downstream execution baton. TECHNICAL_ROADMAP owns whole-system strategy; this file tells Dev what it may execute continuously.

Last updated: 2026-09-11

## Repository / release state

- **Latest published stable:** `v1.0.25` at `a7fc4bcf5f771c18060e5aee7c98026131731c2a`.
- **Current writable development branch:** `v1.0.26`.
- **Current branch HEAD before this handoff commit:** `4418d74fde5c6cbb72237946d73ad2f960abfd73`.
- **Latest exact-head CI-green implementation checkpoint:** `4418d74fde5c6cbb72237946d73ad2f960abfd73`.
- **Release freeze:** none.
- **v1.0.26 release-control request:** none found.
- **Release decision:** KEEP ACCUMULATING; Dev is now in integrated candidate hardening and must not publish.

## Completed objectives in current ancestry

### Objective A — canonical backend startup / repaired-runtime contract: AUTOMATED ACCEPTANCE MET

The earlier incomplete `python app.py` lifecycle has been replaced by a real packaged server contract:

- `ai_backend/serve.py` is the canonical executable Uvicorn/FastAPI entry point and enforces loopback binding;
- Repair launches that server with the backend-local repaired `.venv`, an isolated ephemeral port and a unique instance token;
- Repair verifies both expected backend version and its own instance token before reporting success, then kills/awaits the probe process;
- editor production startup uses the same `serve.py` contract, same repaired backend-local `.venv`, production port `7868`, expected version and instance-token readiness check;
- process-tree containment, data/runtime-root environment and bounded diagnostics remain in place;
- `tools/backend_lifecycle_tests.py`, C# runtime ownership regressions and release audit are in exact-head CI.

MS-030 remains **FIXED - NEEDS USER VERIFICATION** in product acceptance terms until the reference machine confirms Repair health and Generate 3D reaches provider resolution/inference.

### Objective B — MS-009 viewport resize/render authority: IMPLEMENTATION ACCEPTED, USER RETEST PENDING

The historical conflicting resize/presentation path is now retired instead of being layered with another repair loop:

- native `SubViewportContainer.Stretch` remains the normal render-target size owner;
- the v1.0.9 direct resize writer and v1.0.11 resize-triggered world repair are explicitly unsubscribed once the native pipeline takes ownership;
- the v1.0.17 size watchdog is stopped;
- ordinary resize only refreshes diagnostics/probing; it does not recreate/rebind world state;
- focused `ViewportResizeOwnershipTests` guard owner retirement and prohibit reintroducing a direct `SubViewport.Size` writer.

MS-009 remains **FIXED - NEEDS USER VERIFICATION** until the Windows/GTX 1080 reference machine proves grid/background/model/gizmo presentation is invariant through splitter resize.

### Objective C — user-directed MS-027 compact workspace tranche: AUTOMATED ACCEPTANCE MET

The required four visible corrections are implemented as one bounded workspace tranche:

1. multi-line scrollable AI command console/history with explicit Run and authoritative dispatch;
2. rendered interactive orientation cube synchronized to the authoritative camera;
3. compact viewport tool controls with permanent instruction prose hidden;
4. workflow explanatory prose moved behind compact circular info/tooltips.

`WorkspaceAcceptanceTests`, compact workspace Python regressions and exact-head C#/packaging CI cover the tranche. Target-machine UX remains to be verified in the eventual release build.

## CURRENT OBJECTIVE — D: integrated v1.0.26 release-candidate hardening

**Goal / outcome**  
Produce one coherent exact-HEAD candidate containing runtime, updater, viewport and user-directed UI acceptance work.

**Current progress**
- Exact-head `4418d74...` GitHub Actions is green for:
  - editor/launcher/updater/Core C# build and Core tests;
  - semantic-version identity;
  - Python compile/dependency resolution;
  - core logic, execution foundation and durable job journal tests;
  - compact workspace regressions;
  - canonical backend lifecycle test;
  - real geometry regressions;
  - strict release audit;
  - portable package creation/layout;
  - ZIP SHA-256 sidecar verification;
  - installer-definition compilation.
- Branch publication jobs were correctly skipped.
- Full real Godot Windows export + generated-installer smoke-install remain release-candidate gates not exercised by the ordinary branch workflow; do not misreport those as passed.

**Constraints**
- no unrelated feature expansion;
- reconcile issue/status docs honestly;
- preserve MS-029 fix and document that immutable v1.0.25's updater can still cause one manual reopen on the v1.0.25 → v1.0.26 transition;
- run strongest available C#/Core/Python/runtime/geometry/execution/release-audit/Windows export/package/hash/installer validation;
- verify storage/data/runtime-cache/venv preservation and version identity;
- Dev must not create release-control, tag or GitHub Release.

**Remaining acceptance work**
1. obtain/record a real Godot 4.7.2 Windows export at exact candidate HEAD;
2. build the generated installer from that exact export and smoke-install it;
3. re-check exact-head release identity, storage/runtime containment and packaged `serve.py` presence;
4. reconcile PROJECT_STATUS / ISSUES for MS-030, MS-009 and MS-027 without converting target-machine-only evidence into CI acceptance;
5. record the exact release-worthy checkpoint SHA if every available gate is green.

**Acceptance / stop condition**  
Exact current HEAD coherent, all available automated/package gates green, release-worthy checkpoint recorded, docs list remaining target-machine verification accurately, and HANDOFF marks `COORDINATOR REVIEW REQUESTED`.

**Preemption**  
Any failing release gate or new serious user evidence.

**Auto-proceed:** NO. Release boundary/freeze is Coordinator-owned.

## Queue depth / stop boundary

Objectives A–C are now implemented and automated-acceptance complete. Objective D is the only authorized remaining objective. Once D's release-candidate gates are complete, stop and mark `COORDINATOR REVIEW REQUESTED`; do not create release-control, tag, release, or unrelated roadmap work.

## User verification dependency

No user action is needed during hardening.

When Coordinator publishes a fixed release, target-machine verification should cover: update/reopen, Repair health, Generate 3D reaching provider inference, resize invariance, the compact UI tranche, and then the full Stage-C save/reopen/edit/cleanup/export path.
