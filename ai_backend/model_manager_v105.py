from __future__ import annotations

import os
import shutil
import subprocess
import sys
from pathlib import Path
from typing import Any

import model_manager as mm
from model_capabilities import BY_ID
from model_downloads import clear_stage, download_verified, prepare_stage, stage_status

# Approximate *selected model payloads*. The installer queries Hugging Face with
# files_metadata=True before each download and reports/verifies the exact bytes. Provider
# virtual environments/source trees can add extra disk usage and are intentionally not
# represented as model-weight size.
AUDITED_PAYLOAD_GB: dict[str, float] = {
    "sd21": 2.5,
    "sdxl-base": 7.0,
    "flux2-klein-4b": 16.0,
    "hunyuan21-shape": 8.1,
    "triposr": 1.7,
    "partcrafter": 4.2,
    "clipseg-smart-select": 0.7,
    "z-image-turbo": 33.0,
    "qwen-image-2512": 58.0,
    "qwen-image-edit": 58.0,
    "hunyuan2mini": 4.3,
    "sf3d": 4.1,
    "spar3d": 7.4,
    "partpacker": 3.2,
    # Miniscuplter currently manages the TRELLIS.2 integration/source bridge only.
    # The Linux/WSL runtime referenced by MINISCULPTER_TRELLIS2_COMMAND owns its weights.
    "trellis2": 0.0,
}

NEW_COMPONENTS: dict[str, dict[str, Any]] = {
    "z-image-turbo": {"name":"Z-Image Turbo", "kind":"image", "source":"huggingface", "repo_id":"Tongyi-MAI/Z-Image-Turbo", "description":"Fast modern concept generator; CPU offload supported.", "estimated_gb":AUDITED_PAYLOAD_GB["z-image-turbo"]},
    "qwen-image-2512": {"name":"Qwen-Image-2512", "kind":"image", "source":"huggingface", "repo_id":"Qwen/Qwen-Image-2512", "description":"High-end concept image generator.", "estimated_gb":AUDITED_PAYLOAD_GB["qwen-image-2512"]},
    "qwen-image-edit": {"name":"Qwen-Image-Edit", "kind":"image-edit", "source":"huggingface", "repo_id":"Qwen/Qwen-Image-Edit", "description":"High-end semantic image/detail editor.", "estimated_gb":AUDITED_PAYLOAD_GB["qwen-image-edit"]},
    "hunyuan2mini": {"name":"Hunyuan3D 2mini", "kind":"3d", "source":"github+huggingface", "repo_id":"tencent/Hunyuan3D-2mini", "code_url":"https://github.com/Tencent-Hunyuan/Hunyuan3D-2.git", "description":"0.6B resource-efficient image-to-shape provider.", "estimated_gb":AUDITED_PAYLOAD_GB["hunyuan2mini"]},
    "sf3d": {"name":"Stable Fast 3D", "kind":"3d", "source":"github+huggingface", "repo_id":"stabilityai/stable-fast-3d", "code_url":"https://github.com/Stability-AI/stable-fast-3d.git", "description":"Fast mesh reconstruction; upstream Windows support is experimental.", "estimated_gb":AUDITED_PAYLOAD_GB["sf3d"]},
    "spar3d": {"name":"SPAR3D", "kind":"3d", "source":"github+huggingface", "repo_id":"stabilityai/stable-point-aware-3d", "code_url":"https://github.com/Stability-AI/stable-point-aware-3d.git", "description":"Point-aware reconstruction with ~7GB low-VRAM mode.", "estimated_gb":AUDITED_PAYLOAD_GB["spar3d"]},
    "partpacker": {"name":"PartPacker", "kind":"3d-parts", "source":"github+huggingface", "repo_id":"nvidia/PartPacker", "code_url":"https://github.com/NVlabs/PartPacker.git", "description":"NVIDIA part-level generation; official inference is ~10GB VRAM fp16.", "estimated_gb":AUDITED_PAYLOAD_GB["partpacker"]},
    "trellis2": {"name":"TRELLIS.2 4B (external runtime bridge)", "kind":"3d", "source":"github", "repo_id":"microsoft/TRELLIS.2-4B", "code_url":"https://github.com/microsoft/TRELLIS.2.git", "description":"Installs the Miniscuplter integration/source bridge only. TRELLIS.2 weights/runtime are managed by the configured Linux/WSL2 command and are not downloaded by the Windows launcher.", "estimated_gb":AUDITED_PAYLOAD_GB["trellis2"]},
}

mm.COMPONENTS.update(NEW_COMPONENTS)
for _cid, _gb in AUDITED_PAYLOAD_GB.items():
    if _cid in mm.COMPONENTS:
        mm.COMPONENTS[_cid]["estimated_gb"] = _gb

_legacy_status = mm.status

