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
