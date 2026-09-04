from __future__ import annotations

import json
import os
import shutil
import subprocess
import sys
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parent
DATA_ROOT = Path(os.getenv("MINISCULPTER_DATA", ROOT / "data")).resolve()
MODELS_ROOT = DATA_ROOT / "models"
TOOLS_ROOT = DATA_ROOT / "tools"
STATE_FILE = DATA_ROOT / "components.json"

COMPONENTS: dict[str, dict[str, Any]] = {
    "sd21": {
        "name": "Stable Diffusion 2.1 Base", "kind": "image", "source": "huggingface",
        "repo_id": "stabilityai/stable-diffusion-2-1-base",
        "description": "Legacy low-memory 2D fallback.", "estimated_gb": 5.5,
    },
    "sdxl-base": {
        "name": "Stable Diffusion XL Base 1.0", "kind": "image", "source": "huggingface",
        "repo_id": "stabilityai/stable-diffusion-xl-base-1.0",
        "description": "Primary modern 2D generator/editor for 8GB-class hardware using CPU offload.", "estimated_gb": 7.0,
    },
    "flux2-klein-4b": {
        "name": "FLUX.2 Klein 4B", "kind": "image", "source": "huggingface",
        "repo_id": "black-forest-labs/FLUX.2-klein-4B",
        "description": "Optional high-quality generation/editing specialist; heavier than the GTX1080 VRAM budget and therefore offloaded.", "estimated_gb": 13.0,
    },
    "hunyuan21-shape": {
        "name": "Hunyuan3D 2.1 Shape", "kind": "3d", "source": "hunyuan",
        "repo_id": "tencent/Hunyuan3D-2.1", "code_url": "https://github.com/Tencent-Hunyuan/Hunyuan3D-2.1.git",
        "description": "Quality whole-object and selected-detail image-to-shape model.", "estimated_gb": 10.0,
    },
    "triposr": {
        "name": "TripoSR", "kind": "3d", "source": "huggingface",
        "repo_id": "stabilityai/TripoSR", "code_url": "https://github.com/VAST-AI-Research/TripoSR.git",
        "description": "Fast rough single-image 3D reconstruction route; about 6GB VRAM at official defaults.", "estimated_gb": 2.5,
    },
    "partcrafter": {
        "name": "PartCrafter", "kind": "3d-parts", "source": "github+huggingface",
        "repo_id": "wgsxm/PartCrafter", "code_url": "https://github.com/wgsxm/PartCrafter.git",
        "description": "Structured part-aware 3D generation specialist. Official requirement starts at 8GB CUDA VRAM.", "estimated_gb": 12.0,
    },
    "clipseg-smart-select": {
        "name": "CLIPSeg Smart Select", "kind": "segmentation", "source": "huggingface",
        "repo_id": "CIDAS/clipseg-rd64-refined",
        "description": "Local text-guided semantic segmentation used by Smart Select on multi-view renders.", "estimated_gb": 0.7,
    },
}


def _run(command: list[str], cwd: Path | None = None, timeout: int | None = None) -> subprocess.CompletedProcess[str]:
    return subprocess.run(command, cwd=cwd, check=True, text=True, capture_output=True, timeout=timeout)


def _disk_info() -> dict[str, float]:
    DATA_ROOT.mkdir(parents=True, exist_ok=True)
    usage = shutil.disk_usage(DATA_ROOT); gib = 1024 ** 3
    return {"free_gb": round(usage.free / gib, 2), "total_gb": round(usage.total / gib, 2)}


def hardware_info() -> dict[str, Any]:
    info: dict[str, Any] = {"platform": sys.platform, "python": sys.version.split()[0], "gpu": None, "vram_mb": 0,
        "cuda_available": False, "recommended_profile": "cpu", **_disk_info()}
    nvidia_smi = shutil.which("nvidia-smi")
    if nvidia_smi:
        try:
            p = _run([nvidia_smi, "--query-gpu=name,memory.total", "--format=csv,noheader,nounits"], timeout=10)
            lines = p.stdout.strip().splitlines()
            if lines:
                name, mem = [x.strip() for x in lines[0].rsplit(",", 1)]
                info["gpu"] = name; info["vram_mb"] = int(mem); info["cuda_available"] = True
                if info["vram_mb"] > 12288: info["recommended_profile"] = "ultra"
                elif info["vram_mb"] > 8192: info["recommended_profile"] = "high"
                elif info["vram_mb"] > 4096: info["recommended_profile"] = "medium"
                else: info["recommended_profile"] = "low"
        except Exception:
            pass
    return info