# Explicit payload manifests. Never use a whole HF snapshot where a repo contains
# alternative precisions/formats or duplicate single-file checkpoints.
PATTERNS: dict[str, list[str] | None] = {
    "sd21": [
        "model_index.json", "scheduler/**", "feature_extractor/**", "tokenizer/**",
        "text_encoder/config.json", "text_encoder/model.fp16.safetensors",
        "unet/config.json", "unet/diffusion_pytorch_model.fp16.safetensors",
        "vae/config.json", "vae/diffusion_pytorch_model.fp16.safetensors",
    ],
    "sdxl-base": [
        "model_index.json", "scheduler/**", "tokenizer/**", "tokenizer_2/**",
        "text_encoder/config.json", "text_encoder/model.fp16.safetensors",
        "text_encoder_2/config.json", "text_encoder_2/model.fp16.safetensors",
        "unet/config.json", "unet/diffusion_pytorch_model.fp16.safetensors",
        "vae/config.json", "vae/diffusion_pytorch_model.fp16.safetensors",
    ],
    "flux2-klein-4b": ["model_index.json", "scheduler/**", "text_encoder/**", "tokenizer/**", "transformer/**", "vae/**"],
    "hunyuan21-shape": ["hunyuan3d-dit-v2-1/**", "hunyuan3d-vae-v2-1/**"],
    "triposr": ["config.yaml", "model.ckpt"],
    "clipseg-smart-select": ["config.json", "preprocessor_config.json", "tokenizer_config.json", "special_tokens_map.json", "vocab.json", "merges.txt", "model.safetensors"],
    "z-image-turbo": ["model_index.json", "scheduler/**", "text_encoder/**", "tokenizer/**", "transformer/**", "vae/**"],
    "qwen-image-2512": ["model_index.json", "scheduler/**", "text_encoder/**", "tokenizer/**", "transformer/**", "vae/**"],
    "qwen-image-edit": ["model_index.json", "processor/**", "scheduler/**", "text_encoder/**", "tokenizer/**", "transformer/**", "vae/**"],
    "hunyuan2mini": [
        "hunyuan3d-dit-v2-mini/config.yaml", "hunyuan3d-dit-v2-mini/model.fp16.safetensors",
        "hunyuan3d-vae-v2-mini/config.yaml", "hunyuan3d-vae-v2-mini/model.fp16.safetensors",
    ],
    "sf3d": ["config.yaml", "model.safetensors"],
    "spar3d": ["config.yaml", "model.safetensors"],
    "partpacker": ["vae.pt", "flow.pt"],
}


def _signature(component_id: str) -> str:
    patterns = PATTERNS.get(component_id)
    return "manifest-v3:" + component_id + ":" + ("|".join(patterns) if patterns is not None else "special")


def _venv_python(root: Path) -> Path:
    return root / ".venv" / ("Scripts/python.exe" if os.name == "nt" else "bin/python")


def _make_venv(root: Path) -> Path:
    py = _venv_python(root)
    if not py.exists():
        bad = root / ".venv"
        if bad.exists(): shutil.rmtree(bad, ignore_errors=True)
        subprocess.run([sys.executable, "-m", "venv", str(bad)], check=True)
        py = _venv_python(root)
    subprocess.run([str(py), "-m", "pip", "install", "-U", "pip", "wheel", "setuptools"], check=True)
    return py


def _install_requirements(py: Path, code: Path, extra: list[str] | None = None) -> None:
    req = code / "requirements.txt"
    if req.exists(): subprocess.run([str(py), "-m", "pip", "install", "-r", str(req)], cwd=code, check=True)
    if extra: subprocess.run([str(py), "-m", "pip", "install", *extra], cwd=code, check=True)
    subprocess.run([str(py), "-m", "pip", "check"], cwd=code, check=True)


def _ensure_clone(url: str, target: Path) -> None:
    if (target / ".git").is_dir():
        return
    if target.exists(): shutil.rmtree(target, ignore_errors=True)
    mm._clone_fresh(url, target)


def _commit_state(component_id: str, final: Path, hf_rev: str | None, tool_rev: str | None) -> None:
    state = mm.load_state()
    state.setdefault("installed", {})[component_id] = {"installed":True, "path":str(final), "hf_revision":hf_rev, "tool_revision":tool_rev}
    mm.save_state(state)


def _finalize(stage: Path, replacements: list[tuple[Path, Path]], component_id: str, final: Path, hf_rev: str | None, tool_rev: str | None) -> dict[str, Any]:
    mm._swap_staged(replacements, lambda: _commit_state(component_id, final, hf_rev, tool_rev))
    clear_stage(stage)
    return {"id":component_id, "installed":True, "path":str(final), "hardware":mm.hardware_info()}


