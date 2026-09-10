# Miniscuplter Handoff

> Operational baton for the next development run. Inspect actual Git/release/CI state first; repository state wins over this document if they differ.

Last updated: 2026-09-10

## Current state

- **Repository:** `KubovicsT/Miniscuplter`
- **Latest published stable:** `v1.0.19`
- **Stable application commit:** `52f3b95fb6addc0f9f1e7123b75068da4ef1513c`
- **Current development branch:** `v1.0.20`
- **v1.0.20 base:** exact released v1.0.19 commit above
- **Latest application/code commit:** `8d394f289e02682bf27a133ed3456c7dc2852df9`
- **Overall completion estimate:** 56% acceptance-weighted; unchanged pending target-machine Stage-C evidence.

Documentation commits after the application commit advance branch HEAD. Resolve exact branch HEAD and CI from Git at the beginning of the next run.

## What this run accomplished

### MS-022 — real successful-inference qualification seam

The provider readiness work from the previous run was first reconciled and found green. This run then wired real verified 3D completion into qualification persistence.

`ai_backend/job_progress.py` now records inference qualification only after a `3d-generate` job reaches its normal verified completion point. It records the **final provider that actually produced the mesh**, including Auto fallback, plus elapsed time and current hardware context. Failed/cancelled/input-specific jobs do not mark providers inference-tested or globally broken. Qualification persistence is best-effort and happens outside the progress lock, so a state-write failure cannot convert an already verified generation into a failed job.

Relevant commits:

- `60443dbb1e420034faa400ae2edc7b8cf4b3acdb` — Record successful 3D inference qualification
- `c9983b02bc17c60f3887b6424a21aad819d0753b` — Attach hardware context to 3D qualification
- `b9dce88f8b50d99f9074ecf7f380b798feb17990` — Test 3D qualification completion hook

### MS-018 / MS-020 — Stage-C revision-bound generation Core bridge

Added `Core/StageCGeneration.cs` and Stage-B regression coverage. The Core now has a typed vertical-slice contract for:

`accepted ImageRevision → immutable GenerationJobBinding → generated MeshRevision candidate → explicit transactional apply/discard`

A generation binding captures stable job/project/output-object identity, the exact accepted input image revision and the project revision number at submission. Generated output is registered as an immutable mesh revision and review candidate; it does **not** silently become active project state.

If the accepted 2D baseline changes while generation is running, the result is preserved as `Conflict` and cannot overwrite newer work. A current result remains `Ready` until explicit apply. Apply creates the generated object and updates candidate state in one `ProjectSession` transaction, so undo/redo restores both together. Save/reload preserves accepted baseline, ready/applied/conflict status and generated provenance.

While this slice is being proven, only the small Stage-C baseline/candidate descriptors use the existing schema-7 metadata dictionary behind the strongly typed Core API. Image/mesh payloads remain ordinary durable Core revisions. This avoids a premature breaking schema bump and is a migration bridge, not widget-owned state.

Relevant commits:

- `8f0333d36429268fd9e98d03ee363bfe910ce72d` — Add Stage C revision-bound generation bridge
- `051c6048655d3714d7a4a2a4be3ef7b7528216fa` — Add Stage C generation core tests
- `8d394f289e02682bf27a133ed3456c7dc2852df9` — Run Stage C generation core tests

## Validation state

For `8d394f289e02682bf27a133ed3456c7dc2852df9`:

- `core-foundation` restore/build/regression suite: **PASS**;
- editor/launcher/updater/Core C# restore/build/tests: **PASS**;
- Python compilation and dependency dry-run: **PASS**;
- core logic tests: **PASS**;
- job-progress/qualification tests: **PASS**;
- real geometry regression tests: **PASS**;
- release audit: **PASS**;
- portable package build/layout/SHA verification: **PASS**;
- Inno Setup installer-definition compilation: **PASS**.

Branch validation is green. A real Godot Windows release export was not run because this is not yet a release candidate, and no v1.0.20 release was made.

Real CUDA inference was not performed in CI. Do not mark any provider inference-tested from CI alone; the new hook records qualification when the user's actual successful 3D generation reaches verified completion.

## Current unresolved priorities

1. **MS-009 — viewport/grid/model/gizmo:** v1.0.19 fix is published but still needs target-PC verification. Any reported blank viewport becomes immediate priority.
2. **MS-018 — Stage-C thin slice:** Core handoff/candidate semantics now exist; the production editor/backend path must use them next.
3. **MS-013 — storage containment:** v1.0.19 hardening is published but still needs representative real-machine verification.
4. **MS-020 — authoritative Job Broker/stale-result safety:** stable revision-bound candidate semantics now exist in Core; request transport/resource ownership/recovery are still incomplete.
5. **MS-022 — provider qualification:** readiness and successful-inference evidence recording exist; real GTX 1080 qualification/default-provider evidence is still missing.
6. **MS-019 — legacy `Main.V*.cs`:** migrate the Stage-C vertical slice instead of adding new widget-owned state.

## Exact next task

Integrate the production editor/backend 2D→3D seam with `StageCGeneration` while keeping legacy UI compatibility:

1. Locate the existing **Accept 2D Baseline** action and the current project/session compatibility layer. Ensure the accepted image exists as a durable `ImageRevision`, then call `StageCGeneration.AcceptBaseline()` on the real `ProjectSession`.
2. At 3D submission, create `StageCGeneration.BeginImageToMesh()` and carry its stable project/job/output-object/input-image-revision identity through the request/job context rather than relying only on file paths.
3. After the backend returns a verified STL, import it into `MeshData`, create a durable `MeshRevision` through `ProjectStore.CreateMeshRevisionAsync()` using the binding's reserved output object ID, then call `StageCGeneration.RegisterResult()`.
4. Do not directly make the generated STL authoritative. Present `Ready` output for explicit apply; preserve `Conflict` output if baseline/project state advanced. Apply through `StageCGeneration.ApplyCandidate()` and save through `ProjectStore`.
5. Add regression coverage around the actual legacy editor compatibility bridge, especially stale result arrival and save/reload.
6. Separately, collect real GTX 1080 qualification evidence for one lightweight/default 3D provider when the user runs the workflow.

Do not broaden this into an all-at-once editor rewrite. One reliable vertical slice is the goal.

## User verification requested, not blocking autonomous work

On released v1.0.19, test the 3D viewport/grid/starter or generated mesh/gizmo on the GTX 1080 machine and representative data-root/storage operations. If the viewport is blank, retain the viewport diagnostic/render-probe text. If storage escapes the configured data root, retain the unexpected path(s).

## Release policy

Do not mutate v1.0.19. Do not publish v1.0.20 merely because branch CI is green; the production Stage-C bridge is not yet integrated and target-machine acceptance remains incomplete. Before publication require relevant C#/Python/Core/job/geometry/release-audit gates plus real Godot Windows export, artifact/hash verification and installer smoke test. After publication verify GitHub latest-release state and create/use v1.0.21 before further application changes.
