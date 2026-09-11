# Miniscuplter Technical Roadmap

> Coordinator-owned sequencing, priority, architecture and release-chunk authority. Project Charter/accepted Decisions and actual repository/release truth outrank this document; HANDOFF owns the immediate Dev baton.

Last coordinator review: 2026-09-11
Latest published stable: `v1.0.22` pending v1.0.23 publication
Frozen release source: `v1.0.23` at `a6532003bc6ecfcf79fc15afa3ee772cb117b982`
Current writable development branch: `v1.0.24`
Acceptance-weighted completion: **57%**

## Current objective

Complete **Stage-C reference-machine acceptance** while continuing forward development without racing the frozen v1.0.23 release source.

Accepted thin slice:

`2D source → accepted durable baseline → local 3D generation → identity-bound candidate → Apply → durable editable object → transform/sculpt → save/reload → cleanup → exact STL export`

Released-v1.0.22 fixes for MS-023/MS-009/MS-024/MS-025 and related acceptance issues remain **FIXED - NEEDS USER VERIFICATION** where applicable until real-machine retest passes.

## Direction assessment

**PRESERVE.** The selective-refactor architecture continues to converge:

- Core owns durable project/object/revision/history state.
- Godot owns viewport/input/presentation.
- Python owns inference/geometry execution.
- stable IDs, immutable revisions and transactional history remain mandatory;
- STL is interchange/export, never project authority;
- storage stays inside controlled roots;
- published releases are immutable and fixes move forward.

The completed MS-027 sequence respected these boundaries: newer presentation slices increasingly moved to version-neutral components and delegated to existing selection/camera/AI/job owners instead of duplicating authority.

## Release decision / chunking

**v1.0.23 reached the release boundary.**

The accumulated scope is now a coherent, substantial user-testable increment rather than a collection of isolated checkpoints. It includes:

1. workspace splitter persistence, UI/font scaling and tooltip infrastructure;
2. direct viewport tool controls;
3. synchronized collapsible scene hierarchy;
4. view cube and selected-object orbit pivot;
5. unified AI command/history surface through authoritative action owners;
6. MS-026 local resource telemetry;
7. density/spacing cleanup and retirement of superseded instructional UI;
8. MS-028 compile-regression repair.

The exact frozen boundary is `a6532003bc6ecfcf79fc15afa3ee772cb117b982`. v1.0.24 was created from that SHA before release initiation so Dev throughput can continue. v1.0.23 must not move while release-control is active.

Publication does not constitute target-machine acceptance and does not close verification-needed issues.

## Ordered critical path

### P0 — Consume target-machine evidence

At every Dev cycle start, check for new user/reference-machine evidence. Any reproduced persistence, data-loss, viewport, storage, cancellation, provider or Stage-C blocker immediately preempts structural fallback work.

### P1 — Complete Stage-C acceptance

Required evidence:
- intended local 3D route completes;
- Ready/Conflict candidate appears;
- Apply advances durable object/revision;
- save/close/reopen restores the same object/revision;
- Move/Rotate/Scale and one supported sculpt/edit path work;
- cleanup/export follows durable revision authority;
- storage containment holds;
- one practical provider is qualified on GTX 1080 / 8 GB VRAM / 16 GB RAM;
- cancellation/recovery does not poison the next job.

### P2 — v1.0.24 structural fallback while acceptance is externally blocked

MS-027's planned UI sequence is complete. Do **not** invent another UI slice merely to create activity.

Begin **MS-020 durable Job Broker ownership/resource locking/cancellation/crash recovery** in bounded slices that directly improve the accepted Stage-C workflow. First objective: map current job/process ownership and establish one authoritative lifecycle seam for a long-running local job without broad provider or UI expansion.

Guardrails:
- one heavyweight GPU job by default;
- immutable input revision per job;
- structured progress/output identity;
- cancellation acknowledged only after worker stop;
- runtime install/remove/repair shares resource ownership locking;
- preserve current working Stage-C behavior during migration;
- no broad rewrite before each migrated seam has acceptance/regression coverage.

### P3 — outward migration after proven Job Broker seams

Continue MS-019 retirement of duplicate legacy authority only after replacement paths are proven. Migration-before-removal remains mandatory.

### P4 — provider policy and Stage-D breadth

Use measured MS-022 evidence before choosing Fast/Balanced/Quality defaults. Broader sculpt/editing, kitbash and Rig/Pose remain behind Stage-C/Job-Broker reliability unless new evidence changes dependency order.

## Issue priority

1. MS-023 — Critical / FIXED - NEEDS USER VERIFICATION.
2. MS-018 — Critical / IN PROGRESS.
3. MS-009 — Critical / FIXED - NEEDS USER VERIFICATION.
4. MS-024 — High / FIXED - NEEDS USER VERIFICATION.
5. MS-013 — High / FIXED - NEEDS USER VERIFICATION.
6. MS-022 — High / IN PROGRESS.
7. MS-004 — High / FIXED - NEEDS USER VERIFICATION.
8. MS-020 — High / next unblocked structural objective while acceptance waits.
9. MS-025 — Medium / FIXED - NEEDS USER VERIFICATION.
10. MS-027 — Medium / planned bounded sequence COMPLETE IN CODE; visual/user verification pending.
11. MS-026 — Medium / implemented within MS-027; target telemetry plausibility/overhead verification pending.
12. MS-028 — RESOLVED.

## v1.0.24 intended scope

Allowed:
- fixes driven by target-machine acceptance evidence;
- Stage-C diagnostics/regressions;
- bounded MS-020 Job Broker durability/resource/cancellation work while user evidence is unavailable;
- propagation of any concrete release-source fix required by v1.0.23 publication failure.

Not currently in scope:
- additional speculative UI modernization;
- monolithic Main.V* rewrite/removal;
- full provider expansion;
- broad sculpt migration;
- Rig & Pose modernization;
- kitbash expansion;
- unrelated feature breadth.

## Release model

Dev records useful validated checkpoints and keeps developing. Checkpoints are advisory, not freezes. Coordinator chooses release chunk size from the **current** branch HEAD. At release boundary, Coordinator creates the next forward semantic-version branch from the exact frozen SHA, reconciles writable-branch docs there, then creates release-control for the frozen prior branch. Only the active release request freezes that source branch. Never rewind a moving version branch to publish an older checkpoint and never rewrite a published tag/release.

## Risks

1. CI/release success being mistaken for real-machine acceptance.
2. Dev writing to frozen v1.0.23 instead of v1.0.24.
3. MS-020 becoming generalized infrastructure rather than Stage-C reliability work.
4. duplicate job/process authority during migration.
5. cancellation being reported before the worker actually stops.
6. UI modernization restarting after its planned sequence is complete.
7. telemetry/provider observations being overclaimed as a support matrix.
8. storage leakage from job/runtime/cache paths.

## Next Coordinator-level objectives for Dev

1. Work only on v1.0.24; treat v1.0.23 as frozen while release-control is active.
2. Consume new target-machine evidence first every cycle.
3. If none exists, start one bounded MS-020 lifecycle/ownership slice directly serving long-running local job reliability.
4. Preserve current Stage-C behavior and add focused regression coverage around any migrated job seam.
5. Do not start another MS-027 slice.
6. Keep completion at 57% until acceptance evidence changes it.
7. Do not create release requests/tags/releases.

## User dependency

No product-level decision is required. Reference-machine testing remains the external dependency. Prefer the newest successfully published build when available and test the complete Stage-C path plus viewport resize/presentation, storage, cancellation/recovery, and resource telemetry during a long AI job.
