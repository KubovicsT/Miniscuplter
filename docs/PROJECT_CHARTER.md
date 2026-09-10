# Miniscuplter Project Charter

> Canonical long-lived product and engineering context. Read this before making product or architectural changes. Update it only when durable project truth changes; use `PROJECT_STATUS.md`, `ISSUES.md`, and `HANDOFF.md` for fast-moving state.

## 1. Mission

Miniscuplter is a **local AI-assisted 3D modeling application for non-artists**. Its purpose is to let a user start from an idea, prompt, reference image, or existing mesh and reach a useful finished 3D asset with AI assistance plus understandable manual tools.

The defining user is someone who wants to make original 3D models but does not have professional Blender/ZBrush/3D-art skills. The application should hide unnecessary technical complexity and guide the user through a reliable creative workflow.

Local execution is a core product requirement. Paid cloud inference must not be required for the normal workflow.

### Reference hardware

The primary low/medium-spec acceptance machine is approximately:

- Windows desktop
- NVIDIA GTX 1080
- 8 GB VRAM
- 16 GB system RAM
- limited free space on the Windows system drive is a realistic condition

Operations may take minutes on this hardware. Reliability, useful stage feedback, cancellation, recovery, and preservation of work matter more than pretending every AI operation is instantaneous.

## 2. Product boundary — what Miniscuplter is not

Miniscuplter creates and edits the **3D model itself**. It is not primarily a 3D-printing preparation application.

Do not turn the product into:

- a slicer;
- support-generation software;
- G-code/toolpath generation software;
- printer-profile management;
- printer/farm control software.

STL is an important final export format, but the internal project model must not be constrained by STL limitations. Richer interchange such as GLB may be added where it preserves useful scene, rig, material, or metadata information.

## 3. Required user workflow

The user-facing product is organized around four stages. Historical implementation versions such as `v0.7`, `v0.9.6`, or `Main.V*.cs` must never define the visible UX.

### 3.1 2D

Goal: create and approve a strong visual baseline for 3D generation.

Expected capabilities:

- generate a concept from a prompt;
- import a user image;
- search internet references and preview useful results;
- adopt a reference as a working source;
- compare candidate images;
- large image preview with fit/zoom/pan;
- select a region directly on the 2D image;
- prompt-driven whole-image and regional AI editing;
- context-aware **Enhance** for a selected malformed/incoherent area without requiring a detailed user instruction;
- preserve source/candidate lineage;
- explicitly accept an image as the durable 3D baseline.

### 3.2 3D

Goal: generate a useful initial mesh and make it correct enough for the user's intended purpose.

Expected capabilities:

- image-to-3D generation from the accepted baseline;
- reliable visible 3D viewport with grid, axes, selection, camera framing, readable material and transform gizmo;
- orbit/pan/zoom and selection;
- move/rotate/scale;
- sculpting;
- masking/protected regions;
- AI-assisted regional replacement/detail workflows;
- alternatives/candidates with accept/reject rather than silent overwrite;
- kitbashing, reusable parts, sockets and attachments;
- symmetry, grounding, alignment/snapping and practical scale/measurement helpers;
- undo/redo that restores complete dependent state, not only vertex positions.

### 3.3 Rig & Pose

Goal: let a non-rigger create and correct useful poseable models.

Expected capabilities:

- quick/automatic rig generation;
- visible skeleton/joints;
- user correction of landmarks/joints;
- independent rest mesh/rest pose;
- reversible pose state;
- skinning controls appropriate for the target user;
- IK and posing.

### 3.4 Cleanup & Export

Goal: produce a technically usable final mesh while keeping analysis understandable.

Expected capabilities:

- topology/geometry validation;
- repair and finalization;
- remeshing when appropriate;
- minimum-thickness analysis/heatmap;
- structure checks;
- final scale/measurement review;
- explicit preview of export scope;
- validated STL export.

## 4. Definition of a finished product

A finished Miniscuplter should allow a non-artist to complete this representative flow on the reference machine after required models are installed:

`idea/prompt or image → 2D concept/reference → regional corrections → accept baseline → image-to-3D → inspect visible mesh → edit/sculpt/kitbash/AI-correct → optional rig/pose → cleanup/validate → save/reload → export final asset`

The product is not considered finished merely because controls or provider adapters exist. The end-to-end workflow must be **reliable in real use**.

Every major operation should:

- make current state obvious;
- provide truthful stage/progress information;
- preserve user work;
- support cancellation where practical;
- explain failures in user-facing language while retaining technical details;
- avoid silently overwriting newer revisions with stale AI output;
- recover safely from crashes/interrupted writes where practical;
- keep working data off arbitrary system locations when a Miniscuplter data root is configured.

## 5. Product requirements

### Functional requirements

