# Miniscuplter v0.4 — Voxel / Remesh Geometry Context

This preserved branch introduces the first volumetric geometry layer for Miniscuplter. It is a historical milestone; current development is on `v1.0`.

## Added in v0.4

- voxel/remesh geometry operations in the Python backend
- filled-volume reconstruction for mesh cleanup/combination
- geometry-aware context for later AI patching and sculpt workflows
- memory/voxel-budget thinking for constrained hardware
- non-destructive scene editing retained before volumetric operations are explicitly applied

## Why this milestone mattered

Earlier branches treated AI-generated pieces primarily as separate mesh objects. v0.4 establishes the geometry foundation used later for repair, final-model bake/union and selected-detail acceptance.

## Source testing

```bash
git checkout v0.4
```

Use Godot 4.7.2 .NET, .NET 8 and Python 3.10 x64. Voxel operations scale cubically with model dimensions and inverse pitch, so use coarse settings first on limited-RAM systems.

## Historical status

Frozen. v0.5 builds on this with the AI patch workflow, masks, candidate generation, quality controls and learned ETA. See `v1.0` for the complete architecture and release documentation.
