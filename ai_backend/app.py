from __future__ import annotations

import base64
import os
import shlex
import subprocess
from pathlib import Path
from typing import Optional

import requests
from fastapi import FastAPI, Header, HTTPException
from pydantic import BaseModel, Field

from model_manager import install_component, uninstall_component, status as component_status, component_path
from geometry_api import router as geometry_router
from rig_api import router as rig_router
from semantic_select import semantic_select, SMART_SELECT_COMMAND, release_model as release_smart_select
from model_router import choose_image_provider, choose_3d_provider, routing_status, release_all_models
from detail_pipeline import detail_2d, detail_3d, apply_detail
from storage import DEFAULT_IMAGE_SUFFIXES, DEFAULT_MESH_SUFFIXES, validate_input_path, validate_output_directory, validate_output_path
from job_progress import begin as begin_job, bind as bind_job, report as report_job, complete as complete_job, fail as fail_job, current as current_job, get as get_job, get_events as get_job_events, request_cancel as request_job_cancel

APP_VERSION = "1.0.29"
app = FastAPI(title="Miniscuplter AI Backend", version=APP_VERSION)
app.include_router(geometry_router)
app.include_router(rig_router)


def _safe_output_path(value: str, suffixes=None) -> str:
    try:
        return str(validate_output_path(value, suffixes))
    except ValueError as exc:
        raise HTTPException(400, str(exc)) from exc


def _safe_output_directory(value: str) -> str:
    try:
        return str(validate_output_directory(value))
    except ValueError as exc:
        raise HTTPException(400, str(exc)) from exc


def _safe_input_path(value: str, suffixes) -> str:
    try:
        return str(validate_input_path(value, suffixes))
    except (ValueError, FileNotFoundError) as exc:
        raise HTTPException(400, str(exc)) from exc


def _provider_output(path: str, expected: str, suffixes) -> str:
    try:
        actual = validate_output_path(path, suffixes)
        wanted = validate_output_path(expected, suffixes)
    except ValueError as exc:
        raise RuntimeError(f"AI provider returned an unsafe output path: {exc}") from exc
    if actual != wanted:
        raise RuntimeError(f"AI provider wrote an unexpected output path: {actual}")
    if not actual.is_file() or actual.stat().st_size <= 0:
        raise RuntimeError(f"AI provider returned no usable output file: {actual}")
    return str(actual)


SD_WEBUI_URL = os.getenv("MINISCULPTER_SD_URL", "").rstrip("/")
THREED_COMMAND = os.getenv("MINISCULPTER_3D_COMMAND", "")


class ConceptRequest(BaseModel):
    prompt: str = Field(min_length=1, max_length=8000)
    output_path: str = Field(min_length=1, max_length=4096)
    quality: str = Field(default="standard", max_length=64)
    provider: str = Field(default="auto", max_length=64)


class EditRequest(BaseModel):
    image_path: str = Field(min_length=1, max_length=4096)
    mask_path: Optional[str] = Field(default=None, max_length=4096)
    prompt: str = Field(min_length=1, max_length=8000)
    output_path: str = Field(min_length=1, max_length=4096)
    quality: str = Field(default="standard", max_length=64)
    provider: str = Field(default="auto", max_length=64)
    detail: bool = False


class Generate3DRequest(BaseModel):
    image_path: str = Field(min_length=1, max_length=4096)
    output_path: str = Field(min_length=1, max_length=4096)
    prompt: str = Field(default="", max_length=8000)
    quality: str = Field(default="standard", max_length=64)
    provider: str = Field(default="auto", max_length=64)
    role: str = Field(default="quality", max_length=64)
    generation_job_id: Optional[str] = Field(default=None, max_length=128)
    project_id: Optional[str] = Field(default=None, max_length=128)
    project_revision: Optional[int] = Field(default=None, ge=0)
    input_image_revision_id: Optional[str] = Field(default=None, max_length=128)
    output_object_id: Optional[str] = Field(default=None, max_length=128)


