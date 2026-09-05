# Miniscuplter v1.0.5 AI Models

Miniscuplter uses role-based local AI routing. Installing a model does not force the app to use it for every task, and heavy specialists are not intended to stay resident in VRAM simultaneously.

## Managed components

| Component | Role | Typical selected model payload* | Typical use |
|---|---|---:|---|
| Stable Diffusion 2.1 Base | Legacy 2D fallback | ~2.6 GB | Lower-memory concept/edit route |
| Stable Diffusion XL Base 1.0 | Primary 2D | ~7.0 GB | Concept generation and image/detail editing |
| FLUX.2 Klein 4B | Heavy 2D | ~16 GB | Higher-end 2D generation/editing |
| Z-Image Turbo | Modern 2D | ~33 GB | Fast modern concept generation |
| Qwen-Image-2512 | High-end 2D | ~58 GB | High-quality concept generation |
| Qwen-Image-Edit | High-end 2D edit | ~58 GB | Semantic image/detail editing |
| TripoSR | Fast 3D | ~1.7 GB | Rough/fast single-image reconstruction |
| Hunyuan3D 2.1 Shape | Quality 3D | ~7.4 GB | Whole-object generation and selected-detail reconstruction |
| Hunyuan3D 2mini | Efficient 3D | ~3.9 GB | Resource-efficient image-to-shape route |
| Stable Fast 3D | Fast 3D | ~4.1 GB | Fast mesh reconstruction |
| SPAR3D | 3D | ~7.4 GB | Point-aware reconstruction, including low-VRAM mode |
| PartCrafter + RMBG | Structured 3D | ~4.8 GB | Multi-part generation with background removal |
| PartPacker | Structured 3D | ~3.2 GB | NVIDIA part-level generation |
| CLIPSeg | Semantic selection | ~0.6 GB | Smart Select from multi-view renders |
| TRELLIS.2 bridge | External 3D runtime | No Windows-managed weights | Connects to a configured Linux/WSL2 TRELLIS.2 runtime |

\* These figures describe selected model files, not the total disk footprint of provider source trees, Python environments, CUDA/PyTorch packages, caches, or external runtimes. They are only planning figures. Before every Hugging Face transfer Miniscuplter queries the pinned upstream revision with file metadata and displays/verifies the exact selected payload in bytes.

## Audited download manifests

The launcher does not download entire Hugging Face repositories indiscriminately. Large repositories frequently contain duplicate FP32/FP16 checkpoints, PyTorch and Safetensors copies, ONNX/OpenVINO exports, Flax weights, demos and single-file checkpoints in addition to Diffusers component folders.

v1.0.5 therefore uses explicit manifests for each managed model. Important examples:

- SDXL installs only the FP16 Safetensors Diffusers components used by `StableDiffusionXLPipeline`. Root single-file SDXL checkpoints and FP32/ONNX/OpenVINO/Flax alternatives are excluded.
- SD 2.1 likewise installs its FP16 Safetensors Diffusers components only.
- Hunyuan3D 2mini installs only `hunyuan3d-dit-v2-mini/config.yaml` and `model.fp16.safetensors`. The selected checkpoint already carries the model, VAE and conditioner state used by Miniscuplter's pipeline.
- Hunyuan3D 2.1 Shape installs only the self-contained `hunyuan3d-dit-v2-1` FP16 checkpoint/config used by the current inference path.
- Stable Fast 3D and SPAR3D install their local `config.yaml` + `model.safetensors` payloads and their runtime adapters explicitly point the upstream scripts at those local weights.
- PartPacker installs only `vae.pt` and `flow.pt` for its model payload. Its isolated provider environment consumes additional disk space.
- PartCrafter retains the full official PartCrafter and RMBG snapshots because the unmodified official inference script explicitly resolves both local snapshots during execution.

After download, every upstream file for which Hugging Face supplies size metadata is checked against that exact expected byte count before the staged installation can become live.

## Interrupted downloads and resume

Model installs and updates use deterministic staging under `<DataRoot>/.staging` and retain safe partial data when cancelled, interrupted or failed. On the next launcher start Miniscuplter reports the interrupted operation and offers Resume.

Resume does not blindly trust staged files. It re-checks the upstream revision and the Miniscuplter manifest signature first. If either changed, the stale partial payload is discarded. If they still match, Hugging Face/Xet reuses valid partial files and continues the transfer. Staged files left by the old v1.0.5 UUID staging layout are migrated when possible.

An old over-broad SDXL stage is also pruned against the current audited manifest before more data is downloaded, removing files that are no longer selected.

The model operation window has an explicit Cancel action. Closing the window during an active operation asks whether to cancel; the process tree is terminated through the launcher's owned-process lifetime protection and valid partial data is preserved for a later resume.

## Xet transport

The managed AI runtime explicitly installs `hf_xet`. Hugging Face repositories that use Xet therefore use the supported Xet transport instead of silently falling back to slower regular HTTP downloads.

Progress bars and Hugging Face warnings may be written to stderr even when no error occurred. The launcher classifies these as progress/warnings rather than labeling every stderr line as an error.

## Automatic roles

Automatic routing considers the requested operation, installed components and detected hardware. Explicit provider selection always wins.

On an approximately 8 GB NVIDIA GPU, the current launcher starting recommendation is:

```text
SDXL
Stable Fast 3D
Hunyuan3D 2mini
CLIPSeg
```

Hunyuan3D 2.1, FLUX, PartCrafter and the larger modern image models are optional heavier routes. Actual viability also depends on GPU architecture, system RAM, driver/runtime compatibility and the upstream provider implementation.

## Model storage and revisions

Managed downloads live under `<DataRoot>`, normally `<InstallRoot>/AIData` unless the launcher data root has been changed. State records the installed Hugging Face revision and, for components with companion source repositories, the Git revision. The launcher can compare those revisions against upstream and display an update warning.

Model updates are never automatic. The user must explicitly choose Update.

## Transactional install/update behavior

v1.0.5 stages and verifies a complete model/tool candidate before replacing the live installation. If staging, verification or the final swap fails, the existing installed component remains untouched where possible.

The editor must be closed before model install/remove/update operations so loaded model files are not replaced underneath the running backend.

## Python environments and disk footprint

The core backend uses one managed virtual environment under `App/ai_backend/.venv`. Some specialist providers intentionally use isolated environments to prevent upstream dependency conflicts. Those environments can add several gigabytes beyond the model-weight payload shown in the model table.

AI runtime repair does not download model weights. The launcher opens a visible setup console while runtime repair is running so Python/PyTorch/package download and installation progress is observable.

## VRAM behavior

The router releases heavy models when switching specialists. SDXL uses CPU offload on constrained CUDA hardware. Hunyuan similarly attempts CPU offload where supported.

Actual inference time, CUDA compatibility and memory behavior remain runtime-test items because they depend on GPU architecture, driver and third-party model implementation.

## External providers

The architecture retains optional external-provider hooks, including Automatic1111/Forge-compatible Stable Diffusion and custom command adapters. TRELLIS.2 is currently treated as a Linux/WSL2 external-runtime bridge; its weights are not downloaded into the Windows-managed model store by the launcher.
