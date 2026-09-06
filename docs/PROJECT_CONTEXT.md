# Miniscuplter Project Context / Astra Handoff

This document is the human-product context for Miniscuplter. It is intended to be read together with the codebase, `README.md`, `docs/ARCHITECTURE.md`, `docs/AI_MODELS.md`, `docs/BUILD_AND_RELEASE.md`, `docs/RUNTIME_TESTING.md`, and `docs/RELEASE_HISTORY.md`.

The latest stable product baseline at the time of this handoff is **v1.0.11**, release commit:

```text
0e467ae67d7eda105ff8dd0c1e5c74f2da4cc060
```

The code in the repository is valuable as a working prototype/product baseline, but it is **not sacred**. A future implementation may be heavily refactored or rewritten from the ground up if that produces a more reliable, maintainable, performant and user-friendly Miniscuplter. Preserve the product intent, useful behavior, user data, update path and tested capabilities—not accidental historical architecture.

---

## 1. Product mission

Miniscuplter exists so a person who is **not a professional 3D artist** can create their own useful 3D models with AI assistance, locally on an ordinary Windows PC.

The product should make difficult modeling tasks approachable by combining:

- AI-assisted concept creation;
- image editing and reference workflows;
- image-to-3D generation;
- selective AI refinement of parts/details;
- direct manual sculpting and transform tools;
- kitbashing / reusable parts;
- rigging and posing;
- mesh analysis, repair and cleanup;
- final STL export.

The user should not need to understand model repositories, Python environments, CUDA internals, mesh topology jargon or historical development milestones to use the application.

The ideal experience is closer to:

> “Describe or show what I want, get a usable starting model, improve specific parts with AI or simple editing tools, pose it, clean it up and export it.”

than:

> “Operate a collection of unrelated AI demos and low-level modeling utilities.”

---

## 2. Primary target user

The target user:

- may have little or no traditional 3D sculpting experience;
- understands what they want to make, but may not know how to model it manually;
- wants local generation for privacy, cost control and independence from subscription APIs;
- is comfortable waiting somewhat longer if necessary, but needs visible progress and reliable recovery;
- should be able to succeed without learning Blender-level complexity.

The application should support hobby models, miniatures, props, game/rendering assets, CAD-adjacent utility meshes and other general-purpose 3D objects. It must not assume that every output is for 3D printing.

---

## 3. Product boundary / explicit non-goals

Miniscuplter's output is the **3D model itself**.

It is intentionally **not** a slicer or printer-control application.

Do not turn Miniscuplter into a tool for:

- generating print supports;
- slicing;
- G-code/toolpath generation;
- resin exposure profiles;
- printer profiles;
- printer control.

Those tasks belong to other software.

Printability/model-analysis features are useful only insofar as they help create a sound model. Thickness heatmaps, watertightness checks and topology warnings are appropriate. Printer-specific preparation is not.

STL is currently the primary final format, but the architecture may support additional neutral 3D formats later if doing so is useful and does not complicate the core workflow unnecessarily.

---

## 4. Required user-facing workflow

The user should see **one coherent workflow**, not historical implementation layers.

The intended top-level workflow is exactly:

### Step 1 — 2D

Purpose: establish the visual baseline/reference for the model.

Capabilities should include:

- generate a 2D concept from a text prompt;
- load the user's own starting image;
- optionally search external references explicitly when internet access is enabled;
- AI-edit the image;
- generate alternatives;
- inspect the result in a large, zoomable preview;
- explicitly **accept an image as the baseline** before moving to first 3D generation.

A good future version may add useful concept-stage features such as multi-view/turnaround generation, background cleanup, silhouette cleanup, reference boards or consistency checks, but only if they make the modeling workflow easier.

### Step 2 — 3D

Purpose: create and edit the actual model.

Capabilities should include:

- generate the initial 3D mesh from the accepted 2D baseline;
- visible progress, stage information, elapsed time and cancellation;
- sculpting;
- transforms;
- selection and semantic Smart Select;
- masks / local edit regions;
- generate 2D alternatives for a selected region;
- reconstruct selected/local 3D detail;
- apply/merge AI-generated patches non-destructively where practical;
- remesh when useful;
- kitbashing;
- reusable parts library;
- sockets / attachments;
- duplicate/delete/separate objects;
- preserve separate editable components until the user intentionally combines them.

