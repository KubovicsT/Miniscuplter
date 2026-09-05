# Miniscuplter v1.0.5

Miniscuplter is a Windows desktop application for **AI-assisted 3D model creation, kitbashing, posing, sculpting, local detail refinement, model validation/repair and final STL export**.

v1.0.5 builds on the validated v1.0 release candidate and expands the local AI layer into a hardware-aware provider system. Existing v1.0 providers remain supported; new models are optional installs rather than replacements.

## Product boundary

Miniscuplter creates the finished 3D model. It is not a slicer: support generation, slicing, printer profiles and printer toolpaths remain outside the application.

## AI provider tiers

| Role | Lower hardware | Mid tier | High/workstation tier |
|---|---|---|---|
| Concept | SDXL | Z-Image Turbo / FLUX.2 Klein | Qwen-Image-2512 |
| Image edit/detail | SDXL | FLUX.2 Klein | Qwen-Image-Edit |
| Fast image → 3D | TripoSR | Stable Fast 3D | SPAR3D |
| Quality image → 3D | Hunyuan3D 2mini | Hunyuan3D 2.1 | TRELLIS.2 4B |
| Structured parts | PartCrafter | PartCrafter | PartPacker |
| Smart Select | CLIPSeg + rig/metadata/geometry | same hybrid selector | same hybrid selector |

The launcher/backend component status now exposes role, tier, VRAM guidance, native-platform support and hardware-fit metadata. Auto routing considers detected VRAM and installed providers. Explicit user selection always wins and unsupported/uninstalled providers fail clearly rather than silently changing models.

### Windows compatibility boundary

Stable Fast 3D and SPAR3D have upstream experimental Windows support and are installed into isolated per-provider Python environments to protect Miniscuplter's shared AI runtime. PartPacker likewise uses an isolated runtime. TRELLIS.2 upstream officially targets Linux with >=24 GB NVIDIA VRAM; Miniscuplter manages its source/capability and invokes a configured native-Linux/WSL2 runtime through `MINISCULPTER_TRELLIS2_COMMAND` instead of pretending it is a native Windows provider.

## Recommended GTX 1080 / 8 GB starting set

```text
SDXL
Hunyuan3D 2mini
Stable Fast 3D
TripoSR
CLIPSeg
```

SPAR3D low-VRAM mode is available as an additional experiment. Hunyuan3D 2.1 remains useful with offload. Qwen, TRELLIS.2 and PartPacker are intended primarily for larger GPUs.

## Release / update path

GitHub Actions builds both:

```text
Miniscuplter-Setup-1.0.5.exe
Miniscuplter-win-x64.zip
```

The launcher application updater checks the repository's **latest published GitHub Release**, compares its tag against the launcher's assembly version, and accepts only an asset named exactly `Miniscuplter-win-x64.zip`. Therefore an installed v1.0 can update in place to v1.0.5 once v1.0.5 is published as the latest GitHub Release with that ZIP asset. The updater preserves the installed `App/ai_backend/.venv`; if backend runtime requirements change, the launcher's runtime fingerprint intentionally asks for **Repair AI Runtime** rather than using a stale environment.

## Documentation

- `docs/ARCHITECTURE.md` — application architecture
- `docs/AI_MODELS.md` — provider/model architecture and hardware tiers
- `docs/BUILD_AND_RELEASE.md` — CI and release packaging
- `docs/RUNTIME_TESTING.md` — runtime validation protocol
- `docs/RELEASE_HISTORY.md` — milestone/branch history

## Validation boundary

CI/static validation is required before v1.0.5 is considered code-green. Actual CUDA inference for every optional model still requires runtime testing on representative hardware; upstream Windows support for some specialist models is explicitly experimental.
