# Miniscuplter Handoff

> Immediate execution baton for the next Dev Cycle. Inspect actual Git/release/CI first. `docs/TECHNICAL_ROADMAP.md` owns medium/long-horizon technical direction; this file owns the next implementation step.

Last updated: 2026-09-10

## Current state

- **Repository:** `KubovicsT/Miniscuplter`
- **Latest published stable:** `v1.0.19`
- **Stable application commit:** `52f3b95fb6addc0f9f1e7123b75068da4ef1513c`
- **Current development/release-candidate branch:** `v1.0.20`
- **Validated application/release-candidate code:** `bc3106d606b4450aa5cb9d4395b77a5d6e78f11a`
- **Overall completion:** **58% acceptance-weighted**
- **Coordinator roadmap:** `docs/TECHNICAL_ROADMAP.md`
- **Coordinator history:** `docs/COORDINATOR_LOG.md`

This HANDOFF commit itself advances the live v1.0.20 branch. Resolve the exact branch HEAD before creating the next release request; do not reuse an older SHA.

## Coordinator assessment

The bounded v1.0.20 Stage-C application milestone is complete enough for release-candidate validation. Do **not** reopen or broaden application scope while release orchestration is being repaired.

Already implemented and validated at the application layer:

- accepted immutable 2D baseline;
- revision-bound 3D generation/candidate identity;
- explicit Apply and durable restore;
- Core-authoritative mapped transforms with save/reload and transaction Undo/Redo;
- one bounded sculpt/edit commit path creating immutable child `MeshRevision` state;
- stale edit/cleanup protection and lineage preservation;
- revision-bound cleanup;
- exact durable-state validated STL export.

Broader sculpt migration, Job Broker completion, Rig/Pose modernization, kitbash expansion, provider proliferation, global legacy removal and UI rewrite remain out of v1.0.20 scope.

## Current release-control failure

Autonomous-release run `34506900865` failed in `Parse and validate exactly one release request` before any C#/Python/Godot build/export gate ran.

Important evidence: the step printed that it successfully validated request `v1.0.20` at exact candidate `6ead6ae2f3c2823b7b3b93f2fae805e6808f6f57`, then the step exited with code 1.

Root cause: the validation PowerShell intentionally runs `gh release view` to reject an already-existing release. For a correctly absent release, `gh release view` returns nonzero. The script accepts that condition and continues, but the native failure remains in `$LASTEXITCODE`; no later native success resets it before the step ends. Therefore a successful validation is reported as a failed PowerShell step.

This is an **autonomous release workflow defect**, not evidence against the v1.0.20 candidate.

## Exact next task

1. Reconcile latest stable release, live v1.0.20 HEAD, `release-control` HEAD, and current workflow state.
2. On `release-control`, patch `.github/workflows/autonomous_release.yml` so the expected “GitHub Release does not exist” probe cannot leave the validation step failed. Preserve the existing check that an already-existing version causes a hard failure. Use a clear explicit success path/exit handling; do not weaken any other release safeguard.
3. Do not change v1.0.20 application code merely to repair release orchestration.
4. After the workflow fix is committed, update the existing `release-requests/v1.0.20.json` so `candidate_sha` equals the **exact final live v1.0.20 HEAD**.
5. From that request onward, treat v1.0.20 as frozen. Do not commit application or documentation changes to it while autonomous release validation is pending.
6. Inspect the new autonomous-release run through completion. It must pass request/SHA checks, C#/Core/Python/job/geometry/release-audit validation, real Godot 4.7.2 Windows export, output/hash verification, installer creation and silent installer smoke-install before tag/release publication.
7. If a genuine application/release gate fails, diagnose that exact gate, unfreeze only if a source candidate change is required, fix forward, obtain a new coherent candidate SHA, and update the same request.
8. If publication succeeds, verify GitHub latest release/tag is v1.0.20, reconcile canonical docs, and create/use forward-only `v1.0.21` before any further application change.
9. After publication, prioritize target-machine acceptance for MS-009, MS-013, MS-018 and MS-022. A reproduced real-machine defect immediately outranks planned post-release architecture work.

## Current priority order

1. **Release-control orchestration supporting MS-018** — immediate P0 until v1.0.20 is genuinely published.
2. **MS-018** — target-qualify the complete Stage-C thin slice after publication.
3. **MS-009** — target-PC viewport/grid/model/gizmo verification; reproduced blank viewport becomes immediate P0.
4. **MS-013** — target-PC storage-containment verification.
5. **MS-022** — one intended lightweight/default 3D provider qualification on GTX 1080 / 16 GB.
6. **MS-019 / MS-020 broader architecture work** — resume only after release/acceptance unless a concrete blocking regression requires earlier action.

## Release policy

Never modify a published release/tag. Never bypass exact-SHA, branch-freeze, build, export, hash, installer-smoke or immutability gates simply to make automation succeed. The old explicit-tag release path remains fallback infrastructure, but no manual release action is currently required from the user while the autonomous controller can be repaired through repository writes.

## User input

No product/design decision or manual release action is currently required. Once v1.0.20 is published, user/reference-machine testing becomes the next important dependency.