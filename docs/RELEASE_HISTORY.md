# Miniscuplter Release History and Branch Map

Each release branch is preserved as a historical snapshot. Documentation-only corrections may be backported to old release branches; feature code is not silently backported.

| Branch | Milestone | Primary addition |
|---|---|---|
| `main` | v0.1 | Core editor, STL IO, basic sculpting, provider adapters |
| `v0.2` | Managed local AI | SD2.1/Hunyuan component management and hardware detection |
| `v0.3` | User image input | Artwork/reference image → AI workflow |
| `v0.4` | Voxel/remesh | Volumetric geometry context and reconstruction |
| `v0.5` | AI patch workflow | Masks, candidates, quality controls, learned ETA |
| `v0.5.5` | Stabilization | Completion/integration pass across v0.1–v0.5 |
| `v0.6` | Rigging/posing | Quick Rig, skeleton editing, posing, IK |
| `v0.7` | Parts/kitbashing | Parts library, sockets and attachment workflow |
| `v0.8` | Advanced sculpting | Expanded brush set, symmetry, masks, remesh |
| `v0.9` | Final model | Validation, repair/finalization, thickness heatmap |
| `v0.9.5` | Safety/integration | Transactional projects, guards, validated export |
| `v0.9.6` | Smart Select | Command palette and semantic selection |
| `v0.9.7` | Quality presets | Central Low/Medium/High/Ultra/custom runtime settings |
| `v0.9.8` | Multi-model AI | SDXL/FLUX/TripoSR/Hunyuan/PartCrafter routing and detail refinement |
| `v0.9.9` | Productization | Launcher, model/app updates, installer and configurable storage |
| `v1.0` | Release candidate | Full code audit, failure-path hardening, reproducible installer artifact |

## Non-release branches

Development branches such as `*-work`, `*-dev`, `*-temp`, `v0.2-ai`, `v0.2-build` and `v0.4-impl` are historical implementation branches. They are not release targets and should not be used for runtime testing unless investigating old development history.

## Product scope established by v0.9+

Miniscuplter creates and finalizes the 3D model. It does not slice, generate print supports, manage printer profiles, optimize exposure settings, or produce printer toolpaths. STL is the primary final format, but models may be used for printing, rendering, games, CAD utility, archival or another modeling application.

## Current testing target

Use `v1.0` for all current runtime validation. Older release branches exist to preserve milestones and aid regression/history investigation, not because users are expected to choose among them.
