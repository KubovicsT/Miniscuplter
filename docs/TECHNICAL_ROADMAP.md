# Miniscuplter Technical Roadmap

> Authoritative technical planning document owned by the Project Coordinator. The Project Charter and accepted Decisions define product truth; this roadmap defines technical sequencing, priority, architecture direction, and version scope. `HANDOFF.md` remains the immediate Dev Cycle baton.

Last coordinator review: 2026-09-10
Current stable release: `v1.0.22`
Stable release target: `c1ba2d01517cc6bca5a6e6cde3b1e85f853dd0d7`
Current development branch: `v1.0.23`
Latest live branch HEAD reviewed before this roadmap update: `5072e03688e4a1a101c458215e9df8a9fa288110`
Acceptance-weighted completion: **57%**

## 1. Current technical objective

Fix the reproduced v1.0.20 viewport-rendering/readability regression forward on v1.0.21, publish a narrow verified testable fix when release gates are satisfied, then resume the reference-machine Stage-C acceptance pass.

The shipped Stage-C foundation remains:

`2D source → accepted immutable image baseline → revision-bound local 3D generation → explicit candidate Apply → visible/editable Core-owned object → transform/sculpt → save/reload → immutable cleanup revision → exact validated STL export`

The current blocker is no longer architecture completion or release orchestration. It is **real target-machine viewport behavior (MS-009)**.

## 2. Technical-direction assessment

**Direction: PRESERVE, with immediate P0 viewport correction.**

The selective-refactor strategy remains correct. v1.0.20 proved the bounded Stage-C state/release architecture and the autonomous release path. New reference-machine evidence does not justify an architectural reset.

However, the viewport is not accepted. The user has now shown that v1.0.20 renders the grid and starter model, which is a major improvement over the historical completely blank viewport, but the resting render is still materially wrong:

- the grid/floor appears very dark blue/gray in the normal resting layout;
- while the right-side panel divider is actively dragged, the grid temporarily renders correctly;
- after normal layout settles, the incorrect appearance returns;
- the model is too dark to inspect surface form/details;
- the user explicitly wants a Blender-like neutral gray viewport/model readability scheme.

This is concrete target-machine evidence. **MS-009 is reopened and becomes the immediate P0 ahead of the rest of Stage-C qualification.**

## 3. Root-cause direction for MS-009

The exact root cause must be verified by the Dev Cycle, but repository inspection exposes a high-confidence architectural seam that matches the resize-dependent symptom:

1. `Main.V1019ViewportPipeline.cs` declares the native `SubViewportContainer` with `Stretch=true` authoritative and comments that the container should own child viewport dimensions.
2. Legacy paths still write `SubViewport.Size`:
   - `V1017SyncViewport()` writes it and is called by a recurring timer and from the v1.0.19 repair path;
   - `Main.V109Responsive.cs` also writes it after host resize.
3. `Main.V1019ViewportPipeline.cs` additionally schedules a **delayed full repair** 0.25 s after `host.Resized`. During continuous splitter movement that timer is repeatedly restarted, which strongly correlates with the user's observation that the viewport looks correct only while the divider is moving and changes after resizing settles.
4. World ownership is also overlapping: v1.0.17 performs a one-time reparent after enabling `OwnWorld3D`, while v1.0.19 later assigns a new `World3D` and may not force the already-parented world/lights through the same re-registration path. The very dark lit model makes this seam a priority to verify.
5. The current palette itself is too dark/blue for the requested UX: the v1.0.19 floor is near-black blue-gray and the background is very dark. Even after render ownership is fixed, visual hierarchy should be moved toward a Blender-style neutral solid viewport.

Treat these as **evidence-backed hypotheses**, not already-proven causes. Fix the ownership conflict, then verify on a real rendered frame.

## 4. Current milestone — reference-machine Stage-C acceptance

Stage-C acceptance remains the milestone, but it is temporarily gated by the P0 viewport defect.

Acceptance still requires:

1. **Viewport / MS-009**
   - grid, axes, model, selection and gizmo are visible immediately after launch without resizing;
   - the resting frame is visually stable before, during and after splitter resizing;
   - model shading makes curvature/details readable;
   - viewport colors use a Blender-like neutral-gray visual hierarchy rather than the current near-black blue floor;
   - repeated resize/tab-switch/manual-repair operations do not change world/material/grid state.

