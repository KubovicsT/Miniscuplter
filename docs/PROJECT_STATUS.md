# Miniscuplter Project Status

> Fast-moving project dashboard. Inspect actual Git/release/CI state before trusting this file.

Last reconciled: 2026-09-11

## Current release / development state

- **Latest published stable release:** `v1.0.22`
- **Stable release target commit:** `c1ba2d01517cc6bca5a6e6cde3b1e85f853dd0d7`
- **Current development branch:** `v1.0.23`
- **Latest fully validated bounded code/test checkpoint before documentation commits:** `1665a9c9fabd8a3d0f1f4f8ed79411f749e158f6`
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
7. **Density / spacing cleanup** — version-neutral `Main.WorkspaceDensity.cs` shortens the empty 2D-workspace hint, retires two superseded always-visible instructional paragraphs, preserves their guidance in hover tooltips, compacts only the affected presentation sections, and deliberately leaves workflow/job status surfaces visible. It composes after the existing MS-027 workspace layers and owns no project, AI, viewport-tool, selection or persistence state.

Latest density/polish commits:
- `d5ec6aca9a06c7fbc7934aeaffafd0884bccaf89` — compact instructional workspace copy;
- `28df9b6882b02f9acb7b37647523d8c722833c3e` — compose final density layer;
- `1665a9c9fabd8a3d0f1f4f8ed79411f749e158f6` — add focused static regression guard source.

## Validation

Exact code/test checkpoint `1665a9c9fabd8a3d0f1f4f8ed79411f749e158f6` passed:

- Stage-B/Core workflow — `core-foundation` run `34549024074`: **PASS**;
- broader `build` workflow — run `34549024087`: **PASS**;
- C# editor/launcher/updater/Core build and Core tests: **PASS**;
- Python compile and dependency resolution: **PASS**;
- core/execution/job regressions: **PASS**;
- real geometry regressions: **PASS**;
- release audit: **PASS**;
- portable package build/layout and ZIP SHA-256 verification: **PASS**;
- Inno Setup installation and installer-definition compilation: **PASS**.

`tools/ui_density_tests.py` documents focused static ownership/presentation invariants for this bounded polish slice and is syntax-checked by the normal Python compile pass; the existing branch CI remains authoritative for the build/runtime regression families above.

Full Windows/Godot publication remains Coordinator-owned and was not initiated. This is a useful validated checkpoint, not a release decision or freeze.

## Current priorities

1. Consume any new reference-machine evidence from released v1.0.22 immediately; any serious correctness, persistence, viewport, data-safety, storage, cancellation or Stage-C regression preempts UI work.
2. Complete Stage-C acceptance when target-machine testing is available, including persistence, transform/sculpt, cleanup/export, storage, provider/resource and cancellation evidence.
3. The currently ordered MS-027 fallback sequence is now implemented through the bounded density/polish slice. Do not broaden into another UI feature merely to stay busy; wait for Coordinator direction or take only a clearly higher-value unblocked task already present in ROADMAP/HANDOFF.
4. Do not broaden into unrelated features or architecture while Stage-C acceptance remains the milestone.

## Release ownership / continuous development

Release ownership belongs exclusively to the Project Coordinator. Dev may record validated checkpoints and continue implementation; a checkpoint does not freeze the branch. Only an actual Coordinator release request freezes the source semantic-version branch. Dev does not create/update release requests, tags or GitHub Releases.

## User dependency

No product/design decision is required. The principal external dependency remains reference-machine verification of released v1.0.22, especially Generate 3D → candidate → Apply → save/close/reopen → same 3D object/revision, plus viewport resizing/presentation, starter removal, transform/sculpt, cleanup/export and storage behavior. The new MS-026 telemetry should also be observed during a real long AI job to verify values and overhead on the GTX 1080 machine.