- Local 2D generation and editing.
- Local image-to-3D generation.
- Manual 3D editing sufficient for practical corrections.
- AI-assisted regional improvement.
- Rigging and posing.
- Mesh cleanup/analysis.
- Project save/load and recovery.
- Final model export.
- Model/runtime installation, repair and status.
- Self-update through the launcher without destructive reinstall behavior.

### Non-functional requirements

- **Local-first:** normal creation works without paid external inference.
- **Low/medium hardware:** reference GTX 1080/8 GB + 16 GB RAM is a real acceptance target, not an edge case.
- **Storage containment:** generated images, masks, job artifacts, caches, logs and temporary work normally live under the Miniscuplter-controlled data root; do not silently spill to `%APPDATA%`, `%TEMP%`, or C: when a configured data root exists.
- **Data preservation:** updates must preserve projects, AI models, partial model downloads/staging, Python environments, runtime caches, parts library, exports, settings and user data as appropriate.
- **Truthful progress:** never show invented inference percentages. Expose real provider steps when available; otherwise show explicit stages/activity and say that percentage is not known.
- **Failure transparency:** no silent provider swaps when the user explicitly selected a provider; Auto routing may fall back when documented.
- **Transactional persistence:** writes and model-changing operations should be atomic/recoverable where feasible.
- **Backward compatibility:** legacy project migration must create safe new copies and retain originals until migration is proven.
- **Responsive UI:** dynamic status text and window resizing must not make required controls unreachable.
- **Testability:** important correctness claims should have regression tests; GPU/model runtime claims require real-machine qualification where CI cannot prove them.

## 6. Current architecture

### Desktop/editor

Godot .NET / C# currently owns:

- application UI;
- 2D/3D workspace presentation;
- scene/edit state;
- STL import/export;
- selection and sculpt interaction;
- transforms/gizmos;
- rig/pose and parts workflows;
- launcher/backend integration;
- project and recovery entry points.

The historical application accumulated functionality across many `Main.V*.cs` partial classes. This code is a behavioral/migration source, **not sacred architecture**.

### Local backend

Python/FastAPI currently owns or coordinates:

- image generation/editing;
- image-to-3D providers;
- semantic selection;
- geometry analysis/repair/remesh;
- thickness analysis;
- rig prediction;
- AI/detail pipelines;
- provider routing and model/runtime management;
- job progress/status.

### Delivery

A Windows launcher/updater manages:

- application startup;
- AI runtime/model installation and repair;
- update discovery;
- verified release download;
- transactional replacement/rollback;
- preservation of expensive/persistent user/runtime data.

Published stable releases are GitHub Releases with verified ZIP/installer artifacts.

## 7. Accepted architectural direction — Astra takeover audit

A repository-wide GPT-6 Astra audit concluded that Miniscuplter should undergo a **major architectural refactor with selective subsystem rewrites**, rather than continue permanently adding compatibility layers to the historical `Main.V*.cs` architecture.

### Keep unless a concrete blocker is proven

- Godot/.NET as the Windows desktop/rendering shell;
- the local Python boundary for AI inference and geometry;
- useful sculpt/mesh algorithms after correctness/performance review;
- verified/resumable model downloads and manifests;
- compatibility with the existing launcher/update path and user data.

### Replace rather than extend permanently

- `Main.V*.cs` as the authoritative application-state architecture;
- widget-owned/text-name-based identity;
- mesh-only global undo;
- STL as internal project storage;
- request-lifetime-only AI job handling;
- duplicated provider capability/install/routing truth;
- version/milestone-derived UI composition.

### Target domain model

- stable UUID identity for project objects, images, mesh revisions, rigs, attachments and AI candidates;
- immutable mesh/image revisions with explicit provenance;
- selections/masks bound to exact revisions;
- topology-changing operations explicitly transfer or invalidate dependent data;
- display names are labels, never identity.

### Target command/history model

Every model-changing operation should record:

- affected stable IDs;
- complete before state;
- complete after state;
- dependent metadata changes.

Transforms, mesh changes, selections/protected regions, attachment state and AI Apply should undo/redo transactionally together.

### Target project store

- versioned manifest;
- indexed binary mesh assets rather than STL internally;
- durable assets before atomic manifest replacement;
- bounded recovery journal/checkpoints;
- legacy schema migration into a new project copy with a migration log;
- originals retained.

### Target local Job Broker

- authoritative queue and persistent job IDs;
- immutable input revision per job;
- project/object/revision context;
- structured stage/progress/output artifacts;
- one heavyweight GPU job by default;
- cooperative cancellation followed by isolated worker termination if required;
- cancellation acknowledged only after work is actually stopped;
- install/remove/repair uses the same runtime ownership lock;
- stale results become candidates/conflicts rather than overwriting newer state.

### Target provider registry

One source of truth should describe:

