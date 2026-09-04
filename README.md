# Miniscuplter v0.1 — Core Editor Prototype

`main` is the preserved v0.1 historical branch. It represents the first functional editor milestone and is intentionally not the current release candidate. Current development is on `v1.0`.

## Scope

v0.1 established the single-application workflow and core editor shell:

- Godot 4.7.2 .NET desktop application
- multi-object 3D viewport with orbit, pan, zoom and frame
- STL import and binary STL export
- object move, rotate, scale, duplicate, delete and transform baking
- early sculpt brushes: Draw, Smooth, Inflate, Grab, Crease and Flatten
- undo/redo for sculpt operations
- project save/load
- viewport capture and rectangular AI edit-region selection
- AI concept/edit/generate-3D adapter workflow
- local Python backend launcher
- explicit internet reference search through Wikimedia Commons

## AI architecture at this milestone

v0.1 used provider adapters rather than bundling a complete local model stack. 2D generation/editing could use an Automatic1111/Forge-compatible API through `MINISCULPTER_SD_URL`; image-to-3D could use a custom command through `MINISCULPTER_3D_COMMAND`.

This branch predates the later managed model installer, quality presets, rigging, parts library, advanced sculpting, geometry validation, Smart Select, multi-model AI, launcher and installer.

## Source testing

Requirements: Windows 10/11 x64, Godot 4.7.2 .NET and .NET 8. Python is needed only for AI-provider testing.

```bash
git checkout main
```

Open `project.godot` in Godot 4.7.2 .NET and run the project. The editor itself is usable without an AI provider; AI operations should report missing configuration rather than crash.

## Historical status

v0.1 is frozen as the baseline editor snapshot. For the complete current architecture, build/release instructions and runtime-testing documentation, use the `v1.0` branch.