def load_state() -> dict[str, Any]:
    if not STATE_FILE.exists(): return {"installed": {}, "settings": {}}
    try:
        data = json.loads(STATE_FILE.read_text(encoding="utf-8"))
        return data if isinstance(data, dict) else {"installed": {}, "settings": {}}
    except Exception:
        return {"installed": {}, "settings": {}}


def save_state(state: dict[str, Any]) -> None:
    DATA_ROOT.mkdir(parents=True, exist_ok=True)
    temp = STATE_FILE.with_suffix(".json.tmp")
    temp.write_text(json.dumps(state, indent=2), encoding="utf-8")
    temp.replace(STATE_FILE)


def _hf_revision(repo_id: str) -> str:
    from huggingface_hub import HfApi
    info = HfApi().model_info(repo_id, revision="main")
    if not info.sha: raise RuntimeError(f"Hugging Face returned no revision for {repo_id}")
    return str(info.sha)


def _git_local_revision(path: Path) -> str | None:
    if not path.exists() or not (path / ".git").exists(): return None
    try: return _run(["git", "rev-parse", "HEAD"], cwd=path, timeout=10).stdout.strip() or None
    except Exception: return None


def _git_remote_revision(url: str) -> str:
    git = shutil.which("git")
    if not git: raise RuntimeError("Git is required to check the companion inference-code revision")
    out = _run([git, "ls-remote", url, "HEAD"], timeout=30).stdout.strip()
    if not out: raise RuntimeError(f"Git returned no HEAD revision for {url}")
    return out.split()[0]


def _tool_dir(component_id: str) -> Path | None:
    return {
        "hunyuan21-shape": TOOLS_ROOT / "Hunyuan3D-2.1",
        "triposr": TOOLS_ROOT / "TripoSR",
        "partcrafter": TOOLS_ROOT / "PartCrafter",
    }.get(component_id)


def _combined_revision(hf: str | None, tool: str | None) -> str | None:
    if hf and tool: return f"hf:{hf[:12]} git:{tool[:12]}"
    if hf: return hf
    if tool: return tool
    return None


def _remote_revisions(component_id: str, spec: dict[str, Any]) -> tuple[str | None, str | None]:
    hf = _hf_revision(spec["repo_id"]) if spec.get("repo_id") else None
    tool = _git_remote_revision(spec["code_url"]) if spec.get("code_url") else None
    return hf, tool


def _directory_has_files(path: Path) -> bool:
    try: return path.is_dir() and any(p.is_file() for p in path.rglob("*"))
    except OSError: return False


def _component_files_valid(component_id: str, path: Path) -> bool:
    """Reject stale state entries and visibly incomplete managed installations."""
    if not path.exists(): return False
    if component_id in {"sd21", "sdxl-base", "flux2-klein-4b"}:
        return (path / "model_index.json").is_file() and _directory_has_files(path)
    if component_id == "hunyuan21-shape":
        return _directory_has_files(path / "hunyuan3d-dit-v2-1") and _directory_has_files(path / "hunyuan3d-vae-v2-1") and _directory_has_files(TOOLS_ROOT / "Hunyuan3D-2.1")
    if component_id == "triposr":
        return (path / "model.ckpt").is_file() and (path / "config.yaml").is_file() and _directory_has_files(TOOLS_ROOT / "TripoSR")
    if component_id == "partcrafter":
        return (_directory_has_files(path / "pretrained_weights" / "PartCrafter") and
                _directory_has_files(path / "pretrained_weights" / "RMBG-1.4") and
                (path / ".git").is_dir())
    if component_id == "clipseg-smart-select":
        return (path / "config.json").is_file() and (path / "model.safetensors").is_file()
    return _directory_has_files(path)