class GeneratePartsRequest(BaseModel):
    image_path: str = Field(min_length=1, max_length=4096)
    output_dir: str = Field(min_length=1, max_length=4096)
    num_parts: int = Field(default=4, ge=1, le=64)
    tag: str = Field(default="part", min_length=1, max_length=128)
    provider: str = Field(default="auto", max_length=64)


class SmartSelectRequest(BaseModel):
    image_path: str = Field(min_length=1, max_length=4096)
    prompt: str = Field(min_length=1, max_length=8000)
    output_path: str = Field(min_length=1, max_length=4096)


class Detail2DRequest(BaseModel):
    image_path: str = Field(min_length=1, max_length=4096)
    mask_path: str = Field(min_length=1, max_length=4096)
    prompt: str = Field(min_length=1, max_length=8000)
    output_path: str = Field(min_length=1, max_length=4096)
    quality: str = Field(default="standard", max_length=64)
    provider: str = Field(default="auto", max_length=64)


class Detail3DRequest(BaseModel):
    image_path: str = Field(min_length=1, max_length=4096)
    mask_path: str = Field(min_length=1, max_length=4096)
    prompt: str = Field(min_length=1, max_length=8000)
    output_path: str = Field(min_length=1, max_length=4096)
    quality: str = Field(default="standard", max_length=64)
    provider: str = Field(default="auto", max_length=64)


class ApplyDetailRequest(BaseModel):
    source_path: str = Field(min_length=1, max_length=4096)
    detail_path: str = Field(min_length=1, max_length=4096)
    output_path: str = Field(min_length=1, max_length=4096)
    pitch: float = Field(default=0.25, ge=0.04, le=5.0)


class ComponentRequest(BaseModel):
    id: str = Field(min_length=1, max_length=128)


def _stage_c_context(req: Generate3DRequest, transport_job_id: str | None) -> dict:
    return {
        "generation_job_id": (req.generation_job_id or transport_job_id or "").strip() or None,
        "project_id": (req.project_id or "").strip() or None,
        "project_revision": req.project_revision,
        "input_image_revision_id": (req.input_image_revision_id or "").strip() or None,
        "output_object_id": (req.output_object_id or "").strip() or None,
    }


@app.get("/health")
def health():
    return {
        "ok": True,
        "version": APP_VERSION,
        "instance_token": os.getenv("MINISCULPTER_BACKEND_INSTANCE", ""),
    }


@app.get("/models")
def models(check_updates: bool = False):
    return component_status(check_updates=check_updates)


@app.get("/routing")
def routing():
    return routing_status()


@app.get("/job-progress/current")
def job_progress_current():
    return current_job()


@app.get("/job-progress/{job_id}")
def job_progress(job_id: str, after: int = 0):
    snapshot = get_job(job_id)
    if snapshot is None:
        raise HTTPException(404, "Unknown AI job")
    snapshot["events_after"] = get_job_events(job_id, max(0, after)) or []
    return snapshot


@app.post("/job-progress/{job_id}/cancel")
def cancel_job(job_id: str):
    snapshot = request_job_cancel(job_id)
    if snapshot is None:
        raise HTTPException(404, "Unknown AI job")
    return snapshot


@app.post("/components/install")
def install(req: ComponentRequest):
    try:
        release_all_models()
        return install_component(req.id)
    except Exception as e:
        raise HTTPException(500, f"Component installation failed: {e}") from e


@app.post("/components/uninstall")
def uninstall(req: ComponentRequest):
    try:
        release_all_models()
        return uninstall_component(req.id)
    except Exception as e:
        raise HTTPException(500, f"Component removal failed: {e}") from e


@app.post("/release-models")
def release_models():
    release_all_models()
    return {"ok": True}


def _write_b64_image(data, out):
    if "," in data:
        data = data.split(",", 1)[1]
    try:
        raw = base64.b64decode(data, validate=True)
        if not raw or len(raw) > 64 * 1024 * 1024:
            raise ValueError("Decoded image payload is empty or above the 64 MiB safety limit.")
        p = validate_output_path(out, DEFAULT_IMAGE_SUFFIXES)
        p.write_bytes(raw)
        return str(p)
    except (ValueError, OSError) as exc:
        raise HTTPException(400, f"Image payload rejected: {exc}") from exc


