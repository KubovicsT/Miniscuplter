# Miniscuplter Coordinator Log

> Durable Project Coordinator memory. Read completely on every Coordinator run. Preserve strategic decisions, rejected directions, evidence, and outcomes so the project does not oscillate without new evidence.

## 2026-09-10 — Initial substantive Coordinator review

### Review checkpoint

- Stable release: `v1.0.19`
- Stable application commit: `52f3b95fb6addc0f9f1e7123b75068da4ef1513c`
- Development branch: `v1.0.20`
- Latest fully validated application commit reviewed: `dfedbf533e0adb3b7712fe8e18e0a7d901b7929b`
- Acceptance-weighted completion: 57%.

### Strategic assessment

Preserve the selective-migration direction. Finish the Stage-C state-authority gap before broadening: transactional mapped-object transforms, one bounded immutable mesh-edit path, and regression coverage through save/reload/undo/cleanup/export. Keep v1.0.20 bounded and publish it as the testable increment before target-machine acceptance. No product-level user decision was required.

### Durable decisions

- Core owns durable project state; Godot projects migrated state; Python executes inference/geometry; STL is interchange/export only.
- v1.0.20 must not absorb full sculpt migration, full Job Broker durability, Rig/Pose migration, kitbash migration, provider proliferation, global legacy removal, or a full UI rewrite.
- Target-machine acceptance remains required for Stage-C acceptance, but publication of a verified installable build may precede that evidence so the user can test the exact immutable build.
- Any target-machine defect is fixed forward in the next semantic version.

---

## 2026-09-10 — v1.0.20 release-orchestration checkpoint

### No-race / repository checkpoint

- Latest Dev Cycle had completed before this review.
- Latest published stable remained `v1.0.19`.
- Development branch remained `v1.0.20`.
- Live v1.0.20 HEAD observed before Coordinator documentation changes: `6ead6ae2f3c2823b7b3b93f2fae805e6808f6f57`.
- Validated application/release-candidate code: `bc3106d606b4450aa5cb9d4395b77a5d6e78f11a`.
- Acceptance-weighted completion: **58%**.

### Dev Cycle trajectory assessment

The Dev Cycle followed the Coordinator roadmap successfully. It completed the bounded v1.0.20 application target rather than broadening:

- mapped move/rotate/scale/ground and gizmo commits now update durable Core transform state;
- save/reload and export use durable transform state;
- Stage-C undo/redo uses project transactions;
- one bounded sculpt/edit path creates immutable child mesh revisions with stale-parent protection;
- restore, cleanup and export remain aligned to object/revision authority;
- regression coverage was added for transform persistence, sculpt lineage, undo/redo, save/reload and export interactions.

This is evidence of architectural convergence, not churn. The previous P0 state-authority gap is sufficiently complete for the bounded release candidate.

### Autonomous release-control assessment

The permanent `release-control` mechanism is strategically acceptable. It preserves separation between a Dev Cycle release decision and an independently gated exact-SHA Windows build/export/smoke/publication process. Keep the historical explicit-tag workflow as fallback; do not weaken release safety merely for automation convenience.

Two bootstrap defects were observed:

1. request discovery initially examined only the triggering commit rather than the full push range;
2. an expected failing `gh release view` probe left `$LASTEXITCODE` nonzero after otherwise-successful validation.

Both were classified as release-orchestration defects rather than source-candidate defects. The Dev Cycle was directed to repair them without reopening v1.0.20 application scope or weakening gates.

### Priority change

Previous P0 (`MS-019` bounded transform + one mesh-edit authority) was complete enough for the release candidate. Release orchestration became immediate P0 in service of `MS-018` publication/qualification.

---

## 2026-09-10 — v1.0.20 published / target-acceptance checkpoint

### No-race and repository checkpoint