def component_path(component_id: str) -> Path | None:
    state = load_state(); entry = state.get("installed", {}).get(component_id)
    if not isinstance(entry, dict) or not entry.get("installed") or not entry.get("path"): return None
    p = Path(entry["path"]).resolve()
    return p if _component_files_valid(component_id, p) else None


def status(check_updates: bool = False) -> dict[str, Any]:
    state = load_state(); result = []
    for component_id, spec in COMPONENTS.items():
        installed_state = state.get("installed", {}).get(component_id, {})
        valid_path = component_path(component_id)
        installed = valid_path is not None
        entry = dict(spec); entry["id"] = component_id; entry["installed"] = installed
        entry["path"] = str(valid_path) if valid_path else installed_state.get("path")
        installed_hf = installed_state.get("hf_revision") if isinstance(installed_state, dict) else None
        installed_tool = installed_state.get("tool_revision") if isinstance(installed_state, dict) else None
        entry["installed_revision"] = _combined_revision(installed_hf, installed_tool)
        entry["remote_revision"] = None; entry["update_available"] = False; entry["update_error"] = None
        if isinstance(installed_state, dict) and installed_state.get("installed") and not installed:
            entry["update_error"] = "Installation state exists but required model files are missing or incomplete. Reinstall this component."
        if installed and check_updates:
            try:
                remote_hf, remote_tool = _remote_revisions(component_id, spec)
                entry["remote_revision"] = _combined_revision(remote_hf, remote_tool)
                entry["update_available"] = (
                    (remote_hf is not None and installed_hf != remote_hf) or
                    (remote_tool is not None and installed_tool != remote_tool)
                )
            except Exception as exc:
                entry["update_error"] = str(exc)
        result.append(entry)
    return {"hardware": hardware_info(), "components": result, "data_root": str(DATA_ROOT), "disk": _disk_info()}


def _clone_or_update(url: str, target: Path, update: bool) -> None:
    git = shutil.which("git")
    if not git: raise RuntimeError("Git is required to install this component")
    if not target.exists():
        _run([git, "clone", "--depth", "1", url, str(target)])
        return
    if not update: return
    if not (target / ".git").exists(): raise RuntimeError(f"Managed tool directory is not a Git checkout: {target}")
    _run([git, "fetch", "--depth", "1", "origin", "HEAD"], cwd=target)
    _run([git, "reset", "--hard", "FETCH_HEAD"], cwd=target)


def _pip_install(packages: list[str], extra_args: list[str] | None = None) -> None:
    if not packages: return
    command = [sys.executable, "-m", "pip", "install", *packages]
    if extra_args: command.extend(extra_args)
    try:
        _run(command)
        _run([sys.executable, "-m", "pip", "check"])
    except subprocess.CalledProcessError as exc:
        detail = (exc.stderr or exc.stdout or str(exc))[-4000:]
        raise RuntimeError("Python dependency installation failed without changing component state: " + detail) from exc


def _verify_tool_import(code_dir: Path, statement: str, label: str, extra_path: Path | None = None) -> None:
    prefixes = [str(code_dir)]
    if extra_path is not None: prefixes.insert(0, str(extra_path))
    probe = "import sys; " + "".join(f"sys.path.insert(0, {p!r}); " for p in prefixes) + statement
    try:
        _run([sys.executable, "-c", probe], cwd=code_dir, timeout=120)
    except Exception as exc:
        detail = getattr(exc, "stderr", None) or getattr(exc, "stdout", None) or str(exc)
        raise RuntimeError(f"{label} dependencies installed but its inference code cannot be imported: {str(detail)[-3000:]}") from exc


def _install_hunyuan_dependencies(code_dir: Path) -> None:
    # Miniscuplter uses the Hunyuan shape pipeline only. Do not install Tencent's full
    # requirements file because it pins older Diffusers/Transformers versions and includes
    # paint/training/render packages unrelated to shape inference. The core runtime already
    # supplies torch, diffusers, transformers, accelerate, trimesh, numpy, Pillow and rembg.
    _pip_install([
        "PyYAML>=6.0",
        "tqdm>=4.66",
    ])
    _verify_tool_import(
        code_dir,
        "from hy3dshape.pipelines import Hunyuan3DDiTFlowMatchingPipeline",
        "Hunyuan3D 2.1 Shape",
        code_dir / "hy3dshape",
    )


