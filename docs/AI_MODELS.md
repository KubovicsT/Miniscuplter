# Miniscuplter v1.0 AI Models

Miniscuplter uses role-based local AI routing. Installing a model does not force the app to use it for every task, and heavy specialists are not intended to stay resident in VRAM simultaneously.

## Managed components

| Component | Role | Typical use |
|---|---|---|
| Stable Diffusion 2.1 | Legacy 2D fallback | Lower-memory concept/edit route |
| Stable Diffusion XL Base 1.0 | Primary 2D | Concept generation and image/detail editing |
| FLUX.2 Klein 4B | Optional heavy 2D | Higher-end 2D generation/editing where hardware permits |
| TripoSR | Fast 3D | Rough/fast single-image reconstruction |
| Hunyuan3D 2.1 Shape | Quality 3D | Whole-object generation and selected-detail reconstruction |
| PartCrafter | Structured 3D | Multi-part generation |
| CLIPSeg | Semantic selection | Smart Select from multi-view renders |

## Automatic roles

Automatic routing considers the requested operation, installed components and detected hardware. Explicit provider selection always wins.

On an 8 GB NVIDIA GPU the intended starting stack is:

```text
SDXL
TripoSR
Hunyuan3D 2.1 Shape
CLIPSeg
```

FLUX and PartCrafter should be treated as optional stress/heavier routes on an 8 GB Pascal-class GPU.

## Model storage and revisions

Managed downloads live under `<InstallRoot>/AIData`. State records the installed Hugging Face revision and, for components with companion source repositories, the Git revision. The launcher can compare those revisions against upstream and display an update warning.

Model updates are never automatic. The user must explicitly choose Update.

## Transactional install/update behavior

v1.0 stages a complete model/tool candidate before replacing the live installation. The staged candidate is checked for required files and specialist inference imports before state is committed. If the swap fails, the previous live directories are restored where possible.

The editor must be closed before model install/remove/update operations so loaded model files are not replaced underneath the running backend.

## Shared Python environment

The backend uses one managed virtual environment under `App/ai_backend/.venv`. Specialist installers therefore avoid blindly applying upstream requirements files that would downgrade or conflict with shared packages.

Hunyuan, TripoSR and PartCrafter install reviewed inference-only extras and run import verification before the component is considered ready. `pip check` is used to detect inconsistent dependency state.

## VRAM behavior

The router releases heavy models when switching specialists. SDXL uses CPU offload on constrained CUDA hardware. Hunyuan similarly attempts CPU offload on 8 GB-class systems where supported.

Actual inference time, CUDA compatibility and memory behavior remain runtime-test items because they depend on GPU architecture, driver and third-party model implementation.

## External providers

The architecture retains optional external-provider hooks, including Automatic1111/Forge-compatible Stable Diffusion and custom command adapters. These are fallbacks/advanced integrations; the v1.0 product direction is managed local models through the launcher.
