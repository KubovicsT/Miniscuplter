"""Verify the prepared test; the control fix exists only in this CI worktree."""
import hashlib
import json
from pathlib import Path
import subprocess

packet = json.loads(Path("localdev-preparation/LD-20260919-1001.json").read_text(encoding="utf-8"))
source_path = Path(packet["edit_windows"][0]["path"])
original = source_path.read_bytes()
source = original.decode("utf-8-sig").replace("\r\n", "\n")
assert hashlib.sha256(source.encode()).hexdigest() == packet["edit_windows"][0]["sha256"]
fixture = packet["fixtures"][0]
test_path = Path(fixture["path"])
assert not test_path.exists()
command = packet["regression"]["argv"]

def run(stage):
    result = subprocess.run(command, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, timeout=300)
    print(stage, "exit", result.returncode, flush=True)
    print(result.stdout, flush=True)
    return result

baseline = run("CLEAN_BASELINE")
assert baseline.returncode == 0, "Existing suite must pass before fixture"
try:
    test_path.write_text(fixture["content"], encoding="utf-8")
    failed = run("PREPARED_REGRESSION")
    assert failed.returncode != 0 and packet["regression"]["expected_failure"] in failed.stdout
    assert packet["regression"]["success_marker"] not in failed.stdout
    needle = "        string manifest = Path.GetFullPath(projectPath);"
    assert source.count(needle) == 1
    control = '        if (string.IsNullOrWhiteSpace(projectPath))\n            throw new ArgumentException("Project path must not be blank.", nameof(projectPath));\n'
    source_path.write_text(source.replace(needle, control + needle), encoding="utf-8")
    passed = run("TEMPORARY_POSITIVE_CONTROL")
    assert passed.returncode == 0 and packet["regression"]["success_marker"] in passed.stdout
    assert packet["regression"]["expected_failure"] not in passed.stdout
finally:
    source_path.write_bytes(original)
    test_path.unlink(missing_ok=True)
print("PREPARED_TASK_VERIFIED")
