# Miniscuplter Handoff

> Immediate execution baton for the next Dev Cycle. Inspect actual Git/release/CI first. `docs/TECHNICAL_ROADMAP.md` owns medium/long-horizon technical direction; this file owns the next implementation step.

Last updated: 2026-09-10

## Current state

- **Repository:** `KubovicsT/Miniscuplter`
- **Latest published stable:** `v1.0.20`
- **Stable release target commit:** `e63cb0601cdbb91af5be191458ecdbcb7c0b7944`
- **Current development branch:** `v1.0.21`
- **v1.0.21 base:** exact published v1.0.20 target
- **Overall completion:** **58% acceptance-weighted**
- **Immediate P0:** `MS-009` viewport target-machine regression

## New user/reference-machine evidence

Released v1.0.20 is no longer completely blank: the grid/floor and starter 3D model render.

But the viewport is still not accepted:
- normal/resting grid/floor is very dark blue/gray;
- while actively dragging the divider at the left edge of the AI/right-side panel, the grid temporarily appears correct;
- the model is too dark to inspect detail;
- user requests the same general color/readability scheme as Blender: neutral gray viewport, visible grid, readable gray model/studio lighting.

This reopens MS-009 and outranks the remaining Stage-C acceptance checklist.

## Code seam already identified by Coordinator

Investigate and prove, do not blindly patch symptoms:

1. `Main.V1019ViewportPipeline.cs` says native `SubViewportContainer.Stretch=true` owns child viewport dimensions.
2. `V1017SyncViewport()` still writes `SubViewport.Size` and runs periodically.
3. `Main.V109Responsive.cs` also writes `SubViewport.Size` after resize.
4. v1.0.19 queues a delayed full `V1019RepairViewportPipeline()` 0.25 s after host resize. Continuous dragging postpones this repair, which strongly matches "correct while dragging, wrong after settle".
5. v1.0.17 and v1.0.19 overlap `OwnWorld3D` / explicit `World3D` creation/reparent behavior. Verify that the effective rendered World3D actually contains the existing camera, lights, environment, grid and objects.

## Exact next task

1. Reconcile exact HEAD/CI before editing.
2. Fix MS-009 forward on v1.0.21 by establishing **one normal viewport ownership path**:
   - one resize-size owner;
   - one World3D/camera/light/environment ownership/rebind sequence;
   - no routine full repair after every ordinary splitter resize unless proven necessary.
3. Make legacy v1.0.17/v1.0.9 size sync paths no-op/defer when the v1.0.19 native pipeline is authoritative instead of fighting `Stretch=true`.
4. Make the viewport repair operation idempotent and recovery-only; normal resizing should not recreate/rebind/reset rendering state.
5. Apply Blender-like readability:
   - neutral dark gray viewport background;
   - visible neutral minor/major grid;
   - clear axis colors;
   - light/mid neutral-gray model material;
   - neutral studio-like key/fill/ambient lighting so curvature/details are obvious;
   - avoid the current near-black blue floor slab dominating the view.
6. Preserve Stage-C Core state ownership and the 2D canvas overlay.
7. Add targeted regression/diagnostic coverage for:
   - launch state;
   - resize during drag vs after settle;
   - stable World3D/camera/light/grid ownership;
   - tab switching;
   - manual viewport repair;
   - render-frame contrast where practical.
8. Run strongest relevant C#/Core/Python/geometry/release-audit/package validation.
9. When this narrow fix is coherent and release gates are green, publish v1.0.21 through the autonomous release-control path as a meaningful test build.
10. Keep MS-009 open until the user retests the immutable v1.0.21 build on the reference PC.

## Current priority order

1. **MS-009** — immediate P0 target-machine viewport fix.
2. **MS-018** — full Stage-C qualification after viewport retest.
3. **MS-013** — storage containment verification.
4. **MS-022** — reference-hardware 3D provider qualification.
5. **MS-004** — cancellation/recovery.
6. **MS-019 / MS-020** — broader architecture after acceptance unless evidence makes one a blocker.

## Explicit non-priorities

Do not turn this into:
- a full UI rewrite;
- another viewport overlay;
- full sculpt migration;
- broad legacy deletion;
- full Job Broker reconstruction;
- Rig & Pose work;
- kitbash/provider expansion.

## Release policy

v1.0.20 is immutable. Fix forward only on v1.0.21+. Dev Cycle owns release readiness. A narrow v1.0.21 viewport-fix release is appropriate once its intended scope is coherent and all required release gates pass because the real rendering defect needs an immutable target-machine test build.

## User input

No product/design decision is currently required. The user may continue supplying additional v1.0.20 findings; severe new evidence can supersede this baton according to normal priority rules.