def _image_generate(provider, req):
    if provider == "sdxl":
        return __import__("sdxl_image", fromlist=["generate_concept"]).generate_concept(req.prompt, req.output_path)
    if provider == "flux":
        return __import__("flux_klein", fromlist=["generate_concept"]).generate_concept(req.prompt, req.output_path)
    if provider == "sd21":
        return __import__("local_image", fromlist=["generate_concept"]).generate_concept(req.prompt, req.output_path, req.quality)
    if provider in {"zimage", "qwen"}:
        cid = {"zimage": "z-image-turbo", "qwen": "qwen-image-2512"}[provider]
        return __import__("modern_image", fromlist=["generate"]).generate(cid, req.prompt, req.output_path)
    raise RuntimeError(f"Unsupported image provider: {provider}")


def _image_edit(provider, req):
    if provider == "sdxl":
        return __import__("sdxl_image", fromlist=["edit_image"]).edit_image(req.image_path, req.mask_path, req.prompt, req.output_path, detail=req.detail)
    if provider == "flux":
        return __import__("flux_klein", fromlist=["edit_image"]).edit_image(req.image_path, req.mask_path, req.prompt, req.output_path, detail=req.detail)
    if provider == "sd21":
        return __import__("local_image", fromlist=["edit_image"]).edit_image(req.image_path, req.mask_path, req.prompt, req.output_path, req.quality)
    if provider == "qwen-edit":
        return __import__("modern_image", fromlist=["edit"]).edit("qwen-image-edit", req.image_path, req.mask_path, req.prompt, req.output_path)
    raise RuntimeError(f"Unsupported image edit provider: {provider}")


@app.post("/generate-concept")
def generate_concept(req: ConceptRequest, x_miniscupter_job_id: Optional[str] = Header(default=None)):
    req.output_path = _safe_output_path(req.output_path, DEFAULT_IMAGE_SUFFIXES)
    begin_job("2d-generate", x_miniscupter_job_id)
    try:
        report_job("resolving_provider", "Choosing the installed local image model for this hardware and route.", 5)
        d = choose_image_provider("generate", req.provider)
        report_job("preparing_runtime", "Freeing previously loaded models before the selected provider is prepared.", 10, d.provider)
        release_all_models()
        report_job("loading_model", f"Preparing {d.provider} model weights and runtime.", 18, d.provider)
        path = _provider_output(_image_generate(d.provider, req), req.output_path, DEFAULT_IMAGE_SUFFIXES)
        report_job("validating_output", "Image inference returned; verifying the saved result.", 97, d.provider)
        complete_job("Concept image saved and verified.", d.provider)
        return {"path": path, "provider": d.provider, "routing_reason": d.reason, "quality": req.quality}
    except Exception as e:
        fail_job(f"2D image provider failed: {e}")
        raise HTTPException(502, f"2D image provider failed: {e}") from e
    finally:
        bind_job(None)


@app.post("/edit-image")
def edit_image(req: EditRequest, x_miniscupter_job_id: Optional[str] = Header(default=None)):
    req.image_path = _safe_input_path(req.image_path, DEFAULT_IMAGE_SUFFIXES)
    if req.mask_path:
        req.mask_path = _safe_input_path(req.mask_path, DEFAULT_IMAGE_SUFFIXES)
    req.output_path = _safe_output_path(req.output_path, DEFAULT_IMAGE_SUFFIXES)
    begin_job("2d-edit", x_miniscupter_job_id)
    try:
        report_job("resolving_provider", "Choosing the local image-edit model.", 5)
        d = choose_image_provider("detail" if req.detail else "edit", req.provider)
        report_job("preparing_runtime", "Freeing previously loaded models and preparing image-edit memory.", 10, d.provider)
        release_all_models()
        report_job("loading_model", f"Preparing {d.provider} for image editing.", 18, d.provider)
        path = _provider_output(_image_edit(d.provider, req), req.output_path, DEFAULT_IMAGE_SUFFIXES)
        report_job("validating_output", "Edit inference returned; verifying the saved image.", 97, d.provider)
        complete_job("Edited image saved and verified.", d.provider)
        return {"path": path, "provider": d.provider, "routing_reason": d.reason, "quality": req.quality}
    except Exception as e:
        fail_job(f"2D image edit provider failed: {e}")
        raise HTTPException(502, f"2D image edit provider failed: {e}") from e
    finally:
        bind_job(None)