- The latest Dev Cycle completed before substantive review.
- `v1.0.20` is now the published stable release.
- Published target: `e63cb0601cdbb91af5be191458ecdbcb7c0b7944`.
- Autonomous-release run `34508060099` completed successfully through exact-SHA validation, C#/Core, Python/runtime, job/geometry regressions, release audit, verified Godot 4.7.2 Windows export, output/hash verification, installer creation, silent installer smoke-install, immutable-target recheck, tag creation and GitHub Release publication.
- Forward-only development branch `v1.0.21` exists from the exact published target.
- v1.0.21 HEAD reviewed before Coordinator roadmap update: `214b890b3c46be4ef7451c5c9a8fd60f03eee10f`.
- Exact-head v1.0.21 Core, C#, Python/geometry/release-audit and packaging jobs are green. Full Windows-release/publication jobs were correctly skipped for an ordinary branch push.
- Acceptance-weighted completion remains **58%** because release publication does not substitute for target-machine acceptance.

### Outcome of previous direction

The Dev Cycle followed the roadmap successfully:

- it fixed the release-controller `$LASTEXITCODE` defect at the release-control boundary;
- it did not reopen or broaden v1.0.20 application scope;
- it preserved exact-SHA, build/export/hash/smoke and immutability gates;
- it drove the controller to a successful real Windows release;
- it preserved v1.0.20 immutability and moved forward to v1.0.21.

This validates both the bounded-release sequencing and the permanent autonomous release-control architecture. The explicit-tag release path remains fallback only.

### Strategic assessment

**Preserve the selective-refactor direction.** No architectural reset is warranted.

The critical path has now moved from architecture implementation and release orchestration to **real reference-machine acceptance**. The correct response is not to immediately begin another broad migration. The project needs runtime evidence on the intended GTX 1080 / 8 GB VRAM / 16 GB RAM machine before deciding which structural work should follow.

### Priority decision

Current order:

1. `MS-018` — complete the released v1.0.20 Stage-C flow on the reference machine.
2. `MS-009` — verify viewport/grid/model/gizmo; reproduced blank viewport becomes immediate P0.
3. `MS-013` — verify storage containment; severe leakage/safety regression becomes P0.
4. `MS-022` — qualify one intended lightweight/default 3D provider with elapsed time and RAM/VRAM evidence.
5. `MS-004` — cancellation/recovery during a real job where practical.
6. broader `MS-019` / `MS-020` work waits behind acceptance unless a concrete runtime blocker requires it.

### v1.0.21 scope decision

Until target-machine evidence arrives, v1.0.21 is primarily a forward-fix/acceptance-support branch.

Allowed work:
- fixes for reproduced v1.0.20 defects;
- diagnostics directly needed to establish acceptance evidence;
- regressions for observed failures;
- accurate acceptance documentation.

Do not use scheduled-cycle availability as a reason to start full sculpt migration, broad legacy removal, full Job Broker reconstruction, Rig/Pose, kitbash, provider proliferation, UI rewrite, or optional cleanup breadth.

If target-machine acceptance is green, the next Coordinator review should sequence the next structural milestone. Current default ordering after acceptance is `MS-020` durable job ownership/queue/crash recovery, then `MS-019` outward migration from the proven Stage-C seam, provider-tier decisions from measured `MS-022` evidence, then broader Stage-D practical editing work.

### Risks to monitor

- treating CI/release success as real-machine acceptance;
- Dev Cycles drifting into generalized infrastructure while waiting for user evidence;
- v1.0.21 becoming a broad migration branch before acceptance;
- compatibility bridges becoming long-term duplicate authority;
- provider policy being based on theoretical support instead of measured reference-machine behavior.

### User dependency

No product-level decision is required. The important dependency is now real testing of released v1.0.20 on the reference machine. Concrete pass/fail observations should drive v1.0.21 fixes. If testing exposes a difficult-to-reverse product tradeoff, escalate that decision to the user rather than embedding it in an implementation fix.


---

## 2026-09-10 — v1.0.20 viewport target evidence / MS-009 reopened

### No-race and repository checkpoint

