# Miniscuplter v1.0 Runtime Test Protocol

This document separates runtime validation from code/CI validation. A green CI run proves compilation, static/core tests and package construction; it does not prove CUDA/model inference quality or every UI interaction on target hardware.

## Phase 1 — Installer and launcher

Use the GitHub Actions **full Windows release artifact**, not the Godot editor, for the primary test.

Verify:

- installer opens and accepts a custom writable installation folder
- launcher starts after installation
- CPU, RAM, GPU and VRAM are detected correctly
- recommended quality profile is sensible
- install root and AI data root are correct
- application can start even before local AI is configured

## Phase 2 — AI runtime

Install Python 3.10 x64 if it is not already available, then use **Repair AI Runtime**.

Verify:

- `.venv` is created under `App/ai_backend`
- setup completes without `pip check` failures
- launcher reports the AI runtime ready
- runtime fingerprint is present/current
- editor starts the backend successfully

## Phase 3 — Recommended 8 GB model stack

Install one component at a time:

1. SDXL
2. TripoSR
3. Hunyuan3D 2.1 Shape
4. CLIPSeg

For every model verify install status, path, revision display and launcher restart persistence before moving on.

Do not start with FLUX or PartCrafter; they are later stress tests on 8 GB Pascal hardware.

## Phase 4 — Minimal end-to-end workflow

Run this before testing advanced features:

```text
Generate/capture image
→ 2D edit
→ TripoSR 3D generation
→ add mesh to scene
→ save .msculpt project
→ close application
→ reopen project
→ validate mesh
→ export STL
```

Verify exported STL exists, is non-empty and reloads correctly.

## Phase 5 — Feature groups

Test independently before combining:

- object transforms and kitbashing
- parts library and sockets
- Quick Rig, joint editing, pose preview/reset/apply and IK
- all sculpt brushes, symmetry and masks
- voxel remesh and Undo
- Smart Select geometry fallback
- Smart Select CLIPSeg route
- Hunyuan quality generation
- 2D selected-detail refinement
- 3D detail preview/apply/discard
- structural analysis
- repair
- thickness heatmap
- final scene bake/union
- project recovery behavior

## Phase 6 — Model/update management

With the editor closed:

- remove and reinstall one model
- check upstream revisions
- exercise a model update when an upstream revision is available
- verify a failed/incomplete operation does not destroy the previous live installation

Then test application update staging using a controlled GitHub Release. Confirm the AI `.venv` and AIData survive the app update and that a changed backend runtime fingerprint asks for Repair AI Runtime instead of pretending the environment is current.

## Phase 7 — Heavy/edge tests

After the core workflow is stable:

- PartCrafter structured parts
- FLUX.2 Klein
- large meshes (250k–750k+ triangles)
- multiple separate shells
- non-manifold/broken STLs
- very fine remesh/repair pitches
- Ultra/custom quality settings
- cancellation during long AI operations
- low disk space and interrupted network/model download

## Bug-report format

For each failure record:

- exact v1.0 commit/artifact name
- Windows version
- GPU + driver version
- install location
- model/provider involved and installed revision
- exact action sequence
- expected result
- actual result
- launcher status/error text
- editor status/error text
- Python/backend console/log output if available
- input STL/image when the problem is input-specific

Do not work around reproducible failures during the first validation pass. The goal is to expose and fix the underlying v1.0 issue.
