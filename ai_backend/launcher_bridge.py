from __future__ import annotations

import argparse
import json
import sys

from model_manager import install_component, uninstall_component, update_component, status


def main() -> int:
    parser = argparse.ArgumentParser(description="Miniscuplter launcher/model-manager bridge")
    sub = parser.add_subparsers(dest="command", required=True)

    p_status = sub.add_parser("status")
    group = p_status.add_mutually_exclusive_group()
    group.add_argument("--updates", action="store_true")
    group.add_argument("--no-updates", action="store_true")

    for name in ("install", "remove", "update"):
        p = sub.add_parser(name)
        p.add_argument("id")

    args = parser.parse_args()
    try:
        if args.command == "status": result = status(check_updates=bool(args.updates and not args.no_updates))
        elif args.command == "install": result = install_component(args.id)
        elif args.command == "remove": result = uninstall_component(args.id)
        elif args.command == "update": result = update_component(args.id)
        else: raise RuntimeError("Unknown launcher command")
        print(json.dumps(result, ensure_ascii=False))
        return 0
    except Exception as exc:
        print(str(exc), file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
