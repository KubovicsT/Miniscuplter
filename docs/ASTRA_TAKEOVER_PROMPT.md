# GPT-6 Astra Takeover Prompt — Miniscuplter

Copy the prompt below into a new GPT-6 Astra conversation with access to the `KubovicsT/Miniscuplter` GitHub repository.

---

You are taking over development of **Miniscuplter**, a Windows desktop application for local AI-assisted 3D model creation.

Repository:

```text
KubovicsT/Miniscuplter
```

Start from the branch:

```text
astra-handoff
```

That branch is based on the exact v1.0.11 stable release and contains the handoff document:

```text
docs/PROJECT_CONTEXT.md
```

## Your first job: understand before changing

Do not begin by making isolated patches.

First perform a repository-wide technical and product audit.

Read, at minimum:

```text
docs/PROJECT_CONTEXT.md
README.md
docs/ARCHITECTURE.md
docs/AI_MODELS.md
docs/BUILD_AND_RELEASE.md
docs/RUNTIME_TESTING.md
docs/RELEASE_HISTORY.md
```

Then inspect the actual codebase, including the editor, launcher, updater, Python backend, model manager/router, persistence, geometry tools, release scripts and CI.

Treat `docs/PROJECT_CONTEXT.md` as the source of truth for the **product goal and design intent**, but do not assume the current implementation is the correct architecture.

The current code is a functioning baseline and migration source, not something that must be preserved line-for-line.

## Product goal

The core purpose of Miniscuplter is:

> Let a person who is not a professional 3D artist create their own useful 3D models with local AI assistance, on a low/medium-spec Windows PC.

The important real-world target machine is approximately:

```text
Windows 10 x64
NVIDIA GTX 1080
8 GB VRAM
16 GB system RAM
```

The application may be installed on a non-system drive, while the Windows C: drive may have little free space.

Local AI is a first-class requirement. Do not redesign the product around paid cloud inference APIs.

The app's responsibility ends at a finished 3D model. It is **not a slicer**: do not add print supports, slicing, printer profiles or G-code.

## Required top-level workflow

The normal user should see one coherent workflow:

1. **2D**
   - text-to-image concept generation;
   - user-provided starting image;
   - AI image editing / alternatives;
   - reference support where useful;
   - large visual preview;
   - explicitly accept a baseline image.

2. **3D**
   - generate the initial mesh from the accepted baseline;
   - local AI refinement of selected regions;
   - masks / alternatives / local reconstruction;
   - sculpting;
   - transforms;
   - semantic selection;
   - kitbashing / parts / sockets / attachments;
   - non-destructive editing where practical.

3. **Rig & Pose**
   - automatic/quick rigging;
   - joint/skeleton editing;
   - IK where useful;
   - posing.

4. **Cleanup & Export**
   - model inspection;
   - topology/repair/remesh/finalization;
   - thickness heatmap;
   - structural warnings;
   - final STL export.

Historical milestone/version labels such as `v0.5`, `v0.9.6`, etc. are implementation history and must not be part of normal product UX.

Settings/advanced configuration should contain provider/model routing, quality presets, GPU/VRAM behavior, file locations and other technical controls.

## You have permission to redesign/rewrite

I explicitly want you to evaluate whether the current architecture should be:

- cleaned up incrementally;
- heavily refactored;
- partially rewritten;
- or rebuilt substantially/from the ground up.

Do not preserve Godot, the current additive `Main.Vxxx.cs` structure, the current Python layout, launcher split or any other architecture merely because it already exists.

At the same time, do not rewrite for novelty. Compare the options technically and choose the approach that most improves:

- reliability;
- maintainability;
- performance;
- user experience;
- local AI usability;
- Windows installation/update robustness;
- ability to run on low/medium hardware.

If Godot + C# remains the best foundation, clean it up properly. If a different architecture is clearly superior, justify and migrate to it.

## Preserve the things that matter

Whatever architecture you choose, preserve or improve these product guarantees:

- existing projects/user data are not casually lost;
- large downloaded AI models/caches are reused where compatible;
- application updates remain safe and resumable;
- a currently installed v1.0.11 user can migrate through the launcher/update path rather than manually replacing files;
- long AI/geometry jobs have clear progress/stage/elapsed-time/cancel UI;
- failures are visible and actionable;
- the app does not silently fall back in ways that hide broken CUDA/runtime configuration;
- the 3D viewport is obvious, readable and responsive;
- the UI works at realistic desktop/laptop window sizes;
- operations are non-destructive/undoable where practical;
- published releases are treated as immutable.

The current updater's data-preserving behavior is especially valuable. If you replace it, the replacement must be at least as safe.

## Performance expectations

Do not optimize solely for minimum VRAM use.

Use available GPU memory intelligently to improve speed while retaining a safe margin and graceful OOM fallback.

On a GTX 1080 8 GB machine, the app should choose sensible local models and execution modes automatically.

Avoid unnecessary RAM/VRAM transfers, avoid loading multiple heavy models simultaneously, and release models when switching roles.