def _install_triposr_dependencies(code_dir: Path) -> None:
    # TripoSR's upstream requirements pin old shared packages (for example Transformers 4.35)
    # that conflict with the modern SDXL/FLUX backend. Install only its inference-only extras;
    # the compatible newer shared dependencies are supplied by Miniscuplter's core runtime.
    _pip_install([
        "git+https://github.com/tatsy/torchmcubes.git",
        "imageio[ffmpeg]",
        "xatlas==0.0.9",
        "moderngl==5.10.0",
    ])
    _verify_tool_import(code_dir, "from tsr.system import TSR; from tsr.utils import remove_background, resize_foreground", "TripoSR")


def _install_partcrafter_dependencies(code_dir: Path) -> None:
    # The official setup script also installs training/monitoring/VLM packages and Linux EGL
    # system libraries. Miniscuplter uses only local inference without rendering or VLM calls,
    # so install the actual inference dependencies while preserving the shared AI runtime.
    _pip_install([
        "numpy==1.26.4",
        "scikit-learn",
        "opencv-python",
        "peft",
        "jaxtyping",
        "typeguard",
        "matplotlib",
        "imageio-ffmpeg",
        "pyrender",
        "colormaps",
    ])
    _pip_install(["torch-cluster"], ["-f", "https://data.pyg.org/whl/torch-2.5.1+cu124.html"])
    _verify_tool_import(code_dir, "from src.pipelines.pipeline_partcrafter import PartCrafterPipeline; from src.models.briarmbg import BriaRMBG", "PartCrafter")


def _download_hf(snapshot_download, repo_id: str, target: Path, revision: str, allow_patterns: list[str] | None = None) -> None:
    kwargs: dict[str, Any] = {"repo_id": repo_id, "revision": revision, "local_dir": target}
    if allow_patterns is not None: kwargs["allow_patterns"] = allow_patterns
    snapshot_download(**kwargs)