2. **Complete Stage-C flow / MS-018**
   - 2D input/import;
   - baseline acceptance;
   - one intended lightweight/default 3D route;
   - Apply into a visible/editable project-owned object;
   - transform + bounded sculpt/edit;
   - save/reload;
   - cleanup;
   - exact validated STL export.

3. **Storage / MS-013**
   - representative runtime/model/temp/cache paths remain within intended Miniscuplter-controlled locations.

4. **Provider / MS-022**
   - one intended lightweight/default 3D provider completes on GTX 1080 / 8 GB VRAM / 16 GB RAM with elapsed-time and practical resource evidence.

5. **Cancellation / MS-004**
   - cancellation/recovery does not corrupt project state or apply stale output.

## 5. Ordered critical path

### P0 — Fix deterministic viewport ownership and readability on v1.0.21

Dev Cycle should keep the fix narrow and evidence-driven:

1. make the v1.0.19 native viewport path the **single owner** of normal SubViewport sizing;
2. when the v1.0.19 pipeline is active, stop legacy v1.0.17/v1.0.9 code from independently assigning `SubViewport.Size`;
3. normal splitter resize should update layout/render target only; it should **not trigger a full world/camera/material repair after every resize** unless a proven Godot requirement demands it;
4. establish one coherent `World3D` ownership/rebind sequence so camera, lights, environment, grid and objects all live in the effective rendered world;
5. make repair logic idempotent and reserved for recovery, not the ordinary layout path;
6. adopt a Blender-like solid-workspace palette:
   - neutral dark gray background;
   - clearly visible neutral gray grid with stronger major lines;
   - colored axes;
   - light/mid neutral gray model material with readable studio-style lighting;
   - selection/gizmo remains obvious without turning the model into an unreadably dark or strongly tinted surface;
   - avoid a giant near-black/blue ground slab dominating the viewport;
7. preserve 2D canvas overlay behavior and existing Stage-C object authority.

### P1 — Add regression evidence that targets this failure mode

Automated/headless checks cannot substitute for the GTX 1080 render, but they should prevent obvious recurrence:

- test/extract the authoritative viewport configuration so there is only one active resize-size owner;
- verify resize does not run destructive world recreation/reset logic;
- verify world/camera/light/environment/grid ownership is stable and idempotent;
- keep render diagnostics useful: report active world/camera/light/grid state and sampled frame contrast/luminance where practical;
- run C#, Core, Python/geometry, release-audit and packaging validation after the UI fix.

### P2 — Publish and retest the narrow v1.0.21 viewport fix

When the fix is coherent and release gates are genuinely green, v1.0.21 should be a meaningful testable forward-fix release. Do **not** wait for the entire remaining Stage-C acceptance matrix if the user needs the immutable build to verify this target-only rendering defect.

The retest must explicitly compare:

- launch without touching the divider;
- active divider drag;
- after divider release/settle;
- tab changes;
- manual viewport repair;
- starter sphere and an imported/generated mesh.

MS-009 remains open until that real-machine retest is green.

### P3 — Resume remaining Stage-C acceptance

After viewport acceptance, continue MS-018 / MS-013 / MS-022 / MS-004 in the same reference-machine session where practical.

### P4 — Post-acceptance structural work

Unless new evidence changes priority:

1. `MS-020` durable job ownership/queue/resource/crash recovery;
2. `MS-019` outward migration from the proven Stage-C seam;
3. provider-tier policy from measured `MS-022` results;
4. broader Stage-D practical editing.

## 6. Issue priority

1. **MS-009 — P0 / Critical / REOPENED:** rendered but unstable/unreadable viewport on v1.0.20; resize-dependent visual state and very dark model.
2. **MS-018 — Critical milestone:** resume full Stage-C target qualification immediately after the viewport blocker.
3. **MS-013 — High:** storage containment verification.
4. **MS-022 — High:** qualify one intended lightweight/default 3D route.
5. **MS-004 — High acceptance companion:** cancellation/recovery.
6. **MS-019 / MS-020 — High architecture work:** still sequenced after acceptance unless a concrete blocker requires earlier work.

