# Miniscuplter v0.9.7 — Central Quality Presets

This preserved branch centralizes performance/quality controls so AI, geometry and Smart Select operate from one coherent preset system. It is a historical milestone; current development is on `v1.0`.

## Added in v0.9.7

Built-in Low, Medium, High and Ultra presets plus user-defined custom presets control:

- 2D image resolution, steps, guidance and edit strength
- maximum input-image size
- Hunyuan shape steps
- sculpt/remesh voxel pitch
- repair/finalization voxel pitch
- voxel safety budget
- thickness sample budget
- Smart Select view count and render size

Preset selection is persisted, can be pushed to the Python backend, and is recommended from detected hardware without preventing explicit user choice.

## Design principle

The preset answers **how hard an operation should work**. Later model routing independently answers **which specialist model should perform it**.

## Source testing

```bash
git checkout v0.9.7
```

Use Godot 4.7.2 .NET, .NET 8 and Python 3.10 x64. Verify preset persistence, backend synchronization and behavior differences across AI, remesh, repair and Smart Select.

## Historical status

Frozen. `v0.9.8` adds multi-model AI routing and local selected-detail refinement. See `v1.0` for current documentation.
