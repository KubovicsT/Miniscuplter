# Validated canonical version bootstrap

`VERSION` is the single canonical Miniscuplter application version. Agents must not manually edit individual derived version surfaces.

To bootstrap a writable semantic-version branch, create exactly one JSON request under `version-requests/` on the `release-control` branch:

```json
{
  "version": "1.0.30",
  "target_branch": "v1.0.30",
  "expected_head_sha": "FULL_40_CHARACTER_TARGET_BRANCH_HEAD"
}
```

The `validated-version-bootstrap` workflow fails closed unless:

- `target_branch` exactly equals `v<version>`;
- the target branch still equals `expected_head_sha`;
- no target tag or GitHub Release exists;
- the canonical sync engine changes only the fixed version-surface allowlist;
- Python version-bearing files compile;
- `git diff --check` passes;
- the branch HEAD is rechecked immediately before commit;
- the final push is normal fast-forward only.

On success the workflow writes root `VERSION` and derives the following surfaces from it:

- launcher project version;
- updater project version;
- Godot editor assembly version;
- installer version;
- Windows export file/product versions;
- backend API version;
- editor displayed version;
- release-audit expected version;
- backend-lifecycle expected version.

The workflow then explicitly dispatches `build.yml` on the resulting exact target-branch HEAD.

After this contract is established, semantic-version bootstrap must use this workflow instead of manually patching those surfaces. The version request is the control action; `VERSION` is canonical project state; all other listed version strings are generated/derived compatibility surfaces.
