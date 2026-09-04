# Miniscuplter v0.6 — Rigging and Posing

This preserved branch adds the first rigging, skeleton editing and posing workflow. It is a historical milestone; current development is on `v1.0`.

## Added in v0.6

- Quick Rig generation
- optional universal/external rig-provider path
- editable skeleton visualization
- pose preview, reset and apply workflow
- approximate CPU skinning
- 2-bone IK support
- rig state persisted in `.msculpt` projects
- rig/topology invalidation rules when geometry changes

## Design principle

Rigging is useful but must not become a mandatory external dependency. Quick Rig provides an in-app baseline while more advanced rig providers remain optional.

## Source testing

```bash
git checkout v0.6
```

Use Godot 4.7.2 .NET, .NET 8 and Python 3.10 x64. Test Quick Rig first, then joint editing, pose preview/reset/apply, save/reopen and post-topology-change behavior.

## Historical status

Frozen. `v0.7` adds the reusable parts library, sockets and Hero-Forge-style kitbashing workflow. See `v1.0` for current documentation.
