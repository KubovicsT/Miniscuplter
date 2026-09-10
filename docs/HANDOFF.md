# Miniscuplter Handoff

> Immediate execution baton for the next Dev Cycle. Inspect actual Git/release/CI first. `docs/TECHNICAL_ROADMAP.md` owns medium/long-horizon technical direction; this file owns the next implementation step.

Last updated: 2026-09-10

## Current state

- **Repository:** `KubovicsT/Miniscuplter`
- **Latest published stable:** `v1.0.21`
- **Stable release target:** `0c8877b7ac5a04f9f3362851a9a9725f8348cdf8`
- **Current development branch:** `v1.0.22`
- **Last implementation/version candidate before documentation commits:** `36093b66f1a9e746e2a46f5c0d55afd35aa35b84`
- **Overall completion:** **57% acceptance-weighted**
- **Coordinator objective:** remove the confirmed Stage-C persistence blocker and narrow viewport/layout acceptance regressions before resuming broader work.

Always resolve the exact live v1.0.22 HEAD after this documentation commit. v1.0.21 is published and immutable.

## Work completed this Dev Cycle

### MS-023 — one authoritative Stage-C generation owner

Code inspection proved the production race without another long GPU reproduction: v1.0.17 attaches `V1017Generate3DAsync`, while v1.0.20 previously removed only the v1.0.9 handler before attaching `V1020Generate3DAsync`. Both could therefore receive one button press and race on `_v1093DBusy`; the legacy path directly imported a transient mesh and could cause the durable Stage-C path to exit.

`Scripts/Main.V1022Acceptance.cs` is now installed last and deterministically removes v1.0.9, v1.0.17 and any duplicate Stage-C subscription, then attaches exactly one `V1020Generate3DAsync`. Existing Stage-C semantics are preserved: inference result → identity-bound Ready/Conflict candidate → explicit Apply → durable mapped object. No parallel generation architecture was introduced.

### MS-024 / MS-009 — resize/presentation ownership

The acceptance guard reasserts outer `FullRect`/`ExpandFill` layout on window viewport size changes without assigning `SubViewport.Size`, so v1.0.21's native Stretch ownership remains intact. Native studio lighting/presentation is reasserted on viewport-host resize/recovery, and the historical opaque filled grid ground is hidden while bars/axes remain.

### MS-025 — clean empty project

Historical starter spheres are removed after final composition, after New Scene, and after the deferred legacy viewport-repair action. This avoids using an oversized primitive as an implicit scale reference while leaving the established first-real-object selection/framing behavior intact.

### Regression/release protection

`tools/core_logic_tests.py` now asserts final installer order, removal of legacy Generate-3D owners, exactly-one Stage-C rebound, candidate/apply/restore seams, starter cleanup, root client-fill, non-occluding grid and absence of new manual `SubViewport.Size` assignment. `tools/release_audit.py` now expects 1.0.22 and independently guards the v1.0.22 ownership/layout invariants.

One failed intermediate validation is intentionally preserved: build `34518253820` at `42dd143...` failed only because the new static test matched `SubViewport.Size` in a comment. Commit `0e41b78...` narrowed the check to assignment syntax; subsequent implementation CI passed the regression.

Version identity is synchronized to 1.0.22 across Godot/editor, backend, launcher, updater, Windows export and installer surfaces in `36093b66...`.

## Validation state

For implementation commit `0e41b78d6a109bc09515a3d8877f385f91714527`:
- Stage-B/Core: PASS
- C# builds: PASS
- Python compilation/dependency resolution: PASS
- core/execution regressions: PASS
- geometry regressions: PASS
- release audit: PASS
- portable package/layout/hash: PASS

For release-identity commit `36093b66f1a9e746e2a46f5c0d55afd35aa35b84`, exact-head Core, Python/execution/geometry/release-audit and C# validation are green; reconcile the latest packaging status before declaring release readiness. Documentation commits after that candidate advance the branch HEAD and must be included in the final exact-SHA release candidate.

## Exact next task

1. Reconcile latest GitHub release, exact live v1.0.22 HEAD and all CI runs.
2. Update/reconcile `docs/ISSUES.md` so MS-023, MS-024 and MS-025 are `FIXED - NEEDS USER VERIFICATION`, preserving the failed static-test attempt and explaining the implementation evidence.
3. Ensure final v1.0.22 exact-head automated validation is green. Do not release if C#, Core, Python, geometry, release-audit or packaging gates are red.
4. If the bounded v1.0.22 scope remains coherent, submit exactly one `release-requests/v1.0.22.json` on `release-control` with `candidate_sha` equal to the final live v1.0.22 HEAD. Treat that as a release freeze.
5. Follow autonomous release through exact-SHA revalidation, real Godot 4.7.2 Windows export, output/hash verification, installer build and silent smoke-install. Diagnose any genuine failing gate; do not weaken it.
6. On successful publication, verify GitHub latest release/tag targets the exact candidate, then create forward-only `v1.0.23` before any further application change and reconcile PROJECT_STATUS/HANDOFF there.
7. Next user/reference-machine test should exercise: accepted 2D → Hunyuan/qualified 3D → candidate visible → Apply → save/close/reopen → same 3D object/revision → Move/Rotate/Scale → cleanup → exact STL export. Also check right-panel resize, whole-window resize, grid occlusion and absence of starter sphere.
8. Any reproduced persistence/viewport regression outranks planned MS-026 telemetry and broader MS-019/MS-020 work.

## User input

No product/design decision is required. After publication, reference-machine verification is the next important dependency; do not require another long Hunyuan run before the fixed build exists.