def _generate_shape(provider, req, image, output):
    if provider == "hunyuan":
        return __import__("hunyuan_shape", fromlist=["generate_shape"]).generate_shape(image, output, req.prompt, req.quality)
    if provider == "triposr":
        return __import__("triposr_shape", fromlist=["generate_shape"]).generate_shape(image, output, mc_resolution=192 if req.role in {"fast", "rough", "draft"} else 320)
    s = __import__("specialist_3d_v105", fromlist=["x"])
    if provider == "sf3d":
        return s.generate_sf3d(image, output)
    if provider == "spar3d":
        return s.generate_spar3d(image, output, low_vram=int(__import__("model_manager").hardware_info().get("vram_mb", 0)) < 10000)
    if provider == "hunyuan-mini":
        return s.generate_hunyuan_mini(image, output)
    if provider == "trellis2":
        return s.generate_trellis2(image, output)
    raise RuntimeError(f"Provider {provider} is not a single-mesh generator")


@app.post("/generate-3d")
def generate_3d(req: Generate3DRequest, x_miniscupter_job_id: Optional[str] = Header(default=None)):
    req.image_path = _safe_input_path(req.image_path, DEFAULT_IMAGE_SUFFIXES)
    req.output_path = _safe_output_path(req.output_path, (".stl",))
    stage_c_context = _stage_c_context(req, x_miniscupter_job_id)
    transport_job_id = stage_c_context.get("generation_job_id") or x_miniscupter_job_id
    job_id = begin_job("3d-generate", transport_job_id, stage_c_context)
    image = req.image_path
    output = req.output_path
    provider = None
    try:
        report_job("resolving_provider", "Choosing the local 3D reconstruction provider.", 5)
        d = choose_3d_provider(req.role, req.provider)
        provider = d.provider
        report_job("preparing_runtime", "Releasing other models and reserving resources for 3D reconstruction.", 10, provider)
        release_all_models(allow_owner_id=job_id)
        try:
            report_job("loading_model", f"Preparing {provider} model weights and runtime.", 18, provider)
            path = _generate_shape(provider, req, image, output)
            reason = d.reason
            fallback_from = None
        except Exception as primary:
            if (req.provider or "auto").lower() != "auto" or not d.fallback or d.fallback == d.provider:
                raise
            release_all_models(allow_owner_id=job_id)
            provider = d.fallback
            report_job("loading_model", f"Primary provider failed ({primary}). Preparing automatic fallback {provider}.", 20, provider)
            try:
                path = _generate_shape(provider, req, image, output)
                reason = f"{d.reason}; {d.provider} failed ({primary}); automatic fallback to {provider}"
                fallback_from = d.provider
            except Exception as fallback:
                raise RuntimeError(f"Auto 3D route failed. Primary {d.provider}: {primary}. Fallback {provider}: {fallback}") from fallback

        report_job("validating_output", "3D provider returned; verifying the generated mesh file.", 96, provider)
        path = _provider_output(path, output, (".stl",))
        report_job("cleanup", "Releasing 3D model resources before returning control to the editor.", 99, provider)
        release_all_models(allow_owner_id=job_id)
        complete_job("Generated mesh saved and verified.", provider)
        result = {"path": path, "provider": provider, "routing_reason": reason, "role": req.role, "quality": req.quality, "context": stage_c_context}
        if fallback_from:
            result["fallback_from"] = fallback_from
        return result
    except Exception as e:
        fail_job(f"3D provider failed: {e}")
        release_all_models(allow_owner_id=job_id)
        raise HTTPException(502, f"3D provider failed: {e}") from e
    finally:
        bind_job(None)


