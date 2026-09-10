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

APP_VERSION = "1.0.20"
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


class Generate3DRequest(BaseModel):
    image_path: str = Field(min_length=1, max_length=4096)
    prompt: str = Field(default="", max_length=8000)
    output_path: str = Field(min_length=1, max_length=4096)
    quality: str = Field(default="standard", max_length=64)
    provider: str = Field(default="auto", max_length=64)
    role: str = Field(default="quality", max_length=64)
    generation_job_id: Optional[str] = Field(default=None, min_length=1, max_length=96)
    project_id: Optional[str] = Field(default=None, min_length=1, max_length=96)
    project_revision: Optional[int] = Field(default=None, ge=0)
    input_image_revision_id: Optional[str] = Field(default=None, min_length=1, max_length=96)
    output_object_id: Optional[str] = Field(default=None, min_length=1, max_length=96)


class PartsRequest(BaseModel):
    image_path: str = Field(min_length=1, max_length=4096)
    output_dir: str = Field(min_length=1, max_length=4096)
    num_parts: int = Field(default=6, ge=1, le=64)
    tag: str = Field(default="miniscuplter", max_length=64)
    provider: str = Field(default="auto", max_length=64)


class Detail2DRequest(BaseModel):
    image_path: str = Field(min_length=1, max_length=4096)
    mask_path: str = Field(min_length=1, max_length=4096)
    prompt: str = Field(min_length=1, max_length=8000)
    output_path: str = Field(min_length=1, max_length=4096)
    image_provider: str = Field(default="auto", max_length=64)


class Detail3DRequest(BaseModel):
    source_mesh: str = Field(min_length=1, max_length=4096)
    image_path: str = Field(min_length=1, max_length=4096)
    mask_path: str = Field(min_length=1, max_length=4096)
    prompt: str = Field(min_length=1, max_length=8000)
    bounds_min: list[float] = Field(min_length=3, max_length=3)
    bounds_max: list[float] = Field(min_length=3, max_length=3)
    output_patch: str = Field(min_length=1, max_length=4096)
    output_image: str = Field(min_length=1, max_length=4096)
    output_crop: str = Field(min_length=1, max_length=4096)
    image_provider: str = Field(default="auto", max_length=64)
    three_d_provider: str = Field(default="auto", max_length=64)


class DetailApplyRequest(BaseModel):
    source_mesh: str = Field(min_length=1, max_length=4096)
    patch_mesh: str = Field(min_length=1, max_length=4096)
    output_path: str = Field(min_length=1, max_length=4096)
    voxel_size: Optional[float] = Field(default=None, gt=0.0)


class QualityConfigRequest(BaseModel):
    image_size: Optional[int] = Field(default=None, ge=256, le=1536)
    image_steps: Optional[int] = Field(default=None, ge=4, le=80)
    image_guidance: Optional[float] = Field(default=None, ge=0.0, le=20.0)
    image_edit_strength: Optional[float] = Field(default=None, ge=0.05, le=1.0)
    max_input_px: Optional[int] = Field(default=None, ge=256, le=2048)
    shape_steps: Optional[int] = Field(default=None, ge=4, le=100)
    remesh_voxel_mm: Optional[float] = Field(default=None, ge=0.03, le=3.0)
    repair_voxel_mm: Optional[float] = Field(default=None, ge=0.03, le=3.0)
    max_voxel_cells: Optional[int] = Field(default=None, ge=500_000, le=50_000_000)
    thickness_samples: Optional[int] = Field(default=None, ge=500, le=100_000)
    smart_select_views: Optional[int] = Field(default=None, ge=3, le=16)
    smart_select_render_size: Optional[int] = Field(default=None, ge=128, le=1024)


