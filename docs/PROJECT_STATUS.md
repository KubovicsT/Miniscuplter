# Miniscuplter Project Status

> Fast-moving project dashboard. Inspect actual Git/release/CI state before trusting this file.

Last reconciled: 2026-09-11

## Current release / development state

- **Latest published stable release:** `v1.0.22`
- **Stable release target commit:** `c1ba2d01517cc6bca5a6e6cde3b1e85f853dd0d7`
- **Current development branch:** `v1.0.23`
- **Latest fully validated bounded code candidate before documentation commits:** `4538ab0e3251bb84449318a1bd43d9986d94e6d9`
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
5. **Unified AI command/history/dispatcher** — new `Scripts/Main.AiCommandConsole.cs` adds one always-visible AI command line with Up/Down and previous/next history navigation plus clearly named action buttons. Typed slash commands delegate to the existing V096 command executor. AI actions delegate through one console dispatcher to the existing owners: `GenerateConcept`, `V096EditCommand`, `SmartSelectV096Async`, `V1020Generate3DAsync`, and `V098Detail3DAsync`. The console does not create another project state, provider runner, candidate store, or generation handler.

AI-console commits:
- `fc9d7aa411bdb386770e7cd075af3586d6ce88dd` — unified command/history/dispatch layer;
- `4538ab0e3251bb84449318a1bd43d9986d94e6d9` — final composition after the earlier MS-027 slices.

## Validation

Exact code checkpoint `4538ab0e3251bb84449318a1bd43d9986d94e6d9` passed:

- Stage-B/Core workflow — `core-foundation` run `34540497710`: **PASS**;
- C# editor/launcher/updater/Core build — `build` run `34540497715`, dotnet job: **PASS**;
- Python compile and dependency resolution: **PASS**;
- core/execution/job regressions: **PASS**;
- real geometry regressions: **PASS**;
- release audit: **PASS**;
- portable package build/layout and ZIP SHA-256 verification: **PASS**;
- Inno Setup installation and installer-definition compilation: **PASS**.

Full Windows/Godot release and publication jobs were correctly skipped for this ordinary development push. This is a useful validated checkpoint, not a release decision or freeze. Release ownership remains with the Coordinator and development continues until an explicit Coordinator release boundary is chosen.

## Current priorities

1. Consume any new reference-machine evidence from released v1.0.22 immediately; any serious correctness, persistence, viewport, data-safety, storage, cancellation or Stage-C regression preempts UI work.
2. Complete Stage-C acceptance when target-machine testing is available, including persistence, transform/sculpt, cleanup/export, storage, provider/resource and cancellation evidence.
3. If acceptance remains externally blocked and no higher-priority unblocked issue appears, the next Coordinator-approved fallback is **MS-026 local resource telemetry/performance panel**. Keep sampling low-overhead and local-only and do not broaden into density/polish in the same slice.
4. Density/spacing cleanup remains after telemetry and subordinate to Stage-C findings.

## Release ownership / continuous development

Release ownership belongs exclusively to the Project Coordinator. Dev may record validated checkpoints and continue implementation; a checkpoint does not freeze the branch. Only an actual Coordinator release request freezes the source semantic-version branch. Dev does not create/update release requests, tags or GitHub Releases.

## User dependency

No product/design decision is required. The principal external dependency remains reference-machine verification of released v1.0.22, especially Generate 3D → candidate → Apply → save/close/reopen → same 3D object/revision, plus viewport resizing/presentation, starter removal, transform/sculpt, cleanup/export and storage behavior.