def install_component(component_id: str, update: bool = False) -> dict[str, Any]:
    if component_id not in COMPONENTS: raise ValueError(f"Unknown AI component: {component_id}")
    spec = COMPONENTS[component_id]; MODELS_ROOT.mkdir(parents=True, exist_ok=True); TOOLS_ROOT.mkdir(parents=True, exist_ok=True)
    disk = _disk_info(); required = (1.0 if update else float(spec.get("estimated_gb", 0)) * 1.25 + 1.0)
    if disk["free_gb"] < required:
        raise RuntimeError(f"Not enough free disk space for {spec['name']}. About {required:.1f} GB free is recommended; only {disk['free_gb']:.1f} GB is available.")
    try: from huggingface_hub import snapshot_download
    except Exception as exc: raise RuntimeError("huggingface_hub is required. Use Miniscuplter Launcher -> Repair AI Runtime.") from exc

    hf_revision = _hf_revision(spec["repo_id"]) if spec.get("repo_id") else None
    if component_id == "sd21":
        target = MODELS_ROOT / "stable-diffusion-2-1-base"
        _download_hf(snapshot_download, spec["repo_id"], target, hf_revision)
    elif component_id == "sdxl-base":
        target = MODELS_ROOT / "stable-diffusion-xl-base-1.0"
        _download_hf(snapshot_download, spec["repo_id"], target, hf_revision,
            ["model_index.json", "scheduler/**", "text_encoder/**", "text_encoder_2/**", "tokenizer/**", "tokenizer_2/**", "unet/**", "vae/**", "*.safetensors", "LICENSE*", "README.md"])
    elif component_id == "flux2-klein-4b":
        target = MODELS_ROOT / "FLUX.2-klein-4B"
        _download_hf(snapshot_download, spec["repo_id"], target, hf_revision)
    elif component_id == "hunyuan21-shape":
        code_dir = TOOLS_ROOT / "Hunyuan3D-2.1"; _clone_or_update(spec["code_url"], code_dir, update); _install_hunyuan_dependencies(code_dir)
        target = MODELS_ROOT / "Hunyuan3D-2.1"
        _download_hf(snapshot_download, spec["repo_id"], target, hf_revision,
            ["hunyuan3d-dit-v2-1/**", "hunyuan3d-vae-v2-1/**", "README.md", "LICENSE", "Notice.txt"])
    elif component_id == "triposr":
        code_dir = TOOLS_ROOT / "TripoSR"; _clone_or_update(spec["code_url"], code_dir, update); _install_triposr_dependencies(code_dir)
        target = MODELS_ROOT / "TripoSR"
        _download_hf(snapshot_download, spec["repo_id"], target, hf_revision, ["config.yaml", "model.ckpt", "README.md", "LICENSE*"])
    elif component_id == "partcrafter":
        code_dir = TOOLS_ROOT / "PartCrafter"; _clone_or_update(spec["code_url"], code_dir, update); _install_partcrafter_dependencies(code_dir)
        target = code_dir
        _download_hf(snapshot_download, spec["repo_id"], code_dir / "pretrained_weights" / "PartCrafter", hf_revision)
        rmbg_revision = _hf_revision("briaai/RMBG-1.4")
        _download_hf(snapshot_download, "briaai/RMBG-1.4", code_dir / "pretrained_weights" / "RMBG-1.4", rmbg_revision)
    elif component_id == "clipseg-smart-select":
        target = MODELS_ROOT / "clipseg-rd64-refined"
        _download_hf(snapshot_download, spec["repo_id"], target, hf_revision,
            ["config.json", "preprocessor_config.json", "tokenizer_config.json", "special_tokens_map.json", "vocab.json", "merges.txt", "model.safetensors", "README.md"])
    else:
        raise RuntimeError(f"No installer implemented for {component_id}")

    if not _component_files_valid(component_id, target):
        raise RuntimeError(f"{spec['name']} download completed but required runtime files are missing. The component was not marked installed.")

    tool_path = _tool_dir(component_id); tool_revision = _git_local_revision(tool_path) if tool_path else None
    state = load_state(); state.setdefault("installed", {})[component_id] = {
        "installed": True, "path": str(target), "hf_revision": hf_revision, "tool_revision": tool_revision
    }
    state.setdefault("settings", {})["profile"] = hardware_info()["recommended_profile"]; save_state(state)
    return {"id": component_id, "installed": True, "path": str(target), "installed_revision": _combined_revision(hf_revision, tool_revision), "hardware": hardware_info()}


def update_component(component_id: str) -> dict[str, Any]:
    if component_id not in COMPONENTS: raise ValueError(f"Unknown AI component: {component_id}")
    if component_path(component_id) is None: raise RuntimeError("The model is not fully installed. Reinstall it instead of updating it.")
    return install_component(component_id, update=True)


def _managed_path(path: Path) -> Path:
    resolved = path.resolve()
    if resolved == DATA_ROOT or DATA_ROOT not in resolved.parents:
        raise RuntimeError(f"Refusing to remove path outside the Miniscuplter-managed AI data directory: {resolved}")
    return resolved


def uninstall_component(component_id: str) -> dict[str, Any]:
    if component_id not in COMPONENTS: raise ValueError(f"Unknown AI component: {component_id}")
    state = load_state(); entry = state.get("installed", {}).get(component_id); spec = COMPONENTS[component_id]
    paths: list[Path] = []
    if isinstance(entry, dict) and entry.get("path"): paths.append(Path(entry["path"]))
    tool = _tool_dir(component_id)
    if tool is not None and all(tool.resolve() != p.resolve() for p in paths): paths.append(tool)

    for raw in paths:
        resolved = _managed_path(raw)
        if resolved.exists():
            shutil.rmtree(resolved)
            if resolved.exists(): raise RuntimeError(f"Could not completely remove managed component directory: {resolved}")

    state.setdefault("installed", {}).pop(component_id, None)
    save_state(state)
    return {"id": component_id, "installed": False, "name": spec.get("name", component_id)}
