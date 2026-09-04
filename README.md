# Miniscuplter v0.7 — Parts Library, Sockets and Kitbashing

This preserved branch introduces reusable parts and socket-based assembly, moving Miniscuplter toward a Hero-Forge-style construction workflow. It is a historical milestone; current development is on `v1.0`.

## Added in v0.7

- reusable parts library stored under `user://parts_library`
- part categories and metadata
- attachment sockets and mount points
- socket normals, roll, offset, rotation and scale controls
- attachment links persisted with projects
- library save/reload behavior
- non-destructive kitbashing before optional final bake/union

## Design principle

A model should be assembled from reusable editable objects for as long as possible. Final destructive union/remesh is optional and happens only when the user explicitly wants one consolidated mesh.

## Source testing

```bash
git checkout v0.7
```

Use Godot 4.7.2 .NET, .NET 8 and Python 3.10 x64. Test saving a part, reopening the app, socket placement, attachment transform changes and project persistence.

## Historical status

Frozen. `v0.8` expands manual sculpting substantially. v0.9.9 later adds a user-selectable model-library location with compatibility migration from this legacy path. See `v1.0` for current documentation.
