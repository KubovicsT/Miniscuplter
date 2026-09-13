# Miniscuplter validated patch control

This branch is the cloud-only control surface for safe one-file text mutations when ordinary connector writes are unsafe or blocked.

Schema 1 updates one existing regular text file and requires: `target_branch`, exact `expected_head_sha`, `target_path`, exact `expected_blob_sha`, `commit_message`, and unified-diff `patch`.

Schema 2 creates one new UTF-8 text file and requires:

```json
{
  "schema": 2,
  "operation": "create_text_file",
  "target_branch": "v1.0.33",
  "expected_head_sha": "FULL_40_CHARACTER_BRANCH_HEAD",
  "target_path": "Core/NewFile.cs",
  "commit_message": "concise commit message",
  "content": "complete UTF-8 text content"
}
```

Safety rules for both modes:
- target branch must be an untagged semantic-version branch;
- branch HEAD must exactly match `expected_head_sha` and is rechecked before commit;
- control/workflow/request paths are forbidden targets;
- only one regular text file may change;
- symlinks, binaries, deletions and unsafe path traversal are rejected;
- payload is limited to 256 KiB and 5000 changed lines;
- `git diff --check` must pass; Python targets also pass `py_compile`;
- schema 1 additionally requires exact target blob identity;
- schema 2 requires the target path to be absent at the exact expected HEAD;
- push is normal fast-forward only, never force.

After success, the workflow dispatches `build.yml` for the target branch. Use deterministic descriptive request filenames. Inspect failed requests instead of weakening guards.
