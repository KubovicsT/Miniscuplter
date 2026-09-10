# Miniscuplter Project Status

> Fast-moving project dashboard. Inspect actual Git/release/CI state before trusting this file.

Last reconciled: 2026-09-10

## Current release / development state

- **Latest published stable release:** `v1.0.21`
- **Stable release target commit:** `0c8877b7ac5a04f9f3362851a9a9725f8348cdf8`
- **Current development branch:** `v1.0.22`
- **Current validated implementation candidate before documentation commits:** `36093b66f1a9e746e2a46f5c0d55afd35aa35b84`
- **Overall completion:** **57% acceptance-weighted**

v1.0.21 is published and immutable. v1.0.22 remains the only forward application branch for the current acceptance fixes.

## Current phase

The Coordinator-defined critical path remains Stage-C target acceptance. The v1.0.21 reference-machine test proved the native viewport is readable initially, but also exposed three concrete blockers now addressed in v1.0.22 code:

1. **MS-023 — Stage-C generation ownership/persistence.** v1.0.17 and v1.0.20 could both own the same Generate-3D button. v1.0.22 installs a final acceptance guard that removes the v1.0.9, v1.0.17 and duplicate v1.0.20 subscriptions and then attaches exactly one `V1020Generate3DAsync` owner. Successful generation therefore enters the existing revision-bound candidate path instead of racing a transient legacy import.
2. **MS-024 — whole-window resize seams.** v1.0.22 reasserts FullRect/ExpandFill ownership for the outer UI on native viewport size changes without writing `SubViewport.Size`, preserving the v1.0.21 native render-target ownership model.
3. **MS-025 / MS-009 presentation residuals.** v1.0.22 removes the legacy starter sphere from launch/New/recovery-composed scenes, keeps the empty viewport usable, hides the opaque historical grid floor while preserving grid bars/axes, and reasserts the native studio presentation after host resize/recovery.

The durable Core save/reload path was re-audited during implementation: applied objects already restore from their `ActiveMeshRevisionId`; no speculative Core rewrite was needed.

## Validation

Implementation commit `0e41b78d6a109bc09515a3d8877f385f91714527` passed Stage-B/Core, C#, Python compilation/dependency, execution, geometry and release-audit validation after correcting one test-only false positive. The failed intermediate build at `42dd143661a81185c110e7e761d12d41ead15cf7` is retained in issue history: its new static test incorrectly matched the words `SubViewport.Size` inside a comment; the assertion was narrowed to actual assignment syntax.

Release-identity commit `36093b66f1a9e746e2a46f5c0d55afd35aa35b84` advances all editor/backend/launcher/updater/export/installer version surfaces to `1.0.22` and strengthens release audit coverage for the new acceptance guard. Exact-head CI has already passed Core plus the Python/execution/geometry/release-audit and C# legs; packaging is the remaining branch-validation leg at this reconciliation point.

## Current priorities

1. Complete exact-head v1.0.22 validation and publish only if all autonomous Windows export/hash/installer-smoke gates pass.
2. Reference-machine retest of released v1.0.22: generate → visible Ready/Conflict candidate → Apply → save/close/reopen → same durable object/active mesh revision; verify Move/Rotate/Scale, cleanup and STL export.
3. In the same session verify initial/right-panel/whole-window viewport presentation and absence of black seams/opaque floor/starter sphere.
4. Then continue MS-018, MS-013, MS-022 and MS-004 acceptance evidence.
5. MS-026 telemetry and broader MS-019/MS-020 architecture remain behind the current acceptance blockers unless the Coordinator changes priority.

## User input currently required

No product/design decision is required. After v1.0.22 is released, reference-machine verification is the next important evidence dependency.