@app.post("/generate-parts")
def generate_parts(req: GeneratePartsRequest):
    req.image_path = _safe_input_path(req.image_path, DEFAULT_IMAGE_SUFFIXES)
    req.output_dir = _safe_output_directory(req.output_dir)
    try:
        d = choose_3d_provider("structured", req.provider)
        release_all_models()
        if d.provider == "partcrafter":
            r = __import__("partcrafter_shape", fromlist=["generate_parts"]).generate_parts(req.image_path, req.output_dir, req.num_parts, req.tag)
        elif d.provider == "partpacker":
            r = __import__("specialist_3d_v105", fromlist=["generate_partpacker"]).generate_partpacker(req.image_path, req.output_dir, req.tag)
        else:
            raise RuntimeError("Selected provider does not generate structured parts")
        parts = r.get("parts", []) if isinstance(r, dict) else []
        if not isinstance(parts, list) or not parts:
            raise RuntimeError("Structured provider returned no part files")
        safe_dir = Path(req.output_dir).resolve()
        for part in parts:
            checked = Path(_provider_output(str(part), str(part), (".stl",)))
            if safe_dir not in checked.parents:
                raise RuntimeError(f"Structured provider wrote a part outside its output directory: {checked}")
        r["routing_reason"] = d.reason
        return r
    except Exception as e:
        raise HTTPException(502, f"Structured 3D provider failed: {e}") from e


@app.post("/smart-select")
def smart_select(req: SmartSelectRequest):
    req.image_path = _safe_input_path(req.image_path, DEFAULT_IMAGE_SUFFIXES)
    req.output_path = _safe_output_path(req.output_path, DEFAULT_IMAGE_SUFFIXES)
    try:
        path = semantic_select(req.image_path, req.prompt, req.output_path)
        return {"path": _provider_output(path, req.output_path, DEFAULT_IMAGE_SUFFIXES), "provider": "clipseg-smart-select"}
    except Exception as e:
        raise HTTPException(502, f"Smart Select failed: {e}") from e


@app.post("/detail-2d")
def detail_2d_endpoint(req: Detail2DRequest):
    req.image_path = _safe_input_path(req.image_path, DEFAULT_IMAGE_SUFFIXES)
    req.mask_path = _safe_input_path(req.mask_path, DEFAULT_IMAGE_SUFFIXES)
    req.output_path = _safe_output_path(req.output_path, DEFAULT_IMAGE_SUFFIXES)
    try:
        return detail_2d(req.image_path, req.mask_path, req.prompt, req.output_path, req.quality, req.provider)
    except Exception as e:
        raise HTTPException(502, f"2D detail generation failed: {e}") from e


@app.post("/detail-3d")
def detail_3d_endpoint(req: Detail3DRequest):
    req.image_path = _safe_input_path(req.image_path, DEFAULT_IMAGE_SUFFIXES)
    req.mask_path = _safe_input_path(req.mask_path, DEFAULT_IMAGE_SUFFIXES)
    req.output_path = _safe_output_path(req.output_path, (".stl",))
    try:
        return detail_3d(req.image_path, req.mask_path, req.prompt, req.output_path, req.quality, req.provider)
    except Exception as e:
        raise HTTPException(502, f"3D detail generation failed: {e}") from e


@app.post("/apply-detail")
def apply_detail_endpoint(req: ApplyDetailRequest):
    req.source_path = _safe_input_path(req.source_path, DEFAULT_MESH_SUFFIXES)
    req.detail_path = _safe_input_path(req.detail_path, DEFAULT_MESH_SUFFIXES)
    req.output_path = _safe_output_path(req.output_path, (".stl",))
    try:
        return apply_detail(req.source_path, req.detail_path, req.output_path, req.pitch)
    except Exception as e:
        raise HTTPException(502, f"Detail application failed: {e}") from e
