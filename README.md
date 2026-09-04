# Miniscuplter v0.9.8 — Multi-Model AI and Local Detail Refinement

This preserved branch replaces the earlier fixed-model assumption with role-based local AI routing and selected-detail refinement. It is a historical milestone; current development is on `v1.0`.

## Added in v0.9.8

Managed specialist roles include:

- SDXL as the primary modern 2D route on 8 GB-class hardware
- SD2.1 as a legacy low-memory fallback
- FLUX.2 Klein 4B as an optional heavier 2D specialist
- TripoSR for fast/rough image-to-3D
- Hunyuan3D 2.1 Shape for quality whole-object and selected-detail 3D
- PartCrafter for structured multi-part generation
- CLIPSeg for semantic Smart Select

The router respects explicit user provider choices and otherwise selects by operation role, installed models and hardware. Heavy specialists are released before switching so they do not all remain resident in VRAM.

## Detail workflow

- selected 2D regions can be cropped/refined and composited back through the mask
- selected 3D regions can generate an aligned detail patch as a preview
- patch acceptance uses volumetric union only after preview/approval
- the source mesh remains untouched until apply succeeds and Undo remains available

## Source testing

```bash
git checkout v0.9.8
```

Use Godot 4.7.2 .NET, .NET 8 and Python 3.10 x64. On an 8 GB GPU, start with SDXL + TripoSR + Hunyuan Shape + CLIPSeg before stress-testing FLUX or PartCrafter.

## Historical status

Frozen and CI-green at its release head. `v0.9.9` adds the launcher, installation/update system and user-configurable storage locations. See `v1.0` for current documentation.
