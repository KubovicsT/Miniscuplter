# Miniscuplter v0.5 — AI Patch Workflow

This preserved branch introduces the first complete AI-assisted patch/refinement workflow on top of the v0.4 volumetric geometry foundation. It is a historical milestone; current development is on `v1.0`.

## Added in v0.5

- paint/region masks for localized AI work
- multi-candidate AI patch generation and preview
- explicit accept/reject/regenerate workflow before destructive geometry changes
- patch anchoring/alignment to the source model
- quality presets for AI/geometry operations
- learned local ETA estimates based on previous runs rather than fixed guesses
- cancellation/progress plumbing for long AI jobs

## Design principle

Expensive 3D generation is delayed until the user approves the intended 2D/design change. Generated detail remains non-destructive until explicitly accepted.

## Source testing

```bash
git checkout v0.5
```

Use Godot 4.7.2 .NET, .NET 8 and Python 3.10 x64. Test mask creation, candidate generation, cancellation, preview, acceptance and undo separately before combining them into a long workflow.

## Historical status

Frozen. `v0.5.5` is the stabilization/completion pass for v0.1–v0.5. Later branches add rigging, kitbashing, advanced sculpting and final-model validation. See `v1.0` for current documentation.
