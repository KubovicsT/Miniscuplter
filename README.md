# Miniscuplter v0.9.6 — Smart Select and Command Palette

This preserved branch adds semantic selection and a keyboard-first command layer. It is a historical milestone; current development is on `v1.0`.

## Added in v0.9.6

- Space-key command palette
- `/s`, `/s+`, `/s-` semantic selection commands
- grow/shrink/smooth/invert/clear selection operations
- hide/show/isolate/frame commands
- remesh, analyze, thickness, rig, pose, save-part and AI-edit commands
- local CLIPSeg multi-view semantic selection
- metadata/rig-aware selection before AI fallback
- geometry fallback selection when semantic AI is unavailable
- weighted selection integrated with sculpt masks and AI edit workflows
- selection persistence in projects

## Known boundary of this milestone

Smart Select is an assistive semantic surface selector, not full 3D scene understanding. Local CLIPSeg works from rendered views and geometry fallback recognizes only supported anatomical/spatial concepts.

## Source testing

```bash
git checkout v0.9.6
```

Use Godot 4.7.2 .NET, .NET 8 and Python 3.10 x64. Test command parsing independently from CLIPSeg, then compare semantic selection with the geometry fallback.

## Historical status

Frozen. `v0.9.7` centralizes quality/performance presets across the application. See `v1.0` for current documentation.