The 3D step should feel like one editing workspace, not separate “AI”, “v0.5”, “kitbash”, “geometry” and “sculpt” products glued together.

### Step 3 — Rig & Pose

Purpose: make the finished shape poseable and establish final pose.

Capabilities should include:

- quick/automatic rig generation;
- optional more advanced/universal rigging route;
- joint/skeleton editing;
- IK where appropriate;
- posing;
- safe invalidation/rebuild when topology changes.

The user should be able to create a static final model without being forced to understand rigging internals.

### Step 4 — Cleanup & Export

Purpose: make the result structurally usable and export it.

Capabilities should include:

- mesh inspection;
- topology/watertightness/non-manifold diagnostics;
- repair/finalization;
- remesh/union when deliberately requested;
- minimum-thickness heatmap;
- structural/feature warnings;
- scale/dimensions;
- guarded STL export.

The app should explain problems in understandable language and distinguish warnings from actual blockers.

---

## 5. UX principles

These are product requirements, not cosmetic preferences.

### One product, no version archaeology

Historical labels such as:

```text
v0.4 geometry-aware AI
v0.5 patch workflow
v0.9.6 Smart Select
```

must never be presented to normal users as navigation or conceptual groupings.

The existing code accumulated capabilities through additive release-specific partial classes. That history may remain visible in source control, but the UI should be organized by **user task**.

### Progressive disclosure

The normal workflow should show what a non-expert needs first. Advanced provider, quality, runtime and file-location controls belong in **Settings / Advanced Settings**, not in the main creative flow.

### Visible long-running work

Every operation that can take more than a moment should provide as much of the following as technically possible:

- current stage;
- useful progress or at least an activity indicator;
- elapsed time;
- estimated time when it can be estimated honestly;
- provider/model being used;
- cancellation;
- a clear result or failure message;
- access to detailed logs when needed.

Do not leave the user staring at a frozen-looking interface while Python or CUDA works in the background.

### Errors must be visible and actionable

Do not hide important errors in a bottom status line that may be off-screen. Surface failures where the user is working and explain what can be done next.

Avoid silent fallback when it conceals a broken configuration. For example, if NVIDIA hardware is detected but the PyTorch CUDA runtime is unusable, report that clearly rather than silently running an unexpectedly slow CPU job.

### Responsive layout

Do not use fixed-size panels that push controls outside the window.

The application must behave sensibly when resized:

- viewport expands/shrinks correctly;
- tool panels can scroll;
- generated feedback does not make other controls unreachable;
- important actions remain visible at common laptop/desktop resolutions.

### 3D viewport readability

On launch, the user must unmistakably see that a 3D viewport exists.

Provide:

- a visible ground/grid plane;
- clear major/minor spacing;
- axis indication;
- background and model colors with good contrast;
- sensible default lighting;
- frame/zoom controls;
- stable viewport resizing.

### Non-destructive first

Keep objects, generated alternatives, patches and attachments independently editable until the user deliberately applies a destructive operation.

Provide undo/redo and transactional operations where feasible.

---

## 6. Local-first requirement

The application is fundamentally **local-first**.

Normal generation/editing should run locally. The user should not need paid inference APIs to create models.

Internet access is appropriate for explicit functions such as:

- downloading/installing AI models;
- application updates;
- optional reference-image search;
- fetching known upstream provider source/dependencies.

These network features should be explicit and understandable.

Do not quietly upload user artwork/models to third-party inference services.

Optional external or cloud providers could be supported in the future, but local functionality must remain first-class and the user must know when data leaves the machine.

---

## 7. Hardware target and performance philosophy

Miniscuplter should be designed to remain useful on **low/medium-spec consumer PCs**, not only current high-end RTX workstations.

The current real-world development/test machine is approximately:

```text
Windows 10 x64
NVIDIA GeForce GTX 1080
8 GB dedicated VRAM
16 GB system RAM
Python 3.10 local runtime
```

