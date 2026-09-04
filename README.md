# Miniscuplter v0.5.5 — Stabilization Pass

This preserved branch is the cleanup/completion pass for the v0.1–v0.5 feature set. It is a historical milestone; current development is on `v1.0`.

## Stabilized in v0.5.5

- geometry depth/normal handling used by AI editing
- patch anchoring and alignment behavior
- cancellation and progress handling for long AI operations
- autosave/recovery support
- diagnostics and clearer failure reporting
- integration cleanup across core editor, image input, voxel/remesh and AI patch workflows

## Purpose

v0.5.5 intentionally adds little new product scope. Its job is to make the first five milestones work together before adding rigging and posing.

## Source testing

```bash
git checkout v0.5.5
```

Use Godot 4.7.2 .NET, .NET 8 and Python 3.10 x64. Test startup, save/load, image input, AI edit masks, candidate generation, cancellation and voxel operations as an end-to-end regression pass.

## Historical status

Frozen. `v0.6` begins the rigging/posing system. For the complete current architecture and release documentation, use `v1.0`.
