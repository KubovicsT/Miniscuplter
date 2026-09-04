# Miniscuplter v0.9.5 — Integration and Safety Hardening

This preserved branch is the first broad feature-completion/stability pass across the v0.1–v0.9 product. It is a historical milestone; current development is on `v1.0`.

## Hardened in v0.9.5

- transactional project saving
- recovery-aware/strict project loading
- safe destructive mesh operations with rollback/undo expectations
- portable project assets
- parts-library safety guards
- AI cancellation handling
- rig validation and topology invalidation
- sculpt/remesh undo integration
- attachment/load/export integrity guards
- validated STL export as the authoritative final-output path

## Purpose

The goal was not new feature breadth; it was to make existing systems coexist safely and reduce silent corruption or destructive failure modes.

## Source testing

```bash
git checkout v0.9.5
```

Use Godot 4.7.2 .NET, .NET 8 and Python 3.10 x64. Regression-test project save/load/recovery, parts, rigs, sculpt/remesh undo, AI cancellation and STL export.

## Historical status

Frozen. `v0.9.6` adds Smart Select and the command palette. See `v1.0` for current documentation.