def _job_context(req: Generate3DRequest) -> Optional[dict]:
    values = {
        "generation_job_id": req.generation_job_id,
        "project_id": req.project_id,
        "project_revision": req.project_revision,
        "input_image_revision_id": req.input_image_revision_id,
        "output_object_id": req.output_object_id,
    }
    populated = {key: value for key, value in values.items() if value is not None}
    if not populated:
        return None
    if len(populated) != len(values):
        missing = ", ".join(sorted(key for key, value in values.items() if value is None))
        raise HTTPException(400, f"Stage-C generation context must be all-or-nothing; missing: {missing}.")
    return values


@app.get("/health")
def health():
    return {
        "status": "ok",
        "version": APP_VERSION,
        "sd_webui": SD_WEBUI_URL,
        "has_3d_command": bool(THREED_COMMAND),
        "components": component_status(),
        "routing": routing_status(),
        "job": current_job(),
    }


@app.get("/routing")
def routing():
    return routing_status()


@app.get("/components")
def components():
    return component_status()


@app.get("/job-progress/{job_id}")
def job_progress(job_id: str):
    return get_job(job_id)


@app.get("/job-progress/{job_id}/events")
def job_progress_events(job_id: str, since_sequence: int = 0):
    return get_job_events(job_id, since_sequence)


@app.post("/job-cancel/{job_id}")
def job_cancel(job_id: str):
    return request_job_cancel(job_id)


@app.post("/generate-concept")
def generate_concept(req: ConceptRequest, x_miniscupter_job_id: Optional[str] = Header(default=None)):
    out = _safe_output_path(req.output_path, DEFAULT_IMAGE_SUFFIXES)
    job_id = begin_job("image-generate", req.provider, "queued", "Concept generation queued", x_miniscupter_job_id)
    try:
        provider = choose_image_provider("generate", req.provider)
        bind_job(job_id, provider.id, "loading", f"Loading {provider.id}", 0.03)
        result = provider.generate(req.prompt, out, req.quality)
        path = _provider_output(result, out, DEFAULT_IMAGE_SUFFIXES)
        release_all_models()
        complete_job(job_id, "done", "Concept image ready")
        return {"path": path, "provider": provider.id, "quality": req.quality}
    except Exception as exc:
        fail_job(job_id, str(exc))
        release_all_models()
        raise HTTPException(502, str(exc))


@app.post("/edit-image")
def edit_image(req: EditRequest, x_miniscupter_job_id: Optional[str] = Header(default=None)):
    image_path = _safe_input_path(req.image_path, DEFAULT_IMAGE_SUFFIXES)
    mask_path = _safe_input_path(req.mask_path, DEFAULT_IMAGE_SUFFIXES) if req.mask_path else None
    out = _safe_output_path(req.output_path, DEFAULT_IMAGE_SUFFIXES)
    job_id = begin_job("image-edit", req.provider, "queued", "Image edit queued", x_miniscupter_job_id)
    try:
        provider = choose_image_provider("edit", req.provider)
        bind_job(job_id, provider.id, "loading", f"Loading {provider.id}", 0.03)
        result = provider.edit(image_path, mask_path, req.prompt, out, req.quality)
        path = _provider_output(result, out, DEFAULT_IMAGE_SUFFIXES)
        release_all_models()
        complete_job(job_id, "done", "Image edit ready")
        return {"path": path, "provider": provider.id, "quality": req.quality}
    except Exception as exc:
        fail_job(job_id, str(exc))
        release_all_models()
        raise HTTPException(502, str(exc))


