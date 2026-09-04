# Miniscuplter v0.9 — Model Validation, Repair and Finalization

This preserved branch completes the model-output side of the original roadmap. Miniscuplter's job ends with a clean finished model/STL; it is deliberately not a slicer. This is a historical milestone; current development is on `v1.0`.

## Added in v0.9

- structural mesh analysis: watertightness, winding, boundary/non-manifold edges, components, degenerates, bounds, area and closed volume
- opt-in filled-voxel repair
- final visible-scene bake/union with source objects preserved
- voxel/memory safety estimation
- optional minimum-thickness heatmap/inspection
- validated STL finalization path

## Product-scope rule established here

Miniscuplter does **not** generate supports, slice models, manage printer profiles, choose exposure settings or produce printer toolpaths. Thickness is advisory because exported models may be used for printing, rendering, games, CAD utility or other purposes.

A valid watertight single mesh can be exported directly; voxel union is not mandatory. Intentional separate shells are also allowed when they are part of the user's design.

## Source testing

```bash
git checkout v0.9
```

Use Godot 4.7.2 .NET, .NET 8 and Python 3.10 x64. Test analysis first, then repair/final bake on copies because voxel reconstruction may soften details below the selected pitch.

## Historical status

Frozen. `v0.9.5` begins the broad integration/safety pass across everything implemented so far. See `v1.0` for current documentation.
