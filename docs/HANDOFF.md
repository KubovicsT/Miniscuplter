# Miniscuplter Handoff

> Immediate execution baton. TECHNICAL_ROADMAP owns strategic direction; inspect actual Git/release/CI first.

Last updated: 2026-09-11

## Current state

- **Latest published stable:** `v1.0.22` at `c1ba2d01517cc6bca5a6e6cde3b1e85f853dd0d7`.
- **Current writable development branch:** `v1.0.24`.
- **Frozen release source:** `v1.0.23` at exact boundary `a6532003bc6ecfcf79fc15afa3ee772cb117b982`.
- **Latest validated v1.0.24 implementation/test checkpoint:** `7255bb75f0ea18409fe5ad63734cbd99c17ba18d`.
- **Overall completion:** **57% acceptance-weighted**.
- **Coordinator critical path:** Stage-C reference-machine acceptance. Any reproduced correctness/persistence/viewport/data-safety/storage/cancellation regression preempts architectural/fallback work.

## Release state

Do not modify v1.0.23 or release-control from Dev. v1.0.22 remains the latest published release.

The first v1.0.23 publication attempt failed output-version verification because the frozen source still produced v1.0.22 release metadata/package naming. A later Coordinator-owned corrected-candidate retry (`release-control` run `34562412676`) also failed, this time in the combined geometry/release-audit step before Windows export/package publication. Coordinator owns diagnosis and any directed frozen-source correction. This Dev cycle made no release-control, tag, release, or v1.0.23 mutation.

## Previously completed bounded MS-020 baton

The explicit restart/recovery slice remains complete at validated checkpoint `c8451493b31306d12db7ed360a836e3e3b941fb2`:

1. migrated heavyweight `3d-generate` work has one process-local runtime lease;
2. component install/update/repair/remove shares the same heavyweight owner;
3. cancellation remains `cancelling` and retains ownership until physical terminal acknowledgement; late success cannot qualify/apply;
4. compact lifecycle state is journaled under the existing Miniscuplter-controlled backend root using atomic replacement;
5. dead-process `running` work reconciles to inactive `interrupted`; stale cancelling work reconciles to inactive `cancelled`;
6. restart never reacquires the process-local GPU lease and never restores/auto-applies a prior candidate;
7. corrupt/unsupported journal state fails closed as inactive `recovery-error`.

Do **not** expand this into a generalized persistent queue or isolated-worker rewrite without new Coordinator sequencing.

## This Dev Cycle — release-version identity regression guard

No new target-machine acceptance evidence or new Coordinator MS-020 slice was available. Rather than invent broader broker work, this run addressed a concrete release-safety recurrence: semantic-version branches could inherit stale prior-version metadata while both the hard-coded release audit and package surfaces agreed with each other, allowing ordinary branch CI to look healthy until release-control compared against the requested version.

Implementation checkpoint:

- `7255bb75f0ea18409fe5ad63734cbd99c17ba18d` — adds an early branch-derived `Verify semantic-version branch identity` CI step on `v1.*` pushes.

The guard derives expected identity from `github.ref_name` and checks:

- launcher assembly version;
- updater assembly version;
- editor assembly version;
- Inno Setup application version;
- Windows exported file version;
- Windows exported product version;
- backend API `APP_VERSION`;
- displayed editor version;
- `tools/release_audit.py` expected version.

Any stale surface now fails the development-branch CI immediately with a consolidated mismatch report. This specifically closes the process gap exposed by the v1.0.23 publication failure and the multi-file v1.0.24 identity reconciliation without changing Coordinator release ownership.

## Validation

Exact implementation/test checkpoint `7255bb75f0ea18409fe5ad63734cbd99c17ba18d` is fully green:

- `core-foundation` run `34568222471`: **PASS**;
- broader `build` run `34568222539`: **PASS**;
- new branch-derived semantic-version identity guard: **PASS**;
- C# editor/launcher/updater/Core builds: **PASS**;
- Python compilation/runtime dependency resolution: **PASS**;
- core logic and execution/job regressions: **PASS**;
- real geometry regressions: **PASS**;
- release audit: **PASS**;
- portable package layout and ZIP SHA-256 verification: **PASS**;
- installer-definition compilation: **PASS**;
- branch push correctly skipped tag-only full Windows release/publication jobs.

Documentation commits after this checkpoint do not supersede the validated implementation/test checkpoint.

## Exact next task

1. Inspect actual release/branch/CI state and re-read current ROADMAP / COORDINATOR_LOG first.
2. Consume any new reference-machine Stage-C/cancellation/storage evidence immediately; a serious reproduced regression preempts everything else.
3. If no new evidence exists, follow the next Coordinator-sequenced bounded task. **Do not independently broaden MS-020 now that its explicitly ordered restart seam is complete.**
4. Preserve the semantic-version identity guard when creating/advancing future version branches; any branch-name/version mismatch is a release-safety blocker, not something to waive.

## Release/checkpoint rule

`7255bb75f0ea18409fe5ad63734cbd99c17ba18d` is a useful validated implementation checkpoint. It does not freeze v1.0.24 and does not authorize publication. Release readiness/chunk size/publication remain Coordinator-owned.

## User verification dependency

Reference-machine testing remains required. `v1.0.22` is still the latest successfully published build, so exercise:

`accepted 2D baseline → Generate 3D → candidate visible/reviewable → Apply → save → close/reopen → same object/revision → Move/Rotate/Scale/sculpt → cleanup → exact STL export`

Also verify whole-window/right-panel resize presentation, no starter sphere/opaque floor, storage containment, cancellation/recovery and resource behavior during a long AI job. The v1.0.24 restart journal and release-identity CI hardening are development-only until they reach a published build.
