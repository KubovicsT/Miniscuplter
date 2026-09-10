# Miniscuplter Handoff

Last updated: 2026-09-10

## Current state

- **Repository:** `KubovicsT/Miniscuplter`
- **Latest published stable:** `v1.0.18`
- **Stable application commit:** `ce2d876fc145e615d63bd8d9fc809610f6038301`
- **Current development branch:** `v1.0.19`
- **Latest code commit:** `2b5f848c51f5be6ead21a9e5e6bfa3e243a2a078` — readiness-aware routing tests
- **Previous code commits this run:** `159dd9063ba49d35797eed36f86c2f89b16346e9` provider readiness module; `a9de5a7b6af03a8c970b5ba8cc882dc6db0ce405` readiness-aware 3D routing
- **Overall completion:** remains **56% acceptance-weighted**.

## What changed this run

The previous viewport-sizing fix completed CI successfully. This run advanced **MS-022 provider qualification** and Stage-C reliability.

A new `ai_backend/provider_readiness.py` performs lightweight preflight checks for 3D providers before a long generation begins. It verifies installed component state plus important import/device requirements for supported local routes. TripoSR specifically checks the source-tree import and preprocessing dependencies; Hunyuan checks its source import; local GPU routes require CUDA visibility. These checks deliberately do not claim full-inference/VRAM qualification.

`model_router.py` now uses readiness for 3D routing:

- explicit provider selection fails early with an actionable readiness reason when installed but unusable;
- Auto routing skips installed-but-unready providers instead of discovering missing dependencies only after job startup;
- automatic fallback is selected from providers that also pass readiness;
- `/routing`/health routing status exposes per-provider readiness details.

`tools/core_logic_tests.py` now covers early skip/fail-fast semantics by mocking provider readiness independently from installation state.

No published release was modified.

## Validation state

The new commits triggered normal `build` and `core-foundation` CI. The next run must inspect the final conclusions for the latest branch HEAD and fix any regression before continuing. The previous viewport-sizing/handoff commits were observed green before this work began.

Real GUI/CUDA/inference verification remains separate from static CI. MS-009 and MS-013 still require target-machine testing; MS-022 remains in progress because lightweight readiness is not yet an inference self-test or benchmark.

## Highest-priority unresolved work

1. **MS-018 — Stage-C thin slice:** `2D → durable accepted baseline → qualified 3D → visible/editable mesh → save/reload → cleanup → validated STL` on GTX 1080/8 GB + 16 GB RAM.
2. **MS-009 — viewport/grid/model/gizmo reliability:** stronger v1.0.19 pipeline exists but needs real-machine verification.
3. **MS-022 — provider qualification:** extend lightweight readiness into a bounded self-test for the preferred lightweight Stage-C 3D route and eventually record inference-tested/benchmark state.
4. **MS-013 — storage containment:** architecture/tests are strong but representative real-machine operations still need verification.
5. **MS-020 / MS-019 — authoritative Job Broker and legacy-state migration:** continue vertical-slice migration and stale-result protection.

## Exact next action

1. Check `build` and `core-foundation` for the latest v1.0.19 HEAD; fix failures first.
2. If green, add a bounded self-test contract for the preferred Stage-C lightweight 3D provider, starting with TripoSR or the currently best-installed low-hardware route. The self-test should validate imports/device/model-open capability without doing an expensive production generation where possible.
3. Feed readiness/self-test state into provider status so UI/routing can distinguish downloaded, installed, importable, device-tested and inference-tested rather than treating presence as readiness.
4. Continue MS-018 by moving accepted 2D baseline → generated 3D result/import onto stable project/revision identity with stale-result safety.
5. Do not raise the 56% completion estimate until acceptance evidence improves.
