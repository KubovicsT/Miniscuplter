from __future__ import annotations

import argparse
import os

import uvicorn


def main() -> None:
    parser = argparse.ArgumentParser(description="Start the Miniscuplter local AI backend.")
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=7868)
    parser.add_argument("--instance-token", default="")
    args = parser.parse_args()

    if args.host not in {"127.0.0.1", "localhost"}:
        raise SystemExit("Miniscuplter backend must bind to loopback only.")
    if not (1 <= args.port <= 65535):
        raise SystemExit("Invalid backend port.")

    os.environ["MINISCULPTER_BACKEND_INSTANCE"] = args.instance_token
    uvicorn.run("app:app", host="127.0.0.1", port=args.port, log_level="info")


if __name__ == "__main__":
    main()