- provider ID and role;
- pinned code/weights/environment;
- Windows/platform requirements;
- hardware requirements;
- license/source;
- install state;
- importability;
- device/CUDA test result;
- inference self-test result;
- benchmark/resource presets.

`downloaded`, `installed`, `importable`, `device-tested`, and `inference-tested` are different states.

## 8. Accepted staged refactor plan

### Stage A — safety bridge

Updater/runtime/storage/geometry/export/recovery hardening. Substantial work shipped across v1.0.7–v1.0.18, especially v1.0.12 onward.

### Stage B — new foundation and migration harness

Stable IDs, immutable revisions, transactional history, indexed project assets, legacy migration, storage service and job architecture. This has **begun and exists beside the legacy application**, but is not yet authoritative across the whole editor.

### Stage C — complete reliable thin slice

Prove end to end on target hardware:

1. import or generate 2D;
2. approve a durable baseline revision;
3. generate through one qualified lightweight 3D provider;
4. visibly inspect/edit result;
5. save/reload;
6. basic cleanup;
7. preview exact export scope;
8. export STL.

Include measured RAM/VRAM, cancellation recovery and offline operation after required downloads.

### Stage D — practical editing

Move selection, sculpt, transform, topology, parts/attachments and candidates onto the new state/history foundation with spatial acceleration and correct full-state undo.

### Stage E — Rig & Pose / regional AI

Independent rest mesh, correction-friendly rigging, reversible pose state, useful IK, and guided regional replacement: `select → describe/enhance → candidates → compare → place → apply/discard`.

### Stage F — beta/stable transition

Qualified default provider bundle, clean/install/upgrade/low-disk/interruption matrix, migration fixtures, recovery UI, named checkpoints, provider self-tests, and removal of obsolete legacy implementation only after migration/acceptance coverage exists.

## 9. AI/provider philosophy

Miniscuplter may support several providers, but **more adapters is not the same as a better product**. Prefer a smaller qualified set that works reliably on Windows and the target hardware.

Current model families implemented historically include:

- 2D: SD2.1, SDXL, FLUX.2 Klein 4B, Z-Image Turbo, Qwen Image 2512, Qwen Image Edit;
- 3D: TripoSR, Hunyuan3D 2.1, Hunyuan 2mini, Stable Fast 3D, SPAR3D, TRELLIS.2;
- structured parts: PartCrafter, PartPacker;
- semantic selection: CLIPSeg.

Auto routing should increasingly expose user intent such as **Fast / Balanced / Quality** rather than forcing non-expert users to understand every provider. Explicit expert selection can remain available.

The application must account for VRAM/RAM/disk limits and use offload/quantization/fallback strategies where they make the workflow viable. Do not hide an impossible workload behind endless swapping or misleading progress.

## 10. Storage and update policy

Default packaged behavior should keep Miniscuplter-controlled data with the installation/data area rather than arbitrary C: locations. A user-configured data root may be supported, but once chosen it is authoritative.

Persistent/expensive assets include:

- projects and recovery data;
- AI model weights;
- partial/staged model downloads;
- Python `.venv` / isolated provider environments;
- runtime/model caches;
- parts library;
- exports;
- settings;
- job artifacts needed for recovery/history.

Application updates must not needlessly redownload or destroy them.

## 11. Release discipline

- Published versions are immutable.
- New application code goes on a new version branch.
- Version metadata must be internally consistent.
- Branch validation is not the same as a release.
- Before publication: relevant tests, C# builds, Python compile/tests, release audit, real Godot Windows export, package/hash verification and installer smoke test should pass.
- Publish only after those gates pass.
- After publication, start the next application change on the next version branch.
- User-observed runtime/UI defects remain `FIXED - NEEDS USER VERIFICATION` until validated appropriately on the real machine.

## 12. UX principles

- Organize around user goals, not implementation history.
- One obvious primary workflow; advanced controls should not overwhelm the normal path.
- Show outputs visually where the user acts on them.
- Long jobs must show stage, elapsed time, provider and cancellation state.
- Make accept/reject/candidate semantics explicit.
- Never silently replace newer work with stale AI output.
- Errors should have a short human explanation plus expandable technical details.
- Defaults should favor success on target hardware; expert options remain accessible.
- Window resizing and dynamic status content must preserve access to all controls.

## 13. Completion accounting

Project completion is measured against acceptance-ready workstreams, not raw line count or number of buttons. `PROJECT_STATUS.md` owns the current weighted estimate. A feature that exists but is not integrated/reliable receives partial credit only.

## 14. Source-of-truth hierarchy

When documents disagree, resolve the conflict and update them. Operationally use this order:

1. actual repository/code/test/release state;
2. this charter and accepted decisions;
3. `PROJECT_STATUS.md`;
4. `ISSUES.md`;
5. `HANDOFF.md` for the immediate baton;
6. older milestone/release-history documents.

Do not rely on chat memory when the repository can establish the fact.
