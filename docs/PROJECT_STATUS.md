# Miniscuplter Project Status

> Fast-moving project dashboard. Inspect actual Git/release/CI state before trusting this file.

Last reconciled: 2026-09-11

## Current release / development state

- **Latest published stable release:** `v1.0.22`
- **Stable release target commit:** `c1ba2d01517cc6bca5a6e6cde3b1e85f853dd0d7`
- **Current development branch:** `v1.0.23`
- **Latest fully validated bounded code candidate before documentation commits:** `e110a746a1dd9a470c7d05de80de684edfcf1f27`
- **Overall completion:** **57% acceptance-weighted**

v1.0.22 is published and immutable. v1.0.23 remains the writable forward development branch. There is no v1.0.23 release-control request or Coordinator release freeze.

## Current phase

The Coordinator-defined critical path remains **Stage-C target-machine acceptance**. Released v1.0.22 contains bounded fixes for the user-observed generation/persistence, viewport/client-fill and starter-scene issues. User-observed target-machine issues remain **FIXED - NEEDS USER VERIFICATION** where applicable until the released build is retested on the reference Windows / GTX 1080 machine.

The next critical evidence remains:

`accepted 2D baseline → local 3D generation → Ready/Conflict candidate → explicit Apply → save → close/reopen → same durable object/revision → transform/sculpt → cleanup → exact STL export`

plus resize/presentation, storage containment, provider/resource and cancellation/recovery checks.

## v1.0.23 bounded MS-027 progress

While Stage-C acceptance is externally blocked, Dev has advanced only the Coordinator-approved bounded UI slices:

1. **Workspace preferences / UI scale / tooltips** — controlled-root splitter/layout persistence, 75–135% UI scale and reusable tooltips.
2. **Direct viewport tools** — compact Select/Move/Rotate/Scale/Sculpt controls delegating to the existing V1018 tool/input owner.
3. **Synchronized scene hierarchy** — collapsible hierarchy reflecting existing scene objects and delegating selection to the existing selection owner.
4. **View cube + selected-object orbit pivot** — six orthographic snaps plus selected-object-centered orbit while retaining existing camera/input authority.
5. **Unified AI command/history/dispatcher** — one AI command line/history surface delegating to existing authoritative AI action owners, including Stage-C generation through `V1020Generate3DAsync`.
6. **MS-026 resource telemetry** — version-neutral `Main.ResourceTelemetry.cs` adds local ~1 Hz GPU/VRAM/RAM/app-CPU graphs, NVIDIA GPU temperature when available, active provider/job stage/elapsed time and observed per-job peaks. GPU values come from `nvidia-smi`, RAM from Windows physical-memory telemetry, and unsupported sensors are shown as unavailable rather than estimated. The panel observes existing job/provider state only and owns no inference or project state.

Telemetry commits:
- `6bc6d379a8db20f9f9a934ffc504f9cc64501904` — resource panel implementation;
- `0fd8b03db0f9cde3ea23021523c8e3cc3da5a455` — final composition; this exact head exposed a Godot C# source-generator requirement;
- `e110a746a1dd9a470c7d05de80de684edfcf1f27` — add required `partial` modifier to the nested Control and restore green validation.

## Validation

Exact code checkpoint `e110a746a1dd9a470c7d05de80de684edfcf1f27` passed:

- Stage-B/Core workflow — `core-foundation` run `34545123188`: **PASS**;
- broader `build` workflow — run `34545123110`: **PASS**;
- C# editor/launcher/updater/Core build and Core tests: **PASS**;
- Python compile and dependency resolution: **PASS**;
- core/execution/job regressions: **PASS**;
- real geometry regressions: **PASS**;
- release audit: **PASS**;
- portable package build/layout and ZIP SHA-256 verification: **PASS**;
- Inno Setup installation and installer-definition compilation: **PASS**.

Failed attempt preserved: `0fd8b03...` failed the editor C# build only because Godot requires classes deriving from `GodotObject` to be declared `partial` (`GD0001`). The narrow correction in `e110a746...` passed all branch validation gates.

Full Windows/Godot publication remains Coordinator-owned and was not initiated. This is a useful validated checkpoint, not a release decision or freeze.

## Current priorities

1. Consume any new reference-machine evidence from released v1.0.22 immediately; any serious correctness, persistence, viewport, data-safety, storage, cancellation or Stage-C regression preempts UI work.
2. Complete Stage-C acceptance when target-machine testing is available, including persistence, transform/sculpt, cleanup/export, storage, provider/resource and cancellation evidence.
3. If acceptance remains externally blocked and no higher-priority unblocked issue appears, the final currently ordered MS-027 fallback is **density/spacing cleanup and retirement of superseded explanatory UI**. Keep it bounded and presentation-only.
4. Do not broaden into unrelated features or architecture while Stage-C acceptance remains the milestone.

## Release ownership / continuous development

Release ownership belongs exclusively to the Project Coordinator. Dev may record validated checkpoints and continue implementation; a checkpoint does not freeze the branch. Only an actual Coordinator release request freezes the source semantic-version branch. Dev does not create/update release requests, tags or GitHub Releases.

## User dependency

No product/design decision is required. The principal external dependency remains reference-machine verification of released v1.0.22, especially Generate 3D → candidate → Apply → save/close/reopen → same 3D object/revision, plus viewport resizing/presentation, starter removal, transform/sculpt, cleanup/export and storage behavior. The new MS-026 telemetry should also be observed during a real long AI job to verify values and overhead on the GTX 1080 machine.
