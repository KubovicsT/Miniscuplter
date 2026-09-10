# Miniscuplter Handoff

> Operational baton for the next development run. A new agent must inspect actual Git/release/CI state first; repository state wins over this document if they differ.

Last updated: 2026-09-10

## Current state

- **Repository:** `KubovicsT/Miniscuplter`
- **Latest published stable:** `v1.0.19`
- **Stable application commit:** `52f3b95fb6addc0f9f1e7123b75068da4ef1513c`
- **Current development branch:** `v1.0.20`
- **v1.0.20 base:** exact released v1.0.19 commit above
- **Overall completion estimate:** 56% acceptance-weighted; unchanged pending real-machine Stage-C acceptance.

The documentation commits on v1.0.20 advance HEAD beyond the stable base. Resolve exact branch HEAD from Git at the beginning of the next run.

## Read first

1. `docs/PROJECT_CHARTER.md`
2. `docs/PROJECT_STATUS.md`
3. `docs/ISSUES.md`
4. `docs/DECISIONS.md`
5. this file
6. `docs/REFACTOR_PLAN.md`
7. relevant recent commits/code/tests on `v1.0.20`

## What this run accomplished

### v1.0.19 publication

The existing v1.0.19 batch was reviewed as coherent enough for target-machine acceptance testing and published rather than adding more unverified viewport layers first.

The first temporary publication-helper run (`34469184758`) failed during revalidation because the helper referenced a nonexistent `Core.Tests/Core.Tests.csproj`. This was a helper-workflow mistake, not an application failure. The canonical Stage-B project is `Core.Tests/Miniscuplter.Core.Tests.csproj`.

The helper was corrected and the second publication run (`34469538260`) passed:

- editor C# build;
- launcher C# build;
- updater C# build;
- Python compilation and core tests;
- job-progress tests;
- real geometry regression tests;
- Stage-B Core restore/build/tests;
- release audit;
- verified Godot 4.7.2 .NET/templates download;
- real Windows Godot export;
- release ZIP/hash verification;
- silent installer smoke-install;
- immutable v1.0.19 GitHub Release publication.

GitHub `/releases/latest` resolves to v1.0.19. Published assets include:

- `Miniscuplter-win-x64.zip` — 173,859,707 bytes — SHA-256 `358181ec86c152a083cc6072de9a984d03f701b6589af22bbba6f217ee602ca2`
- `Miniscuplter-Setup-1.0.19.exe` — 120,038,819 bytes — SHA-256 `62e53a0f08061ace6d6b0a5ed4abbf5f9ef9a663a0b266ba88e9a8f2541a264c`

The temporary publication branch was reset to the exact released application commit so helper workflow code is not retained as product code.

### v1.0.20 started

Created `v1.0.20` from the exact v1.0.19 released commit. Updated project status and issue ledger for the release transition.

MS-009 (viewport/grid/model/gizmo) and MS-013 (storage containment) are now **FIXED - NEEDS USER VERIFICATION**, not RESOLVED. Their fixes are distributed but CI cannot prove the historical real-machine symptoms are gone.

## User verification requested, but not blocking autonomous work

On v1.0.19, the user should test:

1. launch into the 3D workspace and confirm grid/starter/generated mesh are visible;
2. select/move/rotate/scale and confirm the gizmo is visible and usable;
3. generate/import a 3D mesh and confirm it is selected/framed/rendered;
4. if blank, copy the v1.0.19 viewport diagnostics/render-probe text;
5. run representative 2D edit, reference download, 3D generation, geometry operation, capture/save/cancel and check that working paths stay under the configured Miniscuplter data root rather than C:\ AppData/TEMP.

A failure in either MS-009 or MS-013 becomes the immediate priority when reported.

## Highest-priority autonomous work on v1.0.20

### Primary: MS-022 — provider qualification/self-test contract

Build the provider-readiness layer needed for Stage C. Do not equate downloaded weights with a usable provider.

Required direction:

- define readiness states at least for downloaded / installed / importable / device-tested / inference-tested;
- add a provider self-test result structure with timestamp, runtime/provider version, device/hardware context, failure detail and optional benchmark data;
- start with the lightweight/default 3D provider path intended for the GTX 1080 target;
- preflight missing Python/native dependencies before committing to a long generation job;
- expose/use readiness in routing so Auto does not select a provider known to be broken;
- preserve explicit user selection semantics: explicit selection should explain failure, not silently substitute another provider;
- add tests for readiness transitions and failure reporting.

### Then: MS-018 + MS-020 — bind the Stage-C AI handoff to stable revisions

Move one vertical slice onto the Stage-B core:

`accepted 2D baseline revision → qualified 3D job → candidate mesh revision → explicit accept/apply`

The job must carry immutable project/object/input revision identity. If the baseline/current object changes while the job runs, the result must become a candidate/conflict and must not silently overwrite newer state. Accept/apply should be a transactional history command.

Keep legacy UI compatibility while migrating this slice; do not create another permanent widget-owned source of truth.

## Architectural cautions discovered/reconfirmed

The v1.0.19 viewport path still coexists with legacy viewport repair/sizing handlers in older `Main.V*.cs` layers. Do not add more overlapping viewport ownership unless the released v1.0.19 test proves it is still necessary. If MS-009 persists, consolidate conflicting ownership rather than layering another recovery surface.

Similarly, Stage-B Core is materially implemented but still not authoritative throughout the editor. Prefer migrating vertical slices rather than a risky all-at-once rewrite.

## Release policy

- Never mutate v1.0.19.
- Application changes now belong on v1.0.20.
- Do not publish v1.0.20 merely because a scheduled run ends; publish when its intended batch is coherent and all gates pass.
- Before publication: C#/Python/Core/geometry/job tests + release audit + real Godot Windows export + artifact/hash verification + installer smoke test.
- After publication, verify `/releases/latest`, update canonical docs, and branch v1.0.21 before further application changes.

## Exact next action

On `v1.0.20`, inspect the existing provider registry/model-manager/routing and Stage-B project/revision APIs, then implement **MS-022 provider readiness/self-tests for the intended lightweight 3D route**, with tests and routing integration. After that, begin the accepted-baseline → 3D candidate stable-revision handoff (MS-018/MS-020) unless a v1.0.19 target-machine regression is reported first.