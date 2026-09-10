# Miniscuplter Project Status

> Fast-moving project dashboard. Inspect actual Git/release/CI state before trusting this file.

Last reconciled: 2026-09-10

## Current release / development state

- **Latest published stable release:** `v1.0.19`
- **Stable release application commit:** `52f3b95fb6addc0f9f1e7123b75068da4ef1513c`
- **Current development branch:** `v1.0.20`
- **Validated v1.0.20 application/release-candidate code:** `bc3106d606b4450aa5cb9d4395b77a5d6e78f11a`
- **Latest live branch HEAD before this Coordinator documentation sequence:** `6ead6ae2f3c2823b7b3b93f2fae805e6808f6f57`
- **Overall completion:** **58% acceptance-weighted**

v1.0.19 remains immutable/published. v1.0.20 remains the forward development/release-candidate branch. Coordinator documentation commits after the application candidate advance branch HEAD without changing product capability; resolve exact live HEAD before any release request.

## Current phase

The Coordinator-defined bounded v1.0.20 Stage-C application scope is implemented:

`accepted 2D baseline → revision-bound 3D candidate → explicit Apply → Core-authoritative transform → one immutable sculpt/edit commit → save/reload → revision-bound cleanup → exact validated STL export`

The project is no longer blocked on additional v1.0.20 application architecture. It is currently blocked on the autonomous release-control workflow reaching the real Windows build/export/smoke gates.

## Validation already established

For application candidate `bc3106d606b4450aa5cb9d4395b77a5d6e78f11a`:

- Core/Stage-C validation: **PASS**
- broader Windows build: **PASS**
- Python compilation/dependency resolution: **PASS**
- Core/execution/job regressions: **PASS**
- real geometry regressions: **PASS**
- v1.0.20 release audit: **PASS**
- editor/launcher/updater/Core builds: **PASS**
- portable package/layout/SHA verification: **PASS**
- installer-definition compilation: **PASS**

Real Godot Windows export, final output/hash verification and silent installer smoke-install remain pending because the autonomous release controller has failed before reaching those gates.

## Autonomous release-control state

The permanent `release-control` branch and `.github/workflows/autonomous_release.yml` remain the preferred autonomous release path. The historical explicit-tag workflow remains a safety fallback.

Bootstrap history:

1. Initial request-discovery failure: the workflow inspected only the triggering commit and could miss the release request across a merge-style push. This was corrected to inspect the full push range.
2. Latest retry `34506900865`: the request parser successfully validated v1.0.20 at exact candidate `6ead6ae2f3c2823b7b3b93f2fae805e6808f6f57`, but the validation step still concluded failure before any application build/export work.

Current root cause: the expected failing `gh release view` probe used to prove that v1.0.20 does **not** already exist leaves native `$LASTEXITCODE = 1`; although the script continues and reports successful request validation, the PowerShell step terminates with exit code 1.

This is a release-orchestration defect, not an application-candidate regression.

## Current priorities

1. **MS-018 / release publication:** repair the autonomous release-control success/exit handling and publish the already-bounded v1.0.20 candidate through the full gates.
2. **MS-009:** after publication, verify viewport/grid/model/gizmo on the target PC; any reproduced blank viewport becomes immediate P0.
3. **MS-013:** verify storage containment on the target PC.
4. **MS-022:** qualify one intended lightweight/default 3D provider on GTX 1080 / 16 GB with practical timing/RAM/VRAM evidence.
5. **MS-019:** broader legacy state migration waits behind release/acceptance; its bounded v1.0.20 transform/sculpt requirement is met.
6. **MS-020:** broader durable Job Broker work waits behind Stage-C release/acceptance unless a concrete lifecycle defect blocks testing.

## Immediate engineering priority

Do not add unrelated v1.0.20 application work.

The next Dev Cycle should:

1. repair only the `release-control` workflow's expected absent-release exit handling while preserving all immutability/exact-SHA/build/export/hash/smoke safeguards;
2. submit the exact final v1.0.20 branch HEAD through the existing request and freeze the source branch;
3. follow the workflow to success or a genuine release gate failure;
4. after successful publication, verify latest release v1.0.20 and move all further application changes to forward-only v1.0.21;
5. then obtain reference-machine acceptance evidence.

## User input currently required

No product/design decision or manual release action is currently required. Once v1.0.20 is published, real GTX 1080 / 16 GB target-machine testing becomes the important user input.