def _require_space(spec: dict[str, Any]) -> None:
    # Transactional installs temporarily need both an old and a new copy. Use a conservative
    # floor here; exact HF payload bytes are queried and printed immediately before download.
    need = float(spec.get("estimated_gb", 0) or 0) * 1.15 + 1.0
    disk = mm._disk_info()
    if disk["free_gb"] < need:
        raise RuntimeError(f"About {need:.1f} GB free is required for the selected model payload. Provider runtime/source files can require additional space.")


def install_component(component_id: str, update: bool = False) -> dict[str, Any]:
    if component_id not in mm.COMPONENTS:
        raise ValueError(f"Unknown AI component: {component_id}")
    spec = mm.COMPONENTS[component_id]
    mm.MODELS_ROOT.mkdir(parents=True, exist_ok=True)
    mm.TOOLS_ROOT.mkdir(parents=True, exist_ok=True)
    mm.STAGING_ROOT.mkdir(parents=True, exist_ok=True)
    _require_space(spec)

    hf_rev = mm._hf_revision(spec["repo_id"]) if spec.get("repo_id") else None
    action = "update" if update else "install"
    stage = prepare_stage(mm.STAGING_ROOT, component_id, hf_rev, _signature(component_id), action)
    tool_rev: str | None = None
    replacements: list[tuple[Path, Path]] = []

    try:
        if component_id == "sd21":
            target = stage / "models" / "stable-diffusion-2-1-base"; final = mm.MODELS_ROOT / "stable-diffusion-2-1-base"
            download_verified(spec["repo_id"], target, hf_rev, PATTERNS[component_id]); replacements.append((target, final))
        elif component_id == "sdxl-base":
            target = stage / "models" / "stable-diffusion-xl-base-1.0"; final = mm.MODELS_ROOT / "stable-diffusion-xl-base-1.0"
            download_verified(spec["repo_id"], target, hf_rev, PATTERNS[component_id]); replacements.append((target, final))
        elif component_id == "flux2-klein-4b":
            target = stage / "models" / "FLUX.2-klein-4B"; final = mm.MODELS_ROOT / "FLUX.2-klein-4B"
            download_verified(spec["repo_id"], target, hf_rev, PATTERNS[component_id]); replacements.append((target, final))
        elif component_id == "hunyuan21-shape":
            code = stage / "tools" / "Hunyuan3D-2.1"; final_code = mm.TOOLS_ROOT / "Hunyuan3D-2.1"
            _ensure_clone(spec["code_url"], code); mm._install_hunyuan_dependencies(code)
            target = stage / "models" / "Hunyuan3D-2.1"; final = mm.MODELS_ROOT / "Hunyuan3D-2.1"
            download_verified(spec["repo_id"], target, hf_rev, PATTERNS[component_id]); tool_rev = mm._git_local_revision(code)
            replacements.extend([(code, final_code), (target, final)])
        elif component_id == "triposr":
            code = stage / "tools" / "TripoSR"; final_code = mm.TOOLS_ROOT / "TripoSR"
            _ensure_clone(spec["code_url"], code); mm._install_triposr_dependencies(code)
            target = stage / "models" / "TripoSR"; final = mm.MODELS_ROOT / "TripoSR"
            download_verified(spec["repo_id"], target, hf_rev, PATTERNS[component_id]); tool_rev = mm._git_local_revision(code)
            replacements.extend([(code, final_code), (target, final)])
        elif component_id == "partcrafter":
            target = stage / "tools" / "PartCrafter"; final = mm.TOOLS_ROOT / "PartCrafter"
            _ensure_clone(spec["code_url"], target); mm._install_partcrafter_dependencies(target)
            download_verified(spec["repo_id"], target / "pretrained_weights" / "PartCrafter", hf_rev, None)
            rmbg_repo = "briaai/RMBG-1.4"; rmbg_rev = mm._hf_revision(rmbg_repo)
            # RMBG is small compared with PartCrafter; keep its complete HF snapshot because
            # upstream code may select either PyTorch or safetensors depending on revision.
            download_verified(rmbg_repo, target / "pretrained_weights" / "RMBG-1.4", rmbg_rev, None)
            tool_rev = mm._git_local_revision(target); replacements.append((target, final))
        elif component_id == "clipseg-smart-select":
            target = stage / "models" / "clipseg-rd64-refined"; final = mm.MODELS_ROOT / "clipseg-rd64-refined"
            download_verified(spec["repo_id"], target, hf_rev, PATTERNS[component_id]); replacements.append((target, final))
        elif component_id in {"z-image-turbo", "qwen-image-2512", "qwen-image-edit"}:
            target = stage / "models" / component_id; final = mm.MODELS_ROOT / component_id
            download_verified(spec["repo_id"], target, hf_rev, PATTERNS[component_id])
            if not (target / "model_index.json").exists(): raise RuntimeError("Diffusers model_index.json is missing")
            replacements.append((target, final))
        elif component_id == "hunyuan2mini":
            code = stage / "tools" / "Hunyuan3D-2"; final_code = mm.TOOLS_ROOT / "Hunyuan3D-2"
            _ensure_clone(spec["code_url"], code)
            mm._pip_install(["PyYAML>=6.0", "tqdm>=4.66"])
            target = stage / "models" / "Hunyuan3D-2mini"; final = mm.MODELS_ROOT / "Hunyuan3D-2mini"
            download_verified(spec["repo_id"], target, hf_rev, PATTERNS[component_id]); tool_rev = mm._git_local_revision(code)
            replacements.extend([(code, final_code), (target, final)])
        elif component_id in {"sf3d", "spar3d"}:
            folder = {"sf3d":"stable-fast-3d", "spar3d":"stable-point-aware-3d"}[component_id]
            code = stage / "tools" / folder; final = mm.TOOLS_ROOT / folder
            _ensure_clone(spec["code_url"], code); py = _make_venv(code); _install_requirements(py, code)
            model_dir = code / "miniscuplter-model"
            download_verified(spec["repo_id"], model_dir, hf_rev, PATTERNS[component_id])
            tool_rev = mm._git_local_revision(code); replacements.append((code, final))
        elif component_id == "partpacker":
            code = stage / "tools" / "PartPacker"; final = mm.TOOLS_ROOT / "PartPacker"
            _ensure_clone(spec["code_url"], code); py = _make_venv(code)
            subprocess.run([str(py), "-m", "pip", "install", "torch==2.5.1", "torchvision==0.20.1", "torchaudio", "--index-url", "https://download.pytorch.org/whl/cu124"], check=True)
            _install_requirements(py, code)
            download_verified(spec["repo_id"], code / "miniscuplter-model", hf_rev, PATTERNS[component_id])
            # Provider code expects these names at its root.
            for name in ("vae.pt", "flow.pt"):
                src = code / "miniscuplter-model" / name
                if not src.is_file(): raise RuntimeError(f"PartPacker weight is missing after verification: {name}")
                shutil.copy2(src, code / name)
            tool_rev = mm._git_local_revision(code); replacements.append((code, final))
        elif component_id == "trellis2":
            code = stage / "tools" / "TRELLIS.2"; final = mm.TOOLS_ROOT / "TRELLIS.2"
            _ensure_clone(spec["code_url"], code); tool_rev = mm._git_local_revision(code)
            marker = code / "MINISCULPTER_RUNTIME.txt"
            marker.write_text(
                "Official runtime: Linux, NVIDIA >=24GB VRAM. Configure MINISCULPTER_TRELLIS2_COMMAND.\n"
                "Miniscuplter Launcher does not download TRELLIS.2 weights into the Windows model store.\n",
                encoding="utf-8",
            )
            replacements.append((code, final))
        else:
            raise RuntimeError(f"No v1.0.5 installer for {component_id}")

        if not mm._component_files_valid(component_id, target if component_id not in {"partcrafter", "sf3d", "spar3d", "partpacker", "trellis2"} else final if False else (target if component_id == "partcrafter" else code), stage / "tools"):
            # Most component-specific checks happen in download_verified and provider setup;
            # retain the legacy structural check where its path contract applies.
            if component_id not in {"sf3d", "spar3d", "partpacker", "trellis2"}:
                raise RuntimeError(f"{spec['name']} staged files are incomplete")
        return _finalize(stage, replacements, component_id, final, hf_rev, tool_rev)
    except BaseException:
        # Intentionally preserve the deterministic stage. On the next launcher start/status
        # it is reported as resumable; a retry re-checks upstream revision/manifest and lets
        # huggingface_hub/Xet continue partial files instead of starting over.
        print(f"Partial {component_id} stage preserved for verification/resume: {stage}", file=sys.stderr, flush=True)
        raise


def update_component(component_id: str) -> dict[str, Any]:
    if mm.component_path(component_id) is None:
        raise RuntimeError("The model is not fully installed. Resume/reinstall it instead.")
    return install_component(component_id, True)


def status(check_updates: bool = False) -> dict[str, Any]:
    result = _legacy_status(check_updates=check_updates)
    for item in result.get("components", []):
        cid = item.get("id")
        if cid in AUDITED_PAYLOAD_GB:
            item["estimated_gb"] = AUDITED_PAYLOAD_GB[cid]
        partial = stage_status(mm.STAGING_ROOT, str(cid))
        item.update(partial)
        if partial["resume_available"]:
            action = partial["resume_action"] or "install"
            item["resume_message"] = f"Interrupted {action} detected ({partial['staged_gb']:.2f} GB staged). Resume re-checks the upstream revision and file manifest before continuing."
        else:
            item["resume_message"] = None
    return result
