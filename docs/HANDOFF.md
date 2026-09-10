# Miniscuplter Handoff

> Immediate execution baton. Inspect actual Git/release/CI first; TECHNICAL_ROADMAP owns strategic direction.

Last updated: 2026-09-10

## Current state

- **Latest published stable:** `v1.0.22` at `c1ba2d01517cc6bca5a6e6cde3b1e85f853dd0d7` (immutable).
- **Current development branch:** `v1.0.23`.
- **Current MS-027 tool-strip head:** `637282b2831a1c33d5fab9872b88b534e04593bf`.
- **Previous fully validated bounded candidate:** `74ec73a14645071bb2742fb68f778bedd656aabd`.
- **Overall completion:** **57% acceptance-weighted**.
- **Critical path:** released-v1.0.22 Stage-C reference-machine acceptance. Any reproduced correctness/persistence/viewport/data-safety regression preempts UI fallback work.

## This run

Reference-machine evidence was still unavailable and no newer severe blocker was present, so the run executed exactly one Coordinator-approved MS-027 fallback slice: **direct viewport tool controls**.

- `Main.V1023ViewportToolStrip.cs` hides the historical dropdown and presents compact direct Select / Move / Rotate / Scale / Sculpt controls.
- Buttons are presentation-only: they call the existing `V1018ToolSelected` path and read the existing `_v1018Tool`; no second tool/input state machine was introduced.
- The active tool is visibly latched through `ButtonPressed`.
- Each compact control carries a tooltip instead of adding another instructional panel.
- Existing v1.0.18 viewport input, v1.0.22 viewport ownership and Stage-C transform/sculpt authority remain unchanged.
- `ExtrasInstaller` composes the strip after `InstallV1023UiPreferences()`.

Commits:
- `78175d712239449eb525ee12391d3c66c55ebde7` — direct tool strip;
- `637282b2831a1c33d5fab9872b88b534e04593bf` — final composition wiring.

## Validation

Exact-head validation has now completed and is **RED**. Core-foundation passed, and Python/runtime/geometry/release-audit plus packaging passed, but broader build fails in C# with `CS0122`: `ExtrasInstaller` calls `Main.InstallV1023ViewportToolStrip()` while that method is inaccessible. The later documentation-only head repeats the same C# failure. The previous v1.0.23 candidate `74ec73a...` remains the last fully green bounded candidate. **Do not start another feature slice until MS-028 is fixed and exact-head validation is green.**

No v1.0.23 release request exists. Do not release merely because this secondary UI slice becomes green.

## Primary next task

First inspect released-v1.0.22 target-machine evidence for MS-023/MS-024/MS-025/MS-018 and related MS-013/MS-022/MS-004 evidence. If any serious acceptance failure exists, fix it before UI work.

If acceptance evidence is still unavailable:
1. **fix MS-028 first**: the C# `CS0122` accessibility/composition error for `InstallV1023ViewportToolStrip()`;
2. obtain green exact-head C#/Core/Python/geometry/release-audit/packaging validation and update this handoff to the repaired SHA;
3. if green and no acceptance blocker exists, take exactly one next Coordinator-approved MS-027 slice: **synchronized collapsible scene hierarchy**, reusing existing scene/object selection authority rather than creating duplicate project state;
4. stop before view cube/AI console/telemetry in that cycle.

## User dependency

No product decision is required. The external dependency remains reference-machine testing of released v1.0.22: generate → candidate → Apply → save/close/reopen → same durable object/revision, then transform/sculpt, cleanup/export, resize/presentation, storage containment and provider/resource behavior.
