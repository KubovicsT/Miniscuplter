import json, hashlib, subprocess
from pathlib import Path
p=json.loads(Path("localdev-preparation/LD-20260919-1003.json").read_text())
src=Path(p["edit_windows"][0]["path"]); original=src.read_bytes()
s=original.decode("utf-8-sig").replace("\r\n","\n")
assert hashlib.sha256(s.encode()).hexdigest()==p["edit_windows"][0]["sha256"]
test=Path(p["fixtures"][0]["path"]); assert not test.exists()
def run():
 r=subprocess.run(p["regression"]["argv"],text=True,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,timeout=300)
 print(r.stdout,flush=True); return r
assert run().returncode==0, "Clean baseline failed"
try:
 test.write_text(p["fixtures"][0]["content"],encoding="utf-8")
 r=run(); assert r.returncode!=0 and p["regression"]["expected_failure"] in r.stdout
 needle="        await _saveGate.WaitAsync(cancellationToken);"
 assert s.count(needle)==1
 guard='        if (string.IsNullOrWhiteSpace(projectPath) || !string.Equals(Path.GetExtension(projectPath), ProjectExtension, StringComparison.OrdinalIgnoreCase))\n            throw new ArgumentException("Invalid save destination.", nameof(projectPath));\n'
 src.write_text(s.replace(needle,guard+needle),encoding="utf-8")
 r=run(); assert r.returncode==0 and p["regression"]["success_marker"] in r.stdout
finally:
 src.write_bytes(original);test.unlink(missing_ok=True)
print("PREPARED_TASK_VERIFIED")
