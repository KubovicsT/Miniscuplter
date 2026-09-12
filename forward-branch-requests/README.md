# Validated forward-branch requests

After a successful immutable release, create the next writable semantic-version branch through the `forward-branch` workflow instead of asking the GitHub connector to create the branch directly.

Create exactly one JSON file under `forward-branch-requests/`:

```json
{
  "source_version": "v1.0.29",
  "target_version": "v1.0.30",
  "source_sha": "FULL_40_CHARACTER_PUBLISHED_RELEASE_SHA"
}
```

The workflow fails closed unless:

- `source_version` is the current latest published GitHub Release;
- `source_sha` exactly matches that release target and its immutable version tag;
- `target_version` is a strictly newer semantic version;
- the target branch does not already exist;
- the target tag does not already exist;
- the source commit can be fetched exactly.

On success it performs one normal non-force push creating `refs/heads/<target_version>` at the exact published source SHA. It never rewrites an existing branch, tag, or release.

After creation, Coordinator/Dev must perform the ordinary next-version semantic bootstrap on the new writable branch before product work proceeds.