The machine also has limited free space on its Windows C: drive while Miniscuplter and AI data may live on another drive such as X:.

This machine should be treated as an important baseline, not an edge case.

### Performance expectations

The application should:

- use the GPU aggressively when doing so improves performance;
- avoid wasting large amounts of available VRAM through unnecessarily conservative offload modes;
- still maintain a safety margin against CUDA OOM;
- automatically fall back to safer execution when required;
- release heavy models when switching specialists;
- avoid keeping multiple multi-GB models resident without reason;
- expose a simple performance policy such as Auto / Fast / Balanced / Low-VRAM rather than making normal users tune PyTorch internals;
- optimize for actual end-to-end time, not merely minimum peak VRAM.

The current SDXL implementation evolved toward a VRAM-first policy with an approximately 85% soft PyTorch allocation ceiling and tiered offload behavior. Re-evaluate this empirically rather than preserving it blindly.

### Disk behavior

Do not assume `%TEMP%` on C: has many gigabytes free.

Downloads, model builds, runtime caches, application update staging and temporary AI artifacts should prefer the configured data/install drive where practical.

This requirement came from real failures where updater backups and pip build environments exhausted C: even though the application drive had ample space.

---

## 8. Current architecture (v1.0.11)

The current implementation is roughly:

```text
Miniscuplter.Launcher.exe (.NET / Windows UI)
│
├── hardware detection
├── model install/remove/update
├── AI runtime repair
├── application update discovery/download
└── launches App/Miniscuplter.exe
        │
        ├── Godot 4.7.2 .NET / C# editor
        │   ├── viewport / scene
        │   ├── sculpting
        │   ├── selection
        │   ├── masks
        │   ├── kitbash / parts / sockets
        │   ├── rigging / posing
        │   ├── persistence
        │   ├── validation / export
        │   └── AI job UI
        │
        └── local Python / FastAPI backend
            ├── image generation/editing
            ├── image-to-3D providers
            ├── model routing
            ├── semantic selection
            ├── geometry/remesh/repair/thickness
            └── provider/model management bridges
```

Important implementation pieces include:

- `Scripts/AIClient.cs` — local HTTP AI client and long-job serialization/cancellation;
- `Scripts/BackendLauncher.cs` — starts/contains the Python backend;
- `ai_backend/app.py` — FastAPI routes;
- `ai_backend/model_router.py` / capability files — role-based provider selection;
- `ai_backend/model_downloads.py` / model-manager files — verified resumable model installs;
- `Launcher/` — model/runtime/application lifecycle;
- `Updater/` — transactional self-update;
- `build_release.ps1`, installer files and GitHub Actions — reproducible Windows release.

### Important architecture warning

The editor source accumulated many `Main.Vxxx.cs` additive layers across development milestones. Some UI installation still depends on runtime node searches and compatibility names.

This made rapid incremental development possible, but it is also a major source of complexity, brittle layout behavior and confusing cross-version dependencies.

A future rewrite should **not automatically retain this additive-version architecture**.

It is reasonable to replace it with a cleaner application structure such as:

- stable domain/application services;
- explicit view models/state;
- declarative/editor scene layout;
- workflow-specific modules;
- a job manager/event system;
- clearly defined backend interfaces;
- proper configuration and persistence schemas.

Godot + C# may still be a good choice, but it should be justified after auditing the application. A different desktop/UI architecture is acceptable if it materially improves maintainability, performance and UX and still supports the 3D viewport/editing requirements.

Likewise, the Python backend may be reorganized or replaced where sensible. Do not rewrite merely for novelty; rewrite where the current structure makes correctness, packaging or UX unnecessarily difficult.

---

## 9. AI/provider philosophy

Miniscuplter uses **role-based model selection** rather than assuming one AI model does everything.

Current managed/provider set includes variants of:

### 2D

- Stable Diffusion 2.1;
- SDXL;
- FLUX.2 Klein;
- Z-Image Turbo;
- Qwen Image;
- Qwen Image Edit.

### 3D

