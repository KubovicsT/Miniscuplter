# Miniscuplter Technical Roadmap

> Coordinator-owned sequencing, priority, architecture and release-chunk authority. Project Charter/accepted Decisions and actual repository/release truth outrank this document; HANDOFF owns the immediate Dev baton.

Last coordinator review: 2026-09-11
Latest published stable: `v1.0.22`
Frozen release source: `v1.0.23`, release-fix candidate `01c81fededa38ce73f8eaec997558cafb6ebd2cc`
Current writable development branch: `v1.0.24`
Acceptance-weighted completion: **57%**

## Current objective

Complete **Stage-C reference-machine acceptance** while continuing forward development on v1.0.24 and repairing the diagnosed v1.0.23 release-source version defect without weakening release gates.

Accepted thin slice:

`2D source → accepted durable baseline → local 3D generation → identity-bound candidate → Apply → durable editable object → transform/sculpt → save/reload → cleanup → exact STL export`

Released-v1.0.22 fixes remain **FIXED - NEEDS USER VERIFICATION** where applicable until real-machine retest passes.

## Direction assessment

**PRESERVE.** Core owns durable state; Godot owns viewport/input/presentation; Python owns inference/geometry; stable IDs, immutable revisions, transactional history, controlled storage and immutable published releases remain mandatory.

The completed MS-027 sequence respected these boundaries and is now closed to speculative expansion. v1.0.24 structural fallback is bounded MS-020 reliability work only while Stage-C acceptance is externally blocked.

## v1.0.23 release status

The first autonomous v1.0.23 publication attempt failed **after** exact request validation, C#/Core/Python/job/geometry/release-audit validation and a successful real Godot Windows build. Output verification correctly rejected the package because the frozen source still declared release version `1.0.22` in both `Launcher/Miniscuplter.Launcher.csproj` and `installer/Miniscuplter.iss`; consequently the build produced `Miniscuplter-Setup-1.0.22.exe` while release-control required `1.0.23`.

This is a narrow release-source metadata defect, not an application architecture failure.

Coordinator applied the minimum frozen-source correction:
- launcher package version → `1.0.23`;
- installer version/output name → `1.0.23`;
- corrected frozen source HEAD → `01c81fededa38ce73f8eaec997558cafb6ebd2cc`;
- release-control request updated to that exact SHA for a full-gate retry.

The same defect was proactively prevented forward by setting the writable v1.0.24 launcher/installer metadata to `1.0.24`. Do not revert those version declarations.

v1.0.23 remains frozen. No other source changes belong there unless the current retry exposes another concrete release-source defect.

## Ordered critical path

### P0 — v1.0.23 publication integrity

Monitor the corrected exact-SHA retry through output verification, installer smoke test and publication. Do not weaken any gate. If it fails, diagnose the exact failure before another source change.

### P1 — consume target-machine evidence

Any reproduced persistence, data-loss, viewport, storage, cancellation, provider or Stage-C blocker immediately preempts structural fallback work.

### P2 — complete Stage-C acceptance

Required evidence includes generation → candidate → Apply → save/reopen identity, transform/sculpt, cleanup/export, storage containment, one qualified practical provider, and cancellation/recovery on the reference machine.

### P3 — bounded v1.0.24 MS-020 fallback

Dev trajectory is aligned. Three bounded MS-020 slices now establish migrated heavyweight lifecycle/resource ownership and truthful cancellation semantics. The latest validated implementation checkpoint before Coordinator release-version propagation is `f5ea1378c4ff1ba177262a275469ef8d310d7cc2`.

Next bounded slice: persist only the minimum job lifecycle/tombstone state needed to reconcile backend restart/crash truthfully.

Requirements:
- contained Miniscuplter-controlled state root;
- compact identity/kind/state/cancellation/timestamps/Stage-C context only;
- atomic replacement;
- active/cancelling record from a dead prior process becomes explicit interrupted/cancelled terminal state on recovery;
- never auto-apply interrupted outputs;
- fail closed on corrupt journal;
- preserve one-heavyweight-owner behavior;
- focused regressions for completion, restart reconciliation, corrupt journal and storage containment;
- stop before generalized persistent queue or isolated provider-worker redesign.

### P4 — outward migration after proven Job Broker seams

Continue MS-019 duplicate-authority retirement only after replacement paths are proven. Broader Stage-D/Rig/kitbash/provider breadth remains later.

## Release model

Dev records useful checkpoints and continues. Coordinator chooses release chunk size from current branch HEAD. Only release-request creation freezes a source branch. At freeze, a forward semantic-version branch must already exist so Dev can continue. Failed releases preserve the frozen source until diagnosis; only minimum concrete release-source fixes are allowed there. Published tags/releases are never rewritten.

## Risks

1. CI/release success being mistaken for target-machine acceptance.
2. accidental writes to frozen v1.0.23 beyond diagnosed release repair.
3. release-version metadata drifting behind semantic-version branches again.
4. MS-020 expanding into generalized infrastructure rather than Stage-C reliability.
5. duplicate job/process authority during migration.
6. cancellation being reported before physical worker stop.
7. storage leakage from job/runtime/cache paths.

## Next Coordinator-level objectives for Dev

1. Work only on v1.0.24; v1.0.23 remains Coordinator-owned/frozen.
2. Preserve v1.0.24 launcher/installer version metadata at `1.0.24`.
3. Consume new target-machine evidence first every cycle.
4. If none exists, implement only the bounded restart-reconciliation MS-020 slice described above.
5. Preserve current Stage-C behavior and add focused regressions.
6. Keep completion at 57% until acceptance evidence changes it.
7. Do not create release requests/tags/releases.

## User dependency

No product-level decision is required. Reference-machine testing remains the external dependency. Until v1.0.23 successfully publishes, v1.0.22 remains stable. After publication, prefer v1.0.23 for the complete Stage-C and UI/telemetry acceptance pass.
