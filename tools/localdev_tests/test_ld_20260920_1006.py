from __future__ import annotations

import os
import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "ai_backend"))

from storage import validate_output_path


def fail(message: str) -> None:
    raise AssertionError("LD_FAIL_20260920_1006: " + message)


def main() -> None:
    previous = os.environ.get("MINISCULPTER_DATA")
    try:
        with tempfile.TemporaryDirectory() as temp:
            data_root = Path(temp) / "AIData"
            os.environ["MINISCULPTER_DATA"] = str(data_root)
            try:
                validate_output_path("Workspace/rejected.txt", (".stl",))
                fail("unsupported output suffix was accepted")
            except ValueError as exc:
                if "Unsupported file type" not in str(exc):
                    fail("wrong invalid-suffix error")
            if data_root.exists():
                fail("invalid suffix created the configured data root")

            valid = validate_output_path("Workspace/result.stl", (".stl",))
            if valid != (data_root / "Workspace" / "result.stl").resolve():
                fail("valid output did not resolve inside the configured root")
            if not valid.parent.is_dir():
                fail("valid output did not create its parent directory")
            print("LD_PASS_20260920_1006")
    finally:
        if previous is None:
            os.environ.pop("MINISCULPTER_DATA", None)
        else:
            os.environ["MINISCULPTER_DATA"] = previous


if __name__ == "__main__":
    main()
