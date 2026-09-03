# Miniscuplter v0.1

Miniscuplter is an experimental single-application workflow for AI-assisted miniature sculpting and STL preparation.

## Current v0.1 scope

- Desktop 3D viewport with orbit, pan, zoom and framing
- Multiple mesh objects for non-destructive kitbashing
- STL import and binary STL export
- Sculpt brushes: Draw, Smooth, Inflate, Grab, Crease and Flatten
- Undo/redo for sculpt operations
- Object duplicate/delete, move, rotate, scale and transform baking
- View capture for AI editing
- AI concept generation hook
- Capture -> 2D AI edit -> approved image -> 3D part workflow
- AI-generated parts are added as separate scene objects first
- Internet reference search from the AI panel (Wikimedia Commons)
- Basic print-prep mesh statistics and build-plane placement
- Local AI backend that starts with the app when Python is available
- Provider adapters rather than hard-coding one AI model

## Important v0.1 limitations

This is the first functional editor prototype, not a production sculpting package. The UI exposes the complete intended workflow, but two computationally heavy systems are intentionally adapter-based in v0.1:

1. **2D generation/editing** requires an Automatic1111/Forge-compatible local API configured with `MINISCULPTER_SD_URL`.
2. **Image-to-3D generation** requires a local generator command configured with `MINISCULPTER_3D_COMMAND`.

Voxel remeshing/boolean union and advanced rig posing are represented in the editor but require the planned native geometry backend. Until then, AI parts remain separate and editable, which is also the preferred non-destructive editing workflow.

## Requirements for source testing

- Windows 10/11 64-bit
- Godot Engine 4.7.2 .NET edition
- .NET 8 SDK
- Python 3.11+ for the AI service

Godot 4.7.2 .NET is the version targeted by the project file.

## First run

1. Clone this repository.
2. Run `setup_ai_backend.bat` once.
3. Open `project.godot` with **Godot 4.7.2 .NET**.
4. Press **F6/F5** to run Miniscuplter.
5. The editor should open with a starter sphere in the viewport.

The core editor works without an AI provider. AI buttons will report that a provider is not configured instead of crashing.

## Configure 2D AI

Run an Automatic1111/Forge-compatible Stable Diffusion API locally and set, for example:

```bat
setx MINISCULPTER_SD_URL http://127.0.0.1:7860
```

Restart Miniscuplter after setting the variable.

The backend uses `/sdapi/v1/txt2img` for concepts and `/sdapi/v1/img2img` for viewport edits.

## Configure image-to-3D

`MINISCULPTER_3D_COMMAND` is a command template. It must accept the input image and write an STL to the requested output path.

Available placeholders:

- `{image}` - approved 2D image path
- `{output}` - requested STL output path
- `{prompt}` - current text prompt

Example shape:

```bat
setx MINISCULPTER_3D_COMMAND "python C:\AI\my_3d_adapter.py --image \"{image}\" --output \"{output}\" --prompt \"{prompt}\""
```

The point of this adapter is that Miniscuplter can later ship Hunyuan3D, TripoSR or another backend without changing the editor workflow.

## Internet/reference access

Internet access is **explicit and separable from local generation**. In the AI tab, enable **Allow internet reference search**, enter a reference request in the prompt field, and select **Search references from prompt**. v0.1 searches Wikimedia Commons and opens chosen references in the system browser.

This is intentional: a local model can remain offline while Miniscuplter itself retrieves references only when the user requests them. A future provider can also consume the returned reference images as conditioning inputs.

## AI detail-edit workflow

1. Rotate the model to the desired view.
2. Select **Capture View**.
3. Describe the desired change.
4. Select **Capture -> 2D AI Edit**.
5. Review/regenerate the 2D result until satisfied.
6. Select **Approved 2D -> Generate 3D Part**.
7. The returned STL is added as a separate object.
8. Move/rotate/scale/sculpt it before a future bake/remesh operation.

The architecture deliberately postpones expensive 3D generation until after the 2D design is approved.

## Controls

- Left mouse: sculpt
- Right mouse: orbit
- Middle mouse: pan
- Mouse wheel: zoom
- `Frame`: focus camera on selected object

## Repository layout

```text
Scenes/              Godot scenes
Scripts/             C# editor, mesh IO, sculpting and AI client
ai_backend/          Local Python AI gateway
.github/workflows/   Windows .NET build check
```

## v0.1 testing priorities

Please test startup first, then viewport navigation, STL import/export, each sculpt brush, undo/redo, object transforms, captures, reference search, and finally AI-provider integration if you have one configured.

When reporting a problem, include what you clicked, what you expected, what happened, and any Godot console error text.
