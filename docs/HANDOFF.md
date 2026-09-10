# Miniscuplter Handoff

> Immediate execution baton for the next Dev Cycle. Inspect actual Git/release/CI first. `docs/TECHNICAL_ROADMAP.md` owns medium/long-horizon technical direction; this file owns the next implementation step.

Last updated: 2026-09-10

## Current state

- **Repository:** `KubovicsT/Miniscuplter`
- **Latest published stable:** `v1.0.19`
- **Stable application commit:** `52f3b95fb6addc0f9f1e7123b75068da4ef1513c`
- **Current development branch:** `v1.0.20`
- **v1.0.20 base:** exact released v1.0.19 commit above
- **Latest validated application code candidate:** `bc3106d606b4450aa5cb9d4395b77a5d6e78f11a`
- **Latest pure Stage-C editing implementation commit:** `e71d55ff90d8eec3b03a8f3073e9ffefdc0d1228`
- **Overall completion:** 58% acceptance-weighted
- **Coordinator roadmap:** `docs/TECHNICAL_ROADMAP.md`
- **Coordinator history:** `docs/COORDINATOR_LOG.md`

Resolve the exact live v1.0.20 HEAD before acting; documentation/process reconciliation intentionally advances HEAD beyond the validated application-code commit.

## Coordinator-aligned code state

The bounded v1.0.20 Stage-C target is implemented and prior branch validation is green. Accepted baseline, revision-bound generation and candidate Apply, Core-authoritative mapped transforms, one bounded immutable sculpt commit, save/reload, revision-bound cleanup and exact validated STL export all participate in the replacement Core state model. Do not broaden v1.0.20 into unrelated application work.

Key implementation commits include `e67a2d3...`, `01db848...`, `68725d0...`, `b556602...`, `df8730b...`, `eb2aefc...`, `0ecdbc9...`, and `e71d55f...`. Release-version preparation is represented by `bc3106d606b4450aa5cb9d4395b77a5d6e78f11a` and later documentation-only commits.

## Validation already established

For application candidate `bc3106d606b4450aa5cb9d4395b77a5d6e78f11a`:

- `core-foundation` run `34503132325`: SUCCESS.
- broader build run `34503132420`: SUCCESS.
- Python compilation/dependency resolution: PASS.
- Core logic/execution/job regressions: PASS.
- real geometry regressions: PASS.
- v1.0.20 release audit: PASS.
- editor/launcher/updater/Core C# restore/build: PASS.
- portable package/layout/SHA verification: PASS.
- installer-definition compilation: PASS.

The real Godot 4.7.2 Windows export, final release output/hash checks and silent installer smoke-install are owned by the autonomous release workflow and must pass on the exact final candidate before publication.

## Autonomous release incident and fix

The repository now has a permanent `release-control` branch with `.github/workflows/autonomous_release.yml` and `release-requests/README.md`. This supersedes the stale earlier handoff instruction that required the user to create a tag manually.

A request file `release-requests/v1.0.20.json` correctly targeted v1.0.20 SHA `dc72d4a528b07d5855066d5fcaf2d4ef2c4d1be0`. Autonomous-release run `34506052015` failed in `Parse and validate exactly one release request`, before application build/export gates ran.

Root cause: the parser used `git diff-tree` against only `GITHUB_SHA`; the release-control change arrived as a merge-style commit and the request path was not reliably discovered. This is a release-orchestration defect, not evidence against the application candidate.

Permanent release-control fix: commit `06763c99a0206bef0bf48a66a980c6d6f1095305` changes request discovery to compare the full push range (`github.event.before` → `github.sha`) with a root-push fallback, deduplicates paths, and still requires exactly one semver request. All existing immutability and release gates remain intact.

## Exact next task

1. Resolve current v1.0.20 HEAD after this handoff/status reconciliation.
2. Update the existing `release-control:release-requests/v1.0.20.json` so `candidate_sha` equals that exact 40-character HEAD. This is the release freeze point; do not commit anything else to v1.0.20 while the request runs.
3. Inspect the new `autonomous-release` run. It must independently validate the exact candidate through C#/Core tests, Python/runtime checks, geometry regressions, release audit, verified Godot 4.7.2 Windows export, release output/hash checks, installer creation and silent installer smoke-install.
4. If the workflow succeeds, verify immutable tag/release v1.0.20 exists and GitHub latest release is v1.0.20. Then reconcile canonical docs to published state and create/use forward-only `v1.0.21` before any application change.
5. If the workflow fails, diagnose the actual failing gate. Only unfreeze v1.0.20 if a source-candidate fix is required; fix forward and update the same request to the new coherent SHA. Never weaken gates or rewrite an existing published tag/release.
6. After publication, target-machine acceptance on GTX 1080 / 16 GB remains required for MS-018, MS-009, MS-013 and one intended lightweight/default 3D provider under MS-022.

## Current issue priority

1. **MS-018:** publish and target-qualify the coherent Stage-C thin slice.
2. **MS-009:** target-PC viewport/grid/model/gizmo verification on released v1.0.20; any reproduced blank viewport immediately becomes P0.
3. **MS-013:** target-PC storage containment verification.
4. **MS-022:** one intended lightweight/default 3D route target qualification.
5. **MS-019:** remaining legacy authority outside Stage-C; Coordinator should sequence post-release migration.
6. **MS-020:** broader durable queue/resource ownership/crash recovery after release/acceptance unless a concrete lifecycle blocker appears.

## User input

No product/design or manual release action is currently required. Once v1.0.20 is published, the user is needed only for real target-machine acceptance evidence.
