# Miniscuplter architectural refactor plan

This plan records the accepted direction from the 6 September 2026 takeover audit. The existing application remains the behavioral/migration baseline, not a requirement to preserve its internal structure.

## Decision

Use a **major architectural refactor with selective subsystem rewrites**.

Keep:

- Godot/.NET as the Windows desktop/rendering shell unless a concrete blocker is proven;
- the local Python boundary for inference and geometry;
- useful sculpt/mesh algorithms after correctness/performance review;
- verified model download/resume/manifests;
- compatibility with the installed launcher/update path and existing user data.

Replace rather than extend permanently:

- the historical `Main.V*.cs` partial-class application state;
- widget-owned project state and text/name-based identity;
- mesh-only global undo;
- STL as an internal project representation;
- request-lifetime-only AI job handling;
- duplicated provider capability/install/routing truth;
- version/milestone-derived UI composition.

## Stage A — safety bridge (v1.0.12)

Before distributing the replacement foundation:

- harden updater stream lifetime, rollback ownership and interrupted-transaction recovery;
- retain rollback until the new launcher demonstrates healthy startup;
- use explicit managed application ownership rather than deleting unknown install-root content;
- route recovery autosave and final export through the guarded transactional paths;
- repair the confirmed trimesh finite/topology failures and add real geometry fixtures;
- package the ray-query spatial dependency explicitly;
- repair the confirmed TripoSR source/output adapter seam;
- make v1 release gates work beyond the `v1.0.x` naming assumption.

## Stage B — new foundation and migration harness

Build the replacement application core beside the legacy implementation rather than making the new core depend on it.

### Domain model

- UUID identity for project objects, images, mesh revisions, rigs, attachments and candidates;
- immutable mesh revisions with explicit provenance;
- display names are labels only, never identity;
- selections/masks bind to a specific mesh revision;
- topology-changing commands explicitly transfer or invalidate dependent data.

### Commands/history

- every model-changing operation records affected IDs plus complete before/after state;
- transforms, mesh data and dependent metadata undo together;
- AI Apply is a normal transactional command;
- stale long-running results become candidates/conflicts instead of overwriting newer state.

### Project storage

- versioned manifest;
- indexed binary mesh assets instead of STL internally;
- atomic manifest replacement after new assets are durable;
- bounded recovery journal/checkpoints;
- legacy schema 1–6 import into a new project copy with migration log;
- originals are retained.

### Presentation

- declarative four-workspace UI: **2D → 3D → Rig & Pose → Cleanup & Export**;
- project/assets panel left, primary canvas center, contextual actions right, collapsible jobs/history below;
- one active viewport tool owns input at a time;
- no historical version headings or control-reparenting as product architecture.

### Local job broker

- authoritative queue and job IDs;
- immutable input revision per job;
- structured progress and output artifacts;
- one heavyweight GPU job by default;
- cooperative cancellation followed by isolated worker termination if required;
- cancellation is acknowledged only after the worker is actually stopped;
- install/remove/repair shares the same runtime ownership lock.

### Provider/storage services

- one provider registry: pinned code/weights/env, roles, hardware/platform requirements, licenses, self-tests and benchmark presets;
- separate states for downloaded / installed / importable / device-tested / inference-tested;
- one storage service for weights, environments, caches, projects, logs and job temporary files;
- configured non-system storage applies to inference intermediates and caches as well as downloads.

## Stage C — complete thin slice

Prove the new foundation end to end before migrating every feature:

1. import or generate 2D;
2. approve a durable baseline revision;
3. generate through one qualified lightweight 3D provider;
4. save/reload;
5. basic cleanup;
6. preview exact export scope and export STL.

Acceptance target: GTX 1080 8 GB + 16 GB RAM, with measured peak VRAM/RAM, cancellation recovery and offline use after required downloads.

## Stage D — practical editing

- spatial acceleration for picking;
- cached brush neighborhoods / incremental mesh updates;
- correct full-state undo for sculpt/transform/topology edits;
- protected regions;
- reusable parts/sockets/attachments;
- candidate comparison and model experiment branches.

## Stage E — rig/pose and regional improvement

- true independent rest mesh and reversible pose state;
- landmark-assisted rig setup with user correction;
- improved skinning/IK behind understandable controls;
- guided region improvement: select → describe → candidates → compare → place → apply/discard;
- protected geometry tolerance checked before apply.

## Stage F — beta/stable transition

- qualified default model bundle based on measured target-hardware results;
- clean/install/upgrade/low-disk/interruption matrix;
- legacy project migration fixtures;
- recovery UI and named checkpoints;
- supported provider self-tests;
- remove replaced legacy implementation only after migration and acceptance coverage exists.

## Product-level additions to prioritize

Early:

- before/after comparison and named checkpoints;
- example project / guided first successful model;
- silhouette, orientation and background guidance before 3D generation;
- resource-aware quality presets with disk/RAM/VRAM expectations;
- guided kitbashing and part attachment;
- simple grounding, symmetry, alignment, scale and measurement helpers.

Later, after the core workflow is reliable:

- richer GLB interchange where it preserves rig/material information;
- multi-view consistency assistance;
- constrained region replacement.

Do not prioritize cloud inference, slicing/support generation, a provider marketplace, animation-suite scope or a node graph before the core modeling workflow is reliable.