- The most recent Dev Cycle had completed before this review.
- Stable release remains `v1.0.20` at `e63cb0601cdbb91af5be191458ecdbcb7c0b7944`.
- Development branch is `v1.0.21`.
- Live branch HEAD before this Coordinator update was `5072e03688e4a1a101c458215e9df8a9fa288110`.
- The latest exact-head branch validation before this user evidence was green for Core, C#, Python/geometry/release-audit and packaging; ordinary branch pushes correctly skipped full release/publication jobs.
- Acceptance-weighted completion remains **58%**.

### New reference-machine evidence

The user supplied a screenshot from released v1.0.20 and a precise resize observation:

- the historical blank viewport is materially improved: the grid/floor and starter 3D model now render;
- in the normal resting layout, the floor/grid presentation is dark blue/gray and the model is too dark to inspect;
- while the left edge/divider of the AI side panel is actively dragged, the grid temporarily appears correct;
- this makes MS-009 a reproduced target-machine defect rather than "fixed, needs verification";
- the user explicitly requested a Blender-like viewport/model color scheme.

### Architectural interpretation

Repository inspection found overlapping presentation authority that matches the symptom:

- v1.0.19 declares `SubViewportContainer.Stretch` the native sizing contract;
- `V1017SyncViewport()` still assigns `SubViewport.Size` and runs periodically;
- `Main.V109Responsive.cs` also assigns `SubViewport.Size` after resize;
- v1.0.19 queues a delayed full `V1019RepairViewportPipeline()` 0.25 s after every resize; continuous divider movement repeatedly postpones that callback, which correlates strongly with the user's "looks right only while dragging" report;
- v1.0.17 and v1.0.19 also overlap `OwnWorld3D` / explicit `World3D` creation/rebind behavior;
- the very dark lit object makes loss/mismatch of effective lighting/world registration a serious hypothesis;
- v1.0.19's near-black blue-gray floor/background is also not the requested visual hierarchy even if ownership is fixed.

These are strong hypotheses, not yet a claimed root-cause proof.

### Priority change

**MS-009 is reopened and promoted to immediate P0.**

The remaining Stage-C acceptance work (MS-018/MS-013/MS-022/MS-004) stays important, but there is little value qualifying generation/edit/export usability while the core inspection viewport is unstable/unreadable.

### Coordinator direction

v1.0.21 should remain narrow:

1. make the native viewport path the only normal resize-size owner;
2. stop full world/camera/material repair from running as a routine post-resize side effect;
3. establish one coherent World3D/light/environment/camera ownership/rebind contract;
4. apply a Blender-like neutral dark-gray background, visible neutral grid, colored axes and light neutral-gray model with readable studio lighting;
5. validate invariance before/during/after splitter resize and across tab changes/manual repair;
6. publish a narrow v1.0.21 test build when release gates are satisfied;
7. keep MS-009 open until user/reference-machine retest passes.

Do not answer this failure by adding another render overlay or by broad UI refactor.

### User decision

No additional decision is required. The requested Blender-like visual direction is sufficiently specific for implementation-level color/lighting choices.


---

## 2026-09-10 — v1.0.21 viewport partial-pass verification

### User evidence

The user retested released v1.0.21 on the reference Windows/GTX 1080 machine.

Observed:
- initial 3D viewport now looks good and is materially more readable;
- resizing the right-side AI panel still changes viewport color;
- whole-window resize still leaves black seams/gaps.

### Coordinator interpretation

This is a **partial success**, not a failed overall direction. v1.0.21 proved the new native viewport sizing/lighting/palette direction improves the baseline. The remaining failures are narrower state-ownership/layout defects.

Repository inspection shows one residual duplicate presentation owner: legacy `V1017RepairViewport()` still writes the old dark environment/ambient settings and remains reachable from historical paths even when the v1.0.19+ native pipeline is installed. This must be neutralized/delegated in v1.0.22. The exact right-panel resize trigger must still be traced before claiming it is the sole cause.

MS-024 is now confirmed independently of the SubViewport fix and should be treated as outer/root responsive-layout failure. Inspect root Control/client-area fill behavior rather than reviving competing SubViewport sizing.

### Priority

