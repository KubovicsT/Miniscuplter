# Miniscuplter autonomous release requests

This branch is the permanent release-control surface for Miniscuplter. Application development does not occur here.

To request a release, create exactly one new or updated JSON file named `release-requests/v1.x.y.json` containing:

```json
{
  "version": "v1.x.y",
  "source_branch": "v1.x.y",
  "candidate_sha": "FULL_40_CHARACTER_COMMIT_SHA"
}
```

The request does not itself publish anything. `.github/workflows/autonomous_release.yml` treats it only as a request to validate one exact immutable candidate.

The workflow fails closed unless:

- the request filename, version, and source branch agree;
- the source branch still points exactly to the requested SHA;
- the version tag and GitHub Release do not already exist;
- C# / Core tests pass;
- Python compile/runtime dependency checks pass;
- job/core logic and real geometry regressions pass;
- release audit passes;
- a real Godot 4.7.2 Windows export succeeds;
- release files and SHA-256 match;
- the generated installer passes a silent smoke-install;
- the source branch still points to the same candidate immediately before publication.

Only after all gates pass does the workflow create the lightweight `v1.x.y` tag at the exact candidate SHA and publish the verified assets as the immutable GitHub Release.

If validation fails, no new release is published. Fix the failure forward on the source version branch, obtain a new green candidate SHA, then update that version's request file to the new exact SHA. Never move an already-created release tag.

Once a release request has been submitted, the Dev Cycle must freeze application changes on that source version branch until the request succeeds or fails. After successful publication, canonical project docs are reconciled and further application development moves to the next semantic-version branch.

The historical manual tag-triggered release path remains available as a fallback. The autonomous path exists so scheduled development can publish safely without requiring the ChatGPT GitHub connector itself to create tags.