- TripoSR;
- Hunyuan3D 2.1;
- Hunyuan3D 2mini;
- Stable Fast 3D;
- SPAR3D;
- TRELLIS.2 bridge.

### Structured parts

- PartCrafter;
- PartPacker.

### Semantic selection

- CLIPSeg.

Not all providers have equal Windows maturity. Some are experimental, require isolated environments or upstream build dependencies, and some high-end models are unrealistic for an 8 GB GPU.

### What future development should do

Audit every provider based on:

- actual output usefulness;
- Windows reliability;
- install complexity;
- VRAM/RAM/disk requirements;
- speed on representative hardware;
- licensing/distribution constraints;
- maintenance burden.

Do **not** keep a provider merely because it already has an adapter.

Prefer a smaller set of genuinely reliable, well-routed providers over a large menu of theoretical options.

A normal user should choose intent/quality, not memorize model names. Advanced users may override model routing in Settings.

### Installation expectation

End users should not unexpectedly need to install developer tools such as Git, Visual Studio Build Tools, CMake or the CUDA SDK just to use a normal supported model.

The current TripoSR/`torchmcubes` path exposed this problem during runtime testing. A future implementation should prefer prebuilt/runtime-safe dependencies, vendorable wheels, alternate algorithms, isolated provider packages or another provider rather than asking normal users to become Python/CUDA developers.

---

## 10. Current model/download safety principles

These behaviors are worth preserving even if the implementation is rewritten:

- large model downloads use explicit audited manifests instead of downloading entire upstream repositories blindly;
- exact selected payload byte sizes are queried/verified where upstream metadata allows;
- downloads are staged before going live;
- partial downloads survive cancellation/interruption and can resume;
- stale or changed manifests/revisions are detected;
- redundant abandoned stages are cleaned;
- model weights are stored outside the Git repository;
- model installation/removal should not happen underneath a running editor using those files;
- model/runtime operations are visible and cancellable.

---

## 11. Current runtime lessons

Real testing uncovered several failures that should guide redesign:

### Corrupt Python environment

The installed `.venv` once had missing internal PyTorch files, causing `import torch` to fail before CUDA or SDXL could start. Later, pip itself was also structurally corrupt.

Repair was improved to rebuild the disposable `.venv`, preserve models/caches, structurally validate PyTorch and verify actual CUDA access.

Lesson: **package metadata checks such as `pip check` are not sufficient to prove a runtime is usable**. Runtime health checks should exercise actual imports and GPU access.

### Hidden AI failures

Earlier versions only wrote AI status/error text into a part of the UI that could be off-screen. The user saw no result and did not know where the job failed.

Lesson: long-running AI work needs first-class job UI and visible failure states.

### TEMP / disk pressure

Updater and pip build operations previously used C:\Users\...\Temp and exhausted the small system drive.

Lesson: temporary storage must be data-root/install-drive aware.

### SDXL memory behavior

Sequential CPU offload was initially so conservative that system RAM approached exhaustion while most GTX 1080 VRAM remained unused.

Lesson: optimize for useful hardware utilization and speed with controlled fallback, not simply minimum VRAM consumption.

---

## 12. Geometry/editing principles

### Scene/object model

Treat generated parts and user-imported parts as real editable objects. Preserve transforms and relationships.

### Destructive operations

Operations such as voxel remesh/union may soften detail. They should be explicit, undoable/transactional where feasible and never silently applied as a universal “fix”.

### Validation

Structural checks should include useful concepts such as:

- watertightness;
- winding/orientation;
- open edges;
- non-manifold edges;
- degenerates;
- size/bounds;
- thin features.

Thickness is advisory because required thickness depends on use case.

### Heatmap

A minimum-thickness heatmap is an intended product feature. It should be clear, useful and applicable beyond 3D printing.

---

## 13. Persistence and user-data requirements

Do not sacrifice user work during refactors or updates.

Persistent/expensive data includes, depending on configuration:

- AI model weights;
- interrupted model stages;
- AI runtime caches;
- Python/provider environments;
- projects;
- reusable parts library;
- exports;
- launcher/app settings;
- configured data-root paths.

If project/data schemas change, provide migration rather than silently breaking older projects.