## 7. Architecture and ownership boundaries

- **Core C#:** durable project/object/revision/transform/history authority.
- **Godot viewport:** presentation and interaction only; for the render surface itself there must be one effective owner for SubViewport sizing, World3D, camera/environment/lights and resize lifecycle.
- **Python:** inference/geometry/provider execution, not project-state authority.
- **Launcher/updater:** delivery/runtime/data preservation.
- **release-control:** exact-SHA gated publication.
- **STL:** export/interchange only.

The viewport fix should remove duplicate presentation-state ownership rather than add another compatibility overlay.

## 8. v1.0.21 intended scope

v1.0.21 is now specifically the **MS-009 target-machine viewport correction + acceptance-support release**.

In scope:
- deterministic native viewport resize/world ownership;
- Blender-like readable viewport palette/lighting;
- targeted diagnostics/regression coverage for this failure;
- any directly coupled viewport/gizmo fix exposed by the same root cause;
- accurate MS-009 acceptance documentation.

Not in scope unless new target evidence makes it blocking:
- full sculpt migration;
- broad `Main.V*.cs` removal;
- full Job Broker rewrite;
- Rig & Pose modernization;
- kitbash expansion;
- provider proliferation;
- full UI rewrite;
- unrelated cleanup feature breadth.

## 9. Technical risks

1. Fixing only colors while leaving duplicate resize/world ownership would mask the root problem.
2. Fixing only the resize timing while retaining broken lighting would leave the model unusable.
3. Another additive viewport layer would increase duplicate authority and is explicitly discouraged.
4. CI cannot prove the final GTX 1080 appearance.
5. v1.0.20 is immutable; all fixes stay on v1.0.21+.
6. Do not let this bounded UI regression trigger a general UI redesign.

## 10. Next Coordinator-level objectives for Dev Cycle

1. reproduce/trace the post-resize state transition from the user evidence;
2. collapse normal viewport sizing to one authoritative owner;
3. collapse World3D/camera/light/environment repair to one coherent idempotent path;
4. implement the Blender-like neutral-gray viewport/material/lighting scheme requested by the user;
5. validate launch/resize/settle/tab-switch/repair invariants and preserve Stage-C behavior;
6. publish v1.0.21 when that narrow fix is release-ready;
7. request/rely on a real-machine retest before resolving MS-009;
8. resume the rest of Stage-C acceptance after viewport verification.

## 11. User input

No product-level decision is required. The color-direction request is clear enough to implement: use Blender-like neutral-gray readability rather than the current dark blue/near-black presentation.

The user can continue reporting additional v1.0.20 findings; any data-loss, storage-safety, crash, or complete-workflow blocker may reprioritize the immediate fix list.


## 12. User-directed reference-machine addendum — 2026-09-10

This addendum records new user acceptance evidence received after the previous Coordinator checkpoint. It is authoritative product/test input for the next Coordinator reconciliation and Dev Cycle; it does not erase the prior roadmap history.

### Confirmed correctness blocker

**MS-023** now precedes further long Stage-C qualification work. A successful Hunyuan-mini generation appeared in the live viewport while the Stage-C panel said `3D candidate: none`; after closing/reopening Miniscuplter the persisted 2D image returned but the 3D model was gone. Code inspection shows duplicate legacy and Stage-C Generate-button handlers can produce exactly this behavior. v1.0.22 must restore one authoritative Stage-C generation owner and prove generate → candidate → Apply → save/restart restore before asking for another expensive target-machine run.

### Viewport / responsive acceptance additions

- Whole-window resizing must not leave the black seams/gaps shown in the user's v1.0.20 screenshot (**MS-024**).
- The grid/ground must provide spatial reference without behaving as an opaque slab that hides part of the generated mesh. Blender-like non-occluding grid semantics are part of **MS-009** acceptance.
- Remove the automatic oversized Starter sphere (**MS-025**). New projects should be empty and the first real object should be selected/framed automatically. Provider units/scale should be handled deliberately rather than by a starter primitive.

### Planned resource telemetry

