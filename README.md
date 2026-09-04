# Miniscuplter v0.3 — User Artwork / Image Input

This preserved branch extends the managed-local-AI workflow so user-provided artwork and reference images can directly enter the creation pipeline. It is a historical milestone; current development is on `v1.0`.

## Added in v0.3

- user image/artwork import into the AI workflow
- image-driven concept/edit/generation flow rather than requiring viewport capture as the only source
- clearer separation between source image, edited image and generated 3D result
- continuation of local SD2.1 + Hunyuan3D routing from v0.2

## Inherited systems

v0.3 retains the core editor, STL import/export, object transforms, basic sculpting, save/load, viewport capture, regional masks, references and managed AI component handling from v0.1–v0.2.

## Source testing

```bash
git checkout v0.3
```

Use Godot 4.7.2 .NET, .NET 8 and Python 3.10 x64. Run `setup_ai_backend.bat` before testing managed local AI.

## Historical status

Frozen. The next milestone (`v0.4`) introduces voxel/remesh geometry context so AI and editing can reason about model volume rather than only independent surface meshes. For the full current product documentation, use `v1.0`.