Do not assume `%TEMP%` on C: has several free gigabytes. Model installs, pip builds, runtime caches and app-update staging should prefer the configured application/data drive when possible.

## Provider/model policy

Audit every currently integrated AI provider rather than assuming they all deserve to survive.

Keep providers that are genuinely useful, reliable and maintainable on Windows/local hardware. Remove or demote providers whose install complexity or maintenance burden outweighs their value.

A normal user should choose goals such as quality/speed, not memorize provider names. Advanced users may override routing.

A supported model should not unexpectedly require the user to install Git, Visual Studio Build Tools, CMake or a CUDA developer toolkit. Package/prebuild/isolate dependencies or select a better provider where possible.

You may introduce newer/better local models if they materially improve the product and fit the hardware strategy.

## UX redesign authority

I want the application to be much more user-friendly than the current prototype.

You may redesign the UI/UX substantially.

Focus on:

- a clean workflow;
- clear visual hierarchy;
- obvious primary actions;
- contextual tools instead of giant option dumps;
- good spacing and responsive layout;
- clear scene/object navigation;
- readable 3D viewport, visible grid and good model/background contrast;
- candidate comparison for AI generations;
- larger previews where visual decisions matter;
- a centralized job/progress area;
- progressive disclosure for advanced controls;
- sensible defaults so a non-artist can succeed without understanding the implementation.

Do not simply rearrange the old version-stacked controls into prettier boxes. Re-think the interaction model.

## You may add functionality I have not requested

You are encouraged to propose or implement features I have not thought of when they clearly improve the goal of helping a non-artist create 3D models.

Examples worth evaluating include:

- multi-view / turnaround concept generation;
- view consistency checking;
- automatic silhouette/background preparation before image-to-3D;
- side-by-side generation candidates;
- semantic part separation;
- symmetry-aware editing;
- smarter automatic provider routing;
- machine-learned/local generation-time estimates;
- better project history / non-destructive variants;
- guided workflows for common modeling tasks;
- automatic topology cleanup where reliable.

Do not add features merely because they are technically interesting. Every feature should support the core modeling workflow and remain maintainable.

## Release/development workflow

Do not modify a published release in place.

For implementation work:

1. start from the current stable/handoff baseline;
2. create a new development/version branch;
3. make a coherent set of changes;
4. run appropriate tests/build validation;
5. build the real Windows application/installer;
6. smoke-test installation/update behavior;
7. publish a new immutable stable GitHub Release;
8. ensure the existing launcher can discover and install it;
9. preserve AI models, caches and user data during the update.

If you decide on a major rewrite, it is acceptable to move to a new semantic version such as `1.1.0` or `2.0.0`, but you must create a safe migration path from v1.0.11.

## What I want from you before implementation

After reading and inspecting the repository, give me a serious takeover report before making large changes.

The report should include:

### 1. Your understanding of Miniscuplter

Explain the intended user, product goal and current workflow in your own words.

### 2. Current-system audit

Identify:

- architectural strengths;
- architectural debt;
- duplicated/dead/fragile code;
- UX problems;
- packaging/runtime problems;
- performance problems;
- model/provider problems;
- testing gaps;
- updater/data-migration risks.

### 3. Feature inventory

Create a clear inventory of current features and classify each as:

- keep largely as-is;
- redesign/reimplement;
- merge into another workflow;
- remove/deprecate;
- needs runtime testing.

### 4. Refactor vs rewrite decision

Compare at least:

- incremental cleanup of the current architecture;
- major internal refactor;
- substantial/greenfield rewrite.

Recommend one and explain why.

### 5. Proposed target architecture

Describe the architecture you would build, including:

- desktop UI/3D framework;
- application/domain state;
- project persistence;
- undo/history;
- local AI backend/runtime;
- model/provider management;
- long-job orchestration;
- geometry processing;
- launcher/updater strategy;
- migration from v1.0.11;
- testing/release strategy.

### 6. Proposed UX

Describe the new 2D → 3D → Rig & Pose → Cleanup & Export experience in concrete screen/workflow terms.

### 7. Development plan

Provide staged milestones that keep the application testable throughout the rewrite/refactor.

Prioritize a clean foundation and working end-to-end path over reintroducing every old feature immediately.

### 8. Improvements you recommend beyond the existing scope

Suggest additional functionality only where it clearly improves the product mission.

## Important working style

Be willing to delete/rewrite bad code.

Be conservative with user data and releases, but not with obsolete implementation choices.

Trace problems to their root cause instead of layering another compatibility patch over them.

Use the existing application as a behavioral reference and migration source, not as a constraint that prevents building a much better product.

The outcome I want is not “v1.0.11 with cleaner code.”

The outcome I want is **the best practical version of Miniscuplter you can build for a non-artist who wants to create 3D models locally on ordinary consumer hardware**.

Start by auditing the repository and return the takeover report. Do not begin the large rewrite until you have presented the report and proposed architecture/migration plan.
