from __future__ import annotations

import json
import os
import socket
import subprocess
import sys
import tempfile
import time
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BACKEND = ROOT / "ai_backend"
SERVER = BACKEND / "serve.py"
EXPECTED_VERSION = "1.0.29"


def free_port() -> int:
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as sock:
        sock.bind(("127.0.0.1", 0))
        return int(sock.getsockname()[1])


def main() -> None:
    if not SERVER.is_file():
        raise AssertionError("canonical backend serve.py is missing")

    port = free_port()
    token = "ci-instance-token"
    with tempfile.TemporaryDirectory(prefix="miniscuplter-backend-") as temp:
        env = os.environ.copy()
        env["MINISCULPTER_ROOT"] = temp
        env["MINISCULPTER_DATA"] = str(Path(temp) / "AIData")
        env["PYTHONUNBUFFERED"] = "1"
        command = [
            sys.executable,
            str(SERVER),
            "--host",
            "127.0.0.1",
            "--port",
            str(port),
            "--instance-token",
            token,
        ]
        process = subprocess.Popen(
            command,
            cwd=BACKEND,
            env=env,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True,
        )
        try:
            deadline = time.monotonic() + 20.0
            last_error = "server did not answer"
            while time.monotonic() < deadline:
                if process.poll() is not None:
                    out, err = process.communicate(timeout=2)
                    raise AssertionError(
                        f"backend exited before health check: {process.returncode}\nstdout={out[-3000:]}\nstderr={err[-3000:]}"
                    )
                try:
                    with urllib.request.urlopen(f"http://127.0.0.1:{port}/health", timeout=1.0) as response:
                        payload = json.loads(response.read().decode("utf-8"))
                    if payload.get("ok") is not True:
                        last_error = f"unexpected health payload: {payload}"
                    elif payload.get("version") != EXPECTED_VERSION:
                        last_error = f"version mismatch: {payload}"
                    elif payload.get("instance_token") != token:
                        last_error = f"instance identity mismatch: {payload}"
                    else:
                        print("backend lifecycle regression test passed")
                        return
                except Exception as exc:
                    last_error = repr(exc)
                time.sleep(0.2)
            raise AssertionError(f"backend never became healthy: {last_error}")
        finally:
            if process.poll() is None:
                process.terminate()
                try:
                    process.wait(timeout=5)
                except subprocess.TimeoutExpired:
                    process.kill()
                    process.wait(timeout=5)


if __name__ == "__main__":
    main()
