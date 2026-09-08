from __future__ import annotations

import os
from pathlib import Path
from typing import Iterable

DEFAULT_MAX_INPUT_BYTES = 1024 * 1024 * 1024
MAX_PATH_LENGTH = 4096
DEFAULT_IMAGE_SUFFIXES = (".png", ".jpg", ".jpeg", ".webp", ".bmp")
DEFAULT_MESH_SUFFIXES = (".stl", ".obj", ".ply", ".off", ".glb", ".gltf", ".3mf")


def _positive_int(name: str, default: int, maximum: int) -> int:
    raw = os.getenv(name, str(default)).strip()
    try:
        value = int(raw)
    except ValueError:
        value = default
    return max(1, min(value, maximum))


MAX_INPUT_BYTES = _positive_int("MINISCULPTER_MAX_INPUT_BYTES", DEFAULT_MAX_INPUT_BYTES, 4 * 1024 * 1024 * 1024)


def _absolute(value: Path | str) -> Path:
    return Path(os.path.abspath(os.fspath(value)))


def _canonical(value: Path | str) -> Path:
    return Path(os.path.realpath(os.fspath(value)))


def _is_within(root: Path | str, candidate: Path | str) -> bool:
    root_norm = os.path.normcase(os.path.normpath(str(_absolute(root))))
    candidate_norm = os.path.normcase(os.path.normpath(str(_absolute(candidate))))
    try:
        return os.path.commonpath((root_norm, candidate_norm)) == root_norm
    except ValueError:
        return False


def _reject_reparse_points(root: Path, candidate: Path) -> None:
    root_abs = _absolute(root)
    candidate_abs = _absolute(candidate)
    canonical_root = _canonical(root_abs)
    canonical_candidate = _canonical(candidate_abs)
    if not _is_within(canonical_root, canonical_candidate):
        raise ValueError(f"Path escapes the Miniscuplter data root: {candidate_abs}")

    # Walk the caller's original path before returning its canonical spelling. This
    # rejects a symlink/reparse point even when the platform also supplies an
    # alternate path spelling (for example, a Windows 8.3 component).
    current = candidate_abs
    while True:
        if current.is_symlink():
            raise ValueError(f"Refusing to follow a symlink/reparse point in Miniscuplter data: {current}")
        if current.parent == current:
            break
        current = current.parent




def data_root() -> Path:
    configured = os.getenv("MINISCULPTER_DATA", "").strip()
    if configured:
        requested = Path(configured).expanduser()
    else:
        install = Path(os.getenv("MINISCULPTER_ROOT", Path(__file__).resolve().parent.parent)).expanduser()
        requested = install / "AIData"

    if requested.exists() and requested.is_symlink():
        raise ValueError(f"MINISCULPTER_DATA must not be a symlink/reparse point: {requested}")
    root = requested.resolve()
    root.mkdir(parents=True, exist_ok=True)
    _reject_reparse_points(root, root)
    return root


def resolve(relative: str | Path) -> Path:
    if not relative or not str(relative).strip():
        raise ValueError("A Miniscuplter data path is required.")
    if len(str(relative)) > MAX_PATH_LENGTH:
        raise ValueError("The requested Miniscuplter data path is too long.")

    root = data_root()
    candidate = Path(relative).expanduser()
    if not candidate.is_absolute():
        candidate = root / candidate
    candidate = _absolute(candidate)
    _reject_reparse_points(root, candidate)
    return _canonical(candidate)


def _check_suffix(path: Path, allowed_suffixes: Iterable[str] | None) -> None:
    if allowed_suffixes is None:
        return
    allowed = {str(x).lower() for x in allowed_suffixes}
    if path.suffix.lower() not in allowed:
        raise ValueError(f"Unsupported file type for {path.name}; expected one of {', '.join(sorted(allowed))}.")


def validate_output_path(value: str | Path, allowed_suffixes: Iterable[str] | None = None) -> Path:
    candidate = resolve(value)
    if candidate.exists() and candidate.is_dir():
        raise ValueError(f"Output path is a directory, not a file: {candidate}")
    _check_suffix(candidate, allowed_suffixes)
    candidate.parent.mkdir(parents=True, exist_ok=True)
    _reject_reparse_points(data_root(), candidate)
    return candidate


def validate_output_directory(value: str | Path) -> Path:
    if not value or not str(value).strip():
        raise ValueError("An output directory is required.")
    candidate = resolve(value)
    if candidate.exists() and not candidate.is_dir():
        raise ValueError(f"Output directory is not a directory: {candidate}")
    candidate.mkdir(parents=True, exist_ok=True)
    _reject_reparse_points(data_root(), candidate)
    return candidate


def validate_input_path(value: str | Path, allowed_suffixes: Iterable[str] | None = None,
                        max_bytes: int | None = None, allow_external: bool = True) -> Path:
    if not value or not str(value).strip():
        raise ValueError("An input path is required.")
    raw = Path(value).expanduser()
    if len(str(raw)) > MAX_PATH_LENGTH:
        raise ValueError("The requested input path is too long.")

    if raw.is_absolute() and allow_external:
        candidate = Path(os.path.abspath(str(raw)))
        current = candidate
        while current != current.parent:
            if current.is_symlink():
                raise ValueError(f"Refusing to read a symlink/reparse-point input: {current}")
            current = current.parent
    else:
        candidate = resolve(raw)

    if candidate.is_symlink():
        raise ValueError(f"Refusing to read a symlink/reparse-point input: {candidate}")
    if not candidate.exists() or not candidate.is_file():
        raise FileNotFoundError(f"Input file does not exist: {candidate}")
    size = candidate.stat().st_size
    limit = max_bytes if max_bytes is not None else MAX_INPUT_BYTES
    if size <= 0:
        raise ValueError(f"Input file is empty: {candidate}")
    if size > limit:
        raise ValueError(f"Input file is {size:,} bytes, above the safety limit of {limit:,} bytes: {candidate}")
    _check_suffix(candidate, allowed_suffixes)
    return candidate
