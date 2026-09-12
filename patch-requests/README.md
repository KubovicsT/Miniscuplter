# Miniscuplter validated patch control

This branch is the cloud-only control surface for safely applying small unified diffs to large text files without replacing the entire target file through the ChatGPT GitHub connector.

A patch request is one JSON file under `patch-requests/`:

```json
{
  "schema": 1,
  "target_branch": "v1.0.29",
  "expected_head_sha": "FULL_40_CHARACTER_BRANCH_HEAD",
  "target_path": "ai_backend/app.py",
  "expected_blob_sha": "FULL_40_CHARACTER_TARGET_BLOB_SHA",
  "commit_message": "concise commit message",
  "patch": "UNIFIED_DIFF"
}
```

The `validated-patch-request` workflow rejects the request unless all safety checks pass:

- target is an untagged semantic-version branch;
- branch HEAD exactly equals `expected_head_sha`;
- target file blob exactly equals `expected_blob_sha`;
- exactly one existing regular text file changes;
- control/workflow paths, deletions, symlinks, renames, mode changes and binary patches are rejected;
- patch size is at most 256 KiB and at most 5000 changed lines;
- `git apply --check` and `git diff --check` pass;
- Python targets pass `py_compile`;
- branch HEAD is rechecked immediately before commit;
- push is normal fast-forward only, never force.

After a successful patch commit, the workflow explicitly dispatches `.github/workflows/build.yml` on the target branch because commits pushed by the built-in GitHub Actions token do not themselves trigger ordinary push workflows.

Patch requests should use deterministic descriptive filenames rather than random IDs. A failed request must be inspected and corrected; never weaken guards or use low-level Git operations to bypass a rejection.