@app.post("/generate-3d")
def generate_3d(req: Generate3DRequest, x_miniscupter_job_id: Optional[str] = Header(default=None)):
    image_path = _safe_input_path(req.image_path, DEFAULT_IMAGE_SUFFIXES)
    out = _safe_output_path(req.output_path, DEFAULT_MESH_SUFFIXES)
    stage_c_context = _job_context(req)
    requested_job_id = x_miniscupter_job_id
    if stage_c_context is not None:
        if requested_job_id and requested_job_id != req.generation_job_id:
            raise HTTPException(400, "Stage-C generation job identity does not match the transport job header.")
        requested_job_id = req.generation_job_id

    job_id = begin_job("image-to-3d", req.provider, "queued", "3D reconstruction queued", requested_job_id, context=stage_c_context)
    d = None
    actual_provider = ""
    primary_provider = ""
    start = __import__("time").monotonic()
    try:
        d = choose_3d_provider(req.role, req.provider)
        primary_provider = d.provider
        bind_job(job_id, d.provider, "loading", f"Loading {d.provider}", 0.04)
        release_all_models()
        report_job(job_id, "running", f"Generating mesh with {d.provider}", 0.10, d.provider)
        try:
            _generate_shape(d.provider, image_path, req.prompt, out, req.quality)
            actual_provider = d.provider
        except Exception as primary_exc:
            if req.provider == "auto" and d.fallback and d.fallback != d.provider:
                report_job(job_id, "fallback", f"{d.provider} failed; trying {d.fallback}: {primary_exc}", 0.12, d.fallback)
                release_all_models()
                _generate_shape(d.fallback, image_path, req.prompt, out, req.quality)
                actual_provider = d.fallback
            else:
                raise
        path = _provider_output(out, out, DEFAULT_MESH_SUFFIXES)
        elapsed = __import__("time").monotonic() - start
        from provider_readiness import record_inference_success
        record_inference_success(actual_provider, elapsed_seconds=elapsed, benchmark={"source": "verified-generate-3d"})
        release_all_models()
        complete_job(job_id, "done", "3D mesh ready")
        response = {"path": path, "provider": actual_provider, "routing_reason": d.reason, "role": req.role, "quality": req.quality}
        if stage_c_context is not None:
            response["context"] = stage_c_context
        if primary_provider and actual_provider and primary_provider != actual_provider:
            response["fallback_from"] = primary_provider
        return response
    except Exception as exc:
        fail_job(job_id, str(exc))
        release_all_models()
        raise HTTPException(502, str(exc))


@app.post("/generate-parts")
def generate_parts(req: PartsRequest, x_miniscupter_job_id: Optional[str] = Header(default=None)):
    from model_router import choose_3d_provider
    image_path = _safe_input_path(req.image_path, DEFAULT_IMAGE_SUFFIXES)
    output_dir = _safe_output_directory(req.output_dir)
    d = choose_3d_provider("structured-parts", req.provider)
    job_id = begin_job("structured-parts", d.provider, "queued", "Part generation queued", x_miniscupter_job_id)
    bind_job(job_id, d.provider, "running", f"Generating editable parts with {d.provider}", 0.05)
    try:
        from specialist_3d_v105 import generate_partpacker, generate_partcrafter
        if d.provider == "partpacker":
            manifest = generate_partpacker(image_path, output_dir, max(1, req.num_parts), req.tag)
        else:
            manifest = generate_partcrafter(image_path, output_dir, max(1, req.num_parts), req.tag)
        complete_job(job_id, "done", "Editable parts ready")
        release_all_models()
        return {"path": manifest, "provider": d.provider, "routing_reason": d.reason, "role": "structured-parts"}
    except Exception as exc:
        fail_job(job_id, str(exc))
        release_all_models()
        raise HTTPException(502, str(exc))


@app.post("/detail-2d")
def run_detail_2d(req: Detail2DRequest, x_miniscupter_job_id: Optional[str] = Header(default=None)):
    output_path = _safe_output_path(req.output_path, DEFAULT_IMAGE_SUFFIXES)
    job_id = begin_job("detail-2d", req.image_provider, "queued", "Detail image queued", x_miniscupter_job_id)
    bind_job(job_id, req.image_provider, "running", "Editing detail crop", 0.10)
    try:
        result = detail_2d(req.image_path, req.mask_path, req.prompt, output_path, req.image_provider)
        path = _provider_output(result, output_path, DEFAULT_IMAGE_SUFFIXES)
        complete_job(job_id, "done", "Detail image ready")
        return {"path": path}
    except Exception as exc:
        fail_job(job_id, str(exc))
        raise HTTPException(502, str(exc))


