# Miniscuplter v0.8 — Advanced Sculpting

This preserved branch expands Miniscuplter from basic editing into a substantially broader manual sculpting toolset. It is a historical milestone; current development is on `v1.0`.

## Added in v0.8

Sculpt brushes include Draw, Smooth, Inflate, Grab, Crease, Flatten, Pinch, Scrape, Clay and SnakeHook, with:

- configurable brush radius/strength
- multiple falloff profiles
- procedural alpha behavior
- symmetry
- sculpt masks
- cursor/brush feedback
- voxel remesh integration for topology refresh
- sculpt/remesh undo support

## Known boundary of this milestone

v0.8 does not attempt to match a mature dedicated sculpting application. Dynamic topology, multiresolution sculpting, tablet-pressure refinement, imported alpha libraries and truly localized remeshing remain outside this milestone.

## Source testing

```bash
git checkout v0.8
```

Use Godot 4.7.2 .NET and .NET 8. Python 3.10 x64 is needed for AI workflows. Test every brush on a simple mesh before combining sculpting with masks/remesh.

## Historical status

Frozen. `v0.9` adds model integrity analysis, repair/finalization and the optional thickness heatmap. See `v1.0` for current documentation.
