# Miniscuplter Project Status

> Fast-moving project dashboard. Inspect Git/release/CI before trusting this file blindly.

Last reconciled: 2026-09-10

## Current state

- **Latest published stable:** `v1.0.18` at application commit `ce2d876fc145e615d63bd8d9fc809610f6038301`.
- **Current development branch:** `v1.0.19`.
- **Current phase:** transition between Stage B (replacement foundation/migration harness) and Stage C (one reliable end-to-end thin slice).
- **Overall completion:** **56% acceptance-weighted**. Do not increase from code-only work without acceptance evidence.
- The current branch contains substantial post-v1.0.18 work in viewport reliability, storage containment, updater safety, project-store/history foundations, geometry/runtime contracts, and now provider readiness preflight.

## Acceptance-weighted workstreams

| Workstream | Weight | Completion | Basis |
|---|---:|---:|---|
| Application foundation/state/persistence | 15% | 50% | Stable IDs, project models/history/store and migration exist; legacy `Main.V*.cs` still owns large areas. |
| 2D workflow | 12% | 72% | Generation/import, center canvas, regional edit, Enhance and references exist; provenance/new-core integration remains. |
| 3D generation | 15% | 48% | Multiple providers/routing exist; runtime qualification and target-machine thin-slice proof remain incomplete. |
| 3D editing/sculpt/kitbash | 15% | 45% | Significant legacy functionality exists; replacement-state/history integration remains incomplete. |
| Rig & Pose | 10% | 40% | Historical rig/IK/pose exists; independent rest-state/new-core integration remains. |
| Cleanup/validation/export | 8% | 65% | Repair/remesh/thickness/validated STL plus geometry regressions exist. |
| AI runtime/provider reliability | 10% | 65% | Repair/CUDA/resumable installs/routing/progress are substantial; provider inference qualification remains incomplete. |
| UI/UX | 8% | 58% | Four-stage workflow and newer editing/settings/viewport surfaces exist; legacy composition/runtime defects remain. |
| Launcher/updater/release | 4% | 92% | Mature verified transactional updater/release flow. |
| Testing/hardware qualification | 3% | 50% | Strong CI/static/geometry tests; real CUDA/provider/GUI acceptance matrix incomplete. |

## Current high-priority issues

1. **MS-009 — 3D viewport/grid/model/gizmo reliability.** v1.0.19 has a repaired native viewport pipeline and deterministic sizing contract; still requires target-machine verification.
2. **MS-018 — Stage-C thin slice.** Must prove `2D → accepted baseline → qualified 3D → visible/editable mesh → save/reload → cleanup → validated STL` on GTX 1080/8 GB + 16 GB RAM.
3. **MS-013 — storage containment.** Significant containment code/tests exist; representative real-machine operations must confirm no important C:/AppData/TEMP leakage.
4. **MS-022 — provider qualification/self-tests.** Now actively advancing: 3D routing performs a lightweight readiness preflight before choosing/starting a provider.
5. **MS-020 — authoritative Job Broker/stale-result safety.** Current structured progress/job IDs are a bridge, not final ownership semantics.
6. **MS-019 — legacy `Main.V*.cs` migration.** Continue vertical-slice migration rather than adding overlapping compatibility owners.

## Latest engineering progress

The preceding run removed a concrete SubViewport sizing ownership conflict: v1.0.19 now uses one explicit host-to-render-target sizing contract instead of mixing `SubViewportContainer.Stretch=true` with legacy direct `SubViewport.Size` writes.

This run advances MS-022 with `ai_backend/provider_readiness.py` and readiness-aware 3D routing. Before a long generation begins, an explicitly selected local 3D provider must now be installed and pass lightweight dependency/device checks; Auto routing skips installed-but-unready providers and can choose the next ready provider. Routing status also exposes per-provider readiness details. Core routing tests cover the skip/fail-fast behavior. This is a preflight contract, not a claim that full inference will fit VRAM.

## Next milestone acceptance criteria

The next major milestone requires target-machine proof that:

- 2D generate/import/edit works and accepted baseline persists as a revision;
- one supported lightweight 3D provider passes readiness/self-test and generates;
- generated mesh is visible with grid/camera/selection/gizmo;
- cancellation then restart is reliable;
- save/reload and basic cleanup work;
- final STL validates;
- working files stay under the configured Miniscuplter data root;
- elapsed time and peak RAM/VRAM are recorded.

## Immediate engineering priority

1. Let CI validate the readiness-aware routing changes; fix any regression first.
2. Extend MS-022 from lightweight import/device readiness toward a bounded provider self-test for the preferred Stage-C lightweight route, without launching an expensive full production generation.
3. Continue MS-018 by binding accepted 2D baseline → 3D result/import to stable project/revision identity and stale-result protection.
4. Keep MS-009 and MS-013 open until a released build is tested on the real target machine.

## User input

No product/design decision currently blocks autonomous work. Real-machine verification remains necessary for runtime/UI acceptance issues.