Autosave/recovery and transactional writes are desirable for project-critical state.

---

## 14. Launcher and self-update requirement

One of the most important practical requirements is that development builds can be delivered through the app's existing update flow.

The user wants to test each new version by:

```text
open launcher
→ update offered
→ approve
→ update installs
→ existing AI models/data remain
→ new version launches
```

The current updater already implements valuable behavior:

- GitHub stable-release discovery;
- semantic-version comparison;
- resumable ZIP downloads;
- exact byte-size and SHA-256 verification;
- package-internal `release.json` verification;
- storage-aware update cache selection;
- application-drive staging;
- same-volume move-based backup/install where possible;
- rollback on failure;
- preservation of AIData, `.venv`, caches, projects, exports and settings;
- updater GUI executable rather than an empty console window.

A rewrite may replace this updater, but **must preserve or improve the user experience and safety**.

### Greenfield rewrite migration

If the future application is rewritten enough that it should become v2.x or use a different folder/runtime layout, provide a migration bridge so a currently installed v1.0.11 user can still update through the launcher rather than manually uninstalling/reinstalling and redownloading model data.

A good approach may be:

1. retain compatibility in the existing launcher/updater long enough to install the new application;
2. migrate/reuse AIData where compatible;
3. leave incompatible old runtime data removable only after successful migration;
4. verify rollback before deleting the previous app;
5. then allow the new generation of launcher/updater to take over future releases.

---

## 15. Version / branch / release discipline

The development workflow established with the user is:

```text
published stable release
→ create a NEW version/development branch
→ implement a coherent change batch
→ validate
→ produce the full Windows build
→ smoke-install it
→ publish it as the next stable GitHub Release
→ user tests through the launcher
```

Do not silently modify a previously published application version and replace its assets.

Published releases should be treated as immutable.

For ordinary patch work, increment the patch version (`1.0.11` → `1.0.12` → ...).

If a major architectural/compatibility rewrite justifies a major/minor version such as `1.1.0` or `2.0.0`, explain the reason and ensure the current updater can migrate to it.

Documentation-only planning branches do not need to consume an application release version unless they alter the installed product.

---

## 16. Build/release quality bar

Do not call a version ready merely because code compiles.

The current release pipeline includes:

- .NET builds for editor/launcher/updater;
- Python compile checks;
- core logic tests;
- release audit;
- portable packaging validation;
- SHA sidecar verification;
- Inno Setup installer compilation;
- pinned official Godot editor/template download verification;
- real Windows Godot export;
- output validation;
- silent installer smoke-install;
- GitHub Release publication.

A redesigned project should preserve this philosophy and add stronger tests where useful.

Recommended additions for a rebuild include:

- application/domain unit tests;
- project-save/load migration tests;
- updater migration/rollback tests;
- provider contract tests;
- headless/backend integration tests;
- UI smoke tests for the four workflow steps;
- viewport startup/resize tests;
- long-job cancellation tests;
- low-disk behavior tests;
- runtime health tests that import the real packages;
- representative GPU runtime validation scripts.

Do not pretend CI can prove every optional CUDA/provider combination works. Clearly distinguish static/build validation from real hardware inference testing.

---

## 17. Known current weaknesses / areas worth reconsidering

These are not instructions to patch around forever. They are strong candidates for architectural cleanup.

### Additive editor architecture

The current editor contains many release-specific partial classes and runtime installers. This creates brittle coupling and makes the UI difficult to reason about.

Strongly consider replacing this with a clean workflow/state architecture.

### UI/UX maturity

The UI was assembled feature-by-feature and only recently consolidated into four workflow tabs. Treat v1.0.11 as a functional reference, not a polished final design.

A redesign should establish:

- consistent spacing/hierarchy;
- iconography and readable labels;
- obvious primary/secondary actions;
- contextual tool properties;
- clear object/scene navigation;
- proper progress/job presentation;
- large previews where visual judgment matters;
- settings separated from creative tasks;
- keyboard shortcuts where beneficial;
- accessible scaling and resizing.

### Provider maturity

Some provider integrations are incompletely runtime-tested on Windows. Audit them and remove/rework anything that creates disproportionate installation or maintenance burden.

### Launcher/editor split

Evaluate whether model management, runtime repair, settings and application launching are divided sensibly. A unified shell may eventually be cleaner, but do not lose updater safety or ability to repair the runtime when the editor cannot start.

### Python packaging

Evaluate whether the shared `.venv` plus isolated specialist environments is still the right strategy. The goal is reliable installation and reproducible execution, not preserving Python packaging choices for their own sake.

### Job orchestration

Long-running AI/geometry tasks should probably have a centralized job system with structured events/progress/cancellation/logging rather than each feature implementing its own status UI.

---

## 18. Permission to improve the product beyond the current feature list

The future developer/model has explicit permission to propose and implement functionality the user did not specifically request when it clearly supports the product mission.

Examples of potentially useful directions include:

- concept turnaround / multi-view consistency;
- automatic background/silhouette preparation before image-to-3D;
- smarter provider routing based on available hardware and desired wait time;
- generation-time estimates learned from the local machine;
- side-by-side candidate comparison;
- semantic part separation;
- symmetry-aware editing;
- better automatic topology cleanup;
- object/history browser;
- versioned/non-destructive project history;
- guided workflows for common modeling tasks;
- hardware-aware “quality vs speed” recommendations;
- clearer before/after previews for destructive operations.

However, avoid feature creep. Every added feature should answer:

1. Does this make it easier for a non-artist to create a good 3D model?
2. Can it work reasonably on local low/medium-spec hardware, or degrade gracefully?
3. Does it fit naturally into the four-step workflow or Settings?
4. Can it be maintained and tested reliably?

If not, leave it out.

---

## 19. First task for a new model/developer taking over

Do **not** immediately start changing random files.

First perform a repository-wide audit.

### Phase A — Understand

1. Read this document.
2. Read the current README and architecture/model/build/runtime docs.
3. Inspect the exact v1.0.11 release and codebase.
4. Inventory every user-visible feature and every background subsystem.
5. Map which features are actually implemented, duplicated, fragile, experimental or dead.
6. Identify the most important architecture/UX/performance debt.
7. Understand the existing updater/data-preservation contract.

### Phase B — Decide target architecture

Produce a concrete technical proposal answering:

- keep/refactor Godot or replace it?
- keep/reorganize Python backend or replace parts?
- how should application state/project persistence work?
- how should jobs/progress/cancellation work?
- how should provider routing/install isolation work?
- how should the four-step UI be structured?
- what should remain in a separate launcher, if anything?
- how will v1.0.11 installations update/migrate into the new design?
- what functionality should be removed, retained, improved or newly added?

Do not assume a total rewrite is automatically better. Compare incremental refactor vs greenfield rebuild honestly. A ground-up rebuild is fully allowed if the analysis shows it is the best route.

### Phase C — Build a better Miniscuplter

Once a target is chosen:

- create a new development/version branch;
- establish the clean architecture before piling features back in;
- preserve/migrate user data;
- recreate useful current functionality in a coherent order;
- continuously build/test usable releases;
- keep the self-update path functioning;
- prioritize reliability and understandable UX over raw feature count.

The user explicitly wants the next generation of Miniscuplter to be better than the current implementation, not merely cosmetically rearranged.

---

## 20. Definition of success

A successful Miniscuplter should let the target user do something like this without expert intervention:

1. Open the app on a GTX 1080-class machine.
2. Type a description or load an image.
3. Generate/edit a concept and choose the desired baseline.
4. Generate a first 3D model locally.
5. See clearly what the application is doing and how long it is taking.
6. Select a bad/undesired part and ask AI to improve it.
7. Sculpt or kitbash parts manually when AI is not the best tool.
8. Rig/pose if needed.
9. Inspect thickness/topology and repair genuine problems.
10. Export a clean STL.
11. Save/reopen the project reliably.
12. Update to a newer app version without losing projects or redownloading many gigabytes of AI models unnecessarily.

The user should feel that Miniscuplter is a coherent creative tool designed for them—not a front end over a collection of experimental scripts.
