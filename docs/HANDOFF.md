# Miniscuplter Handoff

> Operational baton for the next development run. Inspect actual Git/release/CI state first; repository state wins over this document if they differ.

Last updated: 2026-09-10

## Current state

- **Repository:** `KubovicsT/Miniscuplter`
- **Latest published stable:** `v1.0.19`
- **Stable application commit:** `52f3b95fb6addc0f9f1e7123b75068da4ef1513c`
- **Current development branch:** `v1.0.20`
- **v1.0.20 base:** exact released v1.0.19 commit above
- **Latest application/code commit from this run:** `8cd00173b78577fed040cce5d71e05738cf404be`
- **Overall completion estimate:** 56% acceptance-weighted; unchanged pending target-machine Stage-C evidence.

Documentation commits after the application commit advance branch HEAD. Resolve exact branch HEAD and CI from Git at the start of the next run.

## What this run did

Continued **MS-022 provider qualification**. Added `ai_backend/provider_readiness.py` and integrated it with 3D routing.

The readiness contract now represents downloaded, installed, importable, device-tested and inference-tested separately, with timestamp, runtime/provider revision, device details, failure detail and optional benchmark metadata. Lightweight generation-time probes cover the main single-mesh Stage-C candidates: TripoSR, Hunyuan3D 2mini, Hunyuan3D 2.1 Shape, Stable Fast 3D and SPAR3D. Probes validate provider imports and CUDA visibility without loading model weights or pretending a real inference occurred.

3D Auto routing now skips an installed provider that fails readiness preflight and chooses only a readiness-eligible fallback. Explicit provider selection remains strict: if that provider is installed but its runtime/device probe fails, the request fails early with the concrete readiness state/failure rather than silently substituting another model.

`routing_status()`/health polling deliberately uses cached/persisted readiness only. The first integration risked running subprocess probes from ordinary health polling; senior self-review caught that before release and commit `b8b07f2399b9c5b48c977b826d3b3a00ece9e9d9` separated cheap status inspection from generation-time preflight.

Regression coverage was added to `tools/core_logic_tests.py` for readiness-aware fallback, explicit-provider failure, independent readiness states and inference-success benchmark persistence. A test-fixture persistence bug was found while reviewing the first CI attempt and corrected in `8cd00173b78577fed040cce5d71e05738cf404be`.

Relevant commits:

- `5d6b5dde8d9159699c2524eb21753e4c8d1aa994` — Add 3D provider readiness preflight
- `b8b07f2399b9c5b48c977b826d3b3a00ece9e9d9` — Keep health checks free of provider subprocess probes
- `ab60bd1e73ef0f074ce9d1806fe94247d6f0b7f1` — Fix provider readiness regression fixture
- `8cd00173b78577fed040cce5d71e05738cf404be` — Fix readiness state test persistence

## Validation state

The earlier build on `ab60bd1...` proved editor/launcher/updater/Core C# build and portable packaging/installer compilation, but its Python core-logic step failed in the newly added mocked persistence fixture. That was a test bug, not a demonstrated application/runtime failure, and it was fixed in `8cd00173...`.

At handoff-write time, fresh `build` and `core-foundation` workflows for `8cd00173...` had started and were still running. Reconcile their final result before making a release decision. No v1.0.20 release was made in this run.

Real CUDA inference was **not** performed by CI. Do not mark a provider inference-tested because import/device preflight passed. `record_inference_success()` exists to store real successful inference/benchmark evidence, but the normal `/generate-3d` success path does not yet call it automatically.

## Current unresolved priorities

1. **MS-009 — viewport/grid/model/gizmo:** v1.0.19 fix is published but still needs target-PC verification. Any reported blank viewport becomes immediate priority.
2. **MS-018 — Stage-C thin slice:** still the critical product acceptance gap.
3. **MS-013 — storage containment:** v1.0.19 hardening is published but still needs representative real-machine path verification.
4. **MS-022 — provider qualification:** now materially in progress; runtime/import/device preflight exists, but real inference qualification/default-provider benchmark evidence is not complete.
5. **MS-020 — authoritative Job Broker/stale-result safety:** next vertical-slice architecture work.
6. **MS-019 — legacy `Main.V*.cs`:** migrate one reliable slice at a time rather than adding new widget-owned state.

## Exact next task

First reconcile CI for `8cd00173b78577fed040cce5d71e05738cf404be`; if the new readiness work fails, fix it before continuing.

If green, finish the useful MS-022 seam by wiring a **verified successful `/generate-3d` output** to `record_inference_success()` using the final provider actually used (including Auto fallback) and elapsed time, without marking input-specific failures as globally broken. Expose/retain enough hardware context for target-machine benchmark evidence. Keep health/status polling non-blocking.

Then move directly into **MS-018 + MS-020**: bind `accepted 2D baseline revision → qualified 3D job → candidate mesh revision → explicit transactional accept/apply`. The job must carry immutable project/object/input revision identity; if the baseline/current object changes while inference runs, the result becomes a candidate/conflict and must never silently overwrite newer state.

## User verification requested, not blocking autonomous work

On released v1.0.19, test the 3D viewport/grid/starter or generated mesh/gizmo on the GTX 1080 machine and representative 2D/3D/storage operations. If the viewport is blank, retain the v1.0.19 viewport diagnostic/render-probe text. If storage escapes the configured data root, record the unexpected path(s).

## Release policy

Do not mutate v1.0.19. Do not publish v1.0.20 merely because a run ends. Before publication, require the relevant C#/Python/Core/job/geometry/release-audit gates plus a real Godot Windows export, artifact/hash verification and installer smoke test. After publication, verify GitHub latest-release state and create/use v1.0.21 before further application changes.