The user explicitly requested in-app resource graphs because long AI runs are currently monitored through Task Manager. Track as **MS-026**.

Planned panel:
- rolling GPU utilization;
- dedicated VRAM use;
- system RAM use;
- GPU temperature;
- secondary CPU use where useful/available;
- provider, current job stage and elapsed time;
- compact post-job observed peak summary.

Implementation constraints:
- local-only;
- low overhead (approximately 1 Hz is sufficient);
- degrade gracefully when a sensor/API is unavailable;
- do not invent unsupported metrics;
- telemetry must not materially slow inference.

This feature is especially valuable as instrumentation for **MS-022** provider qualification and future evidence-based Fast/Balanced/Quality routing. It is planned now but should not delay MS-023/MS-009 correctness work.

### First Hunyuan reference-machine evidence

A successful Hunyuan-mini run completed in roughly **402 s** on the GTX 1080 reference PC. The supplied Task Manager snapshot showed approximately **97% GPU utilization**, **5.4/8.0 GB dedicated VRAM**, **5.0/15.9 GB system RAM used**, and **73 °C GPU temperature**. These are observed snapshot values, not measured peaks. They are sufficient to preserve as useful MS-022 evidence while the application-side telemetry feature is built later.


### v1.0.21 viewport retest outcome

Reference-machine verification now provides a split result:

- **PASS:** the initial 3D viewport presentation is finally good/readable on the target PC.
- **FAIL:** resizing the right-side AI panel still changes viewport color.
- **FAIL:** whole-window resize still exposes black seams/gaps (MS-024).

Strategic consequence: do not reopen the whole viewport architecture. Preserve the v1.0.21 single-size-owner improvements and remove the remaining **presentation/layout duplicate authority**. In particular, legacy `V1017RepairViewport()` still mutates environment colors/ambient settings despite the native v1.0.19+ pipeline; it must not remain an independent presentation owner. Separately fix the root/outer layout so the client area is always fully covered after window resizing.

Priority remains: MS-023 persistence correctness first, then these narrowly reproduced MS-009/MS-024 residuals, then resume full Stage-C acceptance.


## 13. Opportunistic UI modernization workstream — MS-027

The user has approved a substantial workspace/UI modernization direction. It is **planned and actionable**, but it is not allowed to consume time while a higher-priority correctness/acceptance item is available.

### When Dev Cycle may work on MS-027

Dev Cycle may choose a bounded MS-027 slice when:
- the current critical-path item is waiting on user/reference-machine verification;
- a required user decision/input is unavailable;
- or there is genuinely no higher-priority unblocked engineering item.

A reproduced persistence, data-loss, viewport, release, provider, cancellation or Stage-C workflow blocker immediately preempts MS-027 work.

### UX target

The viewport should dominate the application. Replace today's text-heavy/fixed-form composition with a compact resizable modeling workspace:

- icon-based direct viewport tools instead of a dropdown;
- camera-linked clickable view cube with face/edge/corner snapping and selection-centered orbit;
- smaller default typography plus persisted user-adjustable UI/font scale;
- hover tooltips instead of persistent explanatory paragraphs;
- collapsible scene tree synchronized with viewport selection;
- performance/resources panel implementing MS-026;
- unified AI command console with history, keyboard/button navigation and clearly named contextual AI actions;
- all major panel sizes/layout/collapse state persisted across restart.

### Architecture constraints

- Do not create a second AI command ownership path; all UI actions converge on one authoritative dispatcher.
- Scene hierarchy reflects durable project identity rather than inventing parallel state.
- Workspace persistence is UI preference state, separate from project/model state.
- Avoid another additive version-overlay architecture. Prefer consolidating current UI ownership as each bounded slice is migrated.
- Each slice must preserve Stage-C generation/persistence/edit/export behavior and remain individually releasable/testable.

### Suggested opportunistic slice order

1. workspace-layout persistence + UI/font scale + shared tooltip behavior;
2. direct icon tool strip;
3. scene hierarchy tree;
4. view cube + selected-object orbit pivot;
5. unified AI command console/history/dispatcher;
6. MS-026 telemetry graphs;
7. density/spacing cleanup and retirement of superseded explanatory UI.

The full acceptance definition lives in MS-027.