MS-023 remains immediate P0 because successful generated 3D work can disappear after restart. MS-009/MS-024 are the next narrow correctness/usability fixes. No broad viewport rewrite is warranted.

No user product decision is required.


---

## 2026-09-10 — User-directed modular UI modernization plan

### Release/no-race checkpoint

- v1.0.22 autonomous release completed successfully at `c1ba2d01517cc6bca5a6e6cde3b1e85f853dd0d7`.
- A forward-only v1.0.23 planning/development branch was created from that exact published target.
- The UI plan was intentionally not written while the 21:00 Dev Cycle or v1.0.22 release freeze was active.

### User direction accepted

The user provided an annotated workspace layout and specified:
- direct icon buttons for tools rather than dropdown;
- interactive camera-linked view cube with face/edge/corner snapping and selection-centered orbit;
- substantially less always-visible explanatory text, with hover help instead;
- smaller typography plus in-app font/UI scaling;
- collapsible scene tree for all entities;
- performance/resource graphs;
- unified AI command line/history with previous-command navigation and action buttons for real AI operations;
- resizable regions and persistence of the user's workspace arrangement across restarts.

### Coordinator sequencing decision

Track the full request as **MS-027** and integrate MS-026 as its performance-panel component.

MS-027 is an **opportunistic secondary workstream**, not a new P0. Dev Cycle may take bounded UI slices when acceptance/correctness work is blocked on user input/testing or no higher-priority unblocked task exists. Any newly reproduced high-severity issue preempts UI modernization immediately.

Implement incrementally and avoid another legacy overlay. The AI console must converge commands/actions on one authoritative dispatcher specifically to avoid repeating the duplicate-generation-handler failure found in MS-023.

No further user product decision is required for this initial UI direction.


---

## 2026-09-10 — v1.0.23 direct-tool-strip review and red-build correction

### No-race / repository checkpoint

- A newer repository/CI transition began during this Coordinator review, so the first attempted planning write was aborted.
- The Coordinator waited for exact-head CI to finish before mutating planning state.
- Latest stable remains `v1.0.22` at `c1ba2d01517cc6bca5a6e6cde3b1e85f853dd0d7`.
- Development branch is `v1.0.23`.
- Exact branch HEAD reviewed before this write: `549d1018866e242a3b0d5344f6840903a36fa6f6`.
- Exact-head Core-foundation, Python/runtime/geometry/release-audit and packaging passed; C# build failed.
- No active release freeze was present.
- Acceptance-weighted completion remains **57%**.

### Dev Cycle trajectory

The worker stayed within the approved MS-027 fallback while released-v1.0.22 target-machine evidence remained unavailable. It implemented the second bounded slice: direct Select / Move / Rotate / Scale / Sculpt controls reusing the existing V1018 tool state rather than adding a second tool/input state machine.

That functional direction remains aligned with the roadmap.

### New execution regression

Exact-head broader build is red. C# fails with `CS0122` because `ExtrasInstaller` calls `Main.InstallV1023ViewportToolStrip()` while the method is inaccessible at that call site.

Evidence:
- prior failing build run: `34524072795`;
- current exact-head failing build run: `34527580075`;
- current dotnet job repeats the same accessibility failure;
- other validation families remain green.

HANDOFF was originally written while CI was queued and correctly warned not to claim validation, but the completed result is now a known failed validation state.

### Direction decision

**PRESERVE** the selective-refactor architecture and Stage-C milestone.

**NARROW** the immediate execution sequence:
1. repair the compile regression;
2. restore green exact-head validation;
3. only then resume reference-machine acceptance or one bounded MS-027 fallback slice.

No architectural redesign is justified by this failure.

### UI architecture guard

The direct controls correctly reuse V1018 tool authority, but the new presentation was again delivered as a `Main.V1023...` overlay. This is acceptable only as a migration bridge. Future MS-027 work should prefer stable version-neutral UI components/services and must not normalize another `Main.V10xx...` layer per slice.

### User dependency

No new product decision is required. The reference-machine v1.0.22 acceptance pass remains the main external dependency once the branch build is restored.