@app.post("/detail-3d")
def run_detail_3d(req: Detail3DRequest, x_miniscupter_job_id: Optional[str] = Header(default=None)):
    output_patch = _safe_output_path(req.output_patch, DEFAULT_MESH_SUFFIXES)
    output_image = _safe_output_path(req.output_image, DEFAULT_IMAGE_SUFFIXES)
    output_crop = _safe_output_path(req.output_crop, DEFAULT_IMAGE_SUFFIXES)
    job_id = begin_job("detail-3d", req.three_d_provider, "queued", "Detail reconstruction queued", x_miniscupter_job_id)
    bind_job(job_id, req.three_d_provider, "running", "Reconstructing selected detail", 0.10)
    try:
        result = detail_3d(req.source_mesh, req.image_path, req.mask_path, req.prompt, req.bounds_min, req.bounds_max,
                           output_patch, output_image, output_crop, req.image_provider, req.three_d_provider)
        complete_job(job_id, "done", "Detail patch ready")
        return result
    except Exception as exc:
        fail_job(job_id, str(exc))
        raise HTTPException(502, str(exc))


@app.post("/detail-apply")
def run_detail_apply(req: DetailApplyRequest, x_miniscupter_job_id: Optional[str] = Header(default=None)):
    source_mesh = _safe_input_path(req.source_mesh, DEFAULT_MESH_SUFFIXES)
    patch_mesh = _safe_input_path(req.patch_mesh, DEFAULT_MESH_SUFFIXES)
    output_path = _safe_output_path(req.output_path, DEFAULT_MESH_SUFFIXES)
    job_id = begin_job("detail-apply", "geometry", "queued", "Detail apply queued", x_miniscupter_job_id)
    bind_job(job_id, "geometry", "running", "Applying detail patch", 0.10)
    try:
        result = apply_detail(source_mesh, patch_mesh, output_path, req.voxel_size)
        path = _provider_output(result, output_path, DEFAULT_MESH_SUFFIXES)
        complete_job(job_id, "done", "Detail applied")
        return {"path": path}
    except Exception as exc:
        fail_job(job_id, str(exc))
        raise HTTPException(502, str(exc))


@app.post("/geometry/quality-config")
def apply_quality_config(req: QualityConfigRequest):
    from quality_config import update_quality_config
    return update_quality_config(req.model_dump(exclude_none=True))


def _generate_shape(provider: str, image_path: str, prompt: str, out: str, quality: str):
    if provider == "hunyuan-mini":
        from specialist_3d_v105 import generate_hunyuan_mini
        return generate_hunyuan_mini(image_path, out)
    if provider == "sf3d":
        from specialist_3d_v105 import generate_sf3d
        return generate_sf3d(image_path, out)
    if provider == "spar3d":
        from specialist_3d_v105 import generate_spar3d
        return generate_spar3d(image_path, out)
    if provider == "trellis2":
        from specialist_3d_v105 import generate_trellis2
        return generate_trellis2(image_path, out)
    if provider == "triposr":
        from triposr_shape import generate_shape
        return generate_shape(image_path, out, quality)
    if provider == "hunyuan":
        from hunyuan_shape import generate_hunyuan_shape
        return generate_hunyuan_shape(image_path, out, quality)
    if provider == "command":
        return _generate_shape_command(image_path, prompt, out)
    raise RuntimeError(f"Unknown 3D provider {provider}")


def _generate_shape_command(image_path: str, prompt: str, out: str):
    if not THREED_COMMAND:
        raise RuntimeError("MINISCULPTER_3D_COMMAND is not configured")
    args = shlex.split(THREED_COMMAND, posix=False) + ["--image", image_path, "--prompt", prompt, "--output", out]
    subprocess.run(args, check=True)
    return out


def _data_uri(path: str) -> str:
    suffix = Path(path).suffix.lower()
    mime = {".jpg":"image/jpeg", ".jpeg":"image/jpeg", ".webp":"image/webp"}.get(suffix, "image/png")
    return f"data:{mime};base64,{base64.b64encode(Path(path).read_bytes()).decode('ascii')}"
