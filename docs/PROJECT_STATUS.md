# Miniscuplter Project Status

> Fast-moving project dashboard. Inspect actual Git/release/CI state before trusting this file.

Last reconciled: 2026-09-10

## Current release / development state

- **Latest published stable release:** `v1.0.20`
- **Stable release target commit:** `e63cb0601cdbb91af5be191458ecdbcb7c0b7944`
- **Current development branch:** `v1.0.21`
- **v1.0.21 branch base:** exact published v1.0.20 target
- **Overall completion:** **58% acceptance-weighted**

v1.0.20 is published and immutable. All fixes belong on v1.0.21+.

## Current phase

The bounded Stage-C foundation shipped in v1.0.20:

`accepted 2D baseline → revision-bound 3D candidate → explicit Apply → Core-authoritative transform → one immutable sculpt/edit commit → save/reload → revision-bound cleanup → exact validated STL export`

Real reference-machine acceptance is underway.

## New v1.0.20 target-machine finding

**MS-009 has been reopened and is the immediate P0.**

The user's v1.0.20 screenshot confirms important progress: the grid/floor and starter 3D model now render instead of the historical fully blank viewport. But the viewport is not yet usable enough for acceptance:

- normal/resting grid/floor appears very dark blue/gray;
- the grid temporarily appears correct while the right-side panel divider is actively moved;
- the model is too dark to inspect details;
- the requested visual target is a Blender-like neutral-gray viewport with readable model shading.

Repository inspection points to overlapping resize/world ownership as the likely seam: the native v1.0.19 viewport says Stretch owns sizing, while legacy paths still assign SubViewport.Size, and a delayed full repair runs after resize. World3D ownership/rebind logic is also duplicated. Root cause must be proven by the implementation fix rather than assumed.

## Current priorities

1. **MS-009 — P0:** deterministic/readable viewport on v1.0.21; single resize/world owner + Blender-like palette/lighting.
2. **MS-018:** resume full Stage-C target qualification after the viewport blocker.
3. **MS-013:** storage containment verification.
4. **MS-022:** qualify one intended lightweight/default 3D provider on GTX 1080 / 16 GB.
5. **MS-004:** cancellation/recovery during a real job.
6. **MS-019 / MS-020:** broader architecture remains sequenced after acceptance unless concrete evidence changes the dependency order.

## Immediate engineering priority

Fix MS-009 forward on v1.0.21 without adding another viewport overlay or broad UI rewrite.

Required result:
- correct appearance immediately after launch;
- no visual state change caused by splitter resize/settle;
- readable neutral-gray model shading;
- clearly visible grid/axes;
- stable tab-switch/manual-repair behavior;
- existing 2D canvas and Stage-C object authority preserved.

When the narrow fix is coherent and all release gates pass, v1.0.21 may be published as the immutable test build needed for real-machine verification.

## User input currently required

No additional product decision is required. The user should continue reporting v1.0.20 findings; after v1.0.21 is published, the viewport fix specifically needs real-machine verification before MS-009 can be resolved.
