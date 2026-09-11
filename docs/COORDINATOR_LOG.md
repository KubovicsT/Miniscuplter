# Miniscuplter Coordinator Log

> Durable Project Coordinator memory. Preserve strategic decisions, rejected/failed approaches, evidence and outcomes so direction does not oscillate without new evidence.

## Durable architecture / sequencing history

### 2026-09-10 — Selective-refactor direction preserved

Coordinator accepted the takeover architecture as the governing direction rather than a ground-up rewrite:

- Core owns durable project/object/revision/history state;
- Godot owns presentation, input and viewport behavior;
- Python owns inference/geometry execution;
- stable IDs, immutable revisions and transactional history replace widget/name authority incrementally;
- STL is interchange/export, not internal project truth;
- migration is proven before legacy authority is removed.

v1.0.20 was deliberately bounded to Stage-C state authority: durable transforms, one immutable sculpt/edit path, save/reload/undo/cleanup/export regression coverage. Full sculpt migration, full Job Broker reconstruction, Rig/Pose, kitbash, provider proliferation and broad legacy removal were explicitly deferred.

### 2026-09-10 — Autonomous release-control validated

The permanent `release-control` mechanism became the exact-SHA publication path. Early orchestration defects were preserved as lessons:

1. request discovery initially examined only the triggering commit instead of the full push range;
2. an expected failing `gh release view` probe left `$LASTEXITCODE` nonzero.

Both were fixed without weakening release gates. v1.0.20 then published through exact-SHA validation, C#/Core/Python/geometry checks, real Godot Windows export, package/hash verification, installer smoke test and immutable-target verification.

Published releases are immutable; failures are fixed forward.

### 2026-09-10 — Reference-machine evidence outranks CI

Released v1.0.20 target testing reopened viewport/rendering work despite green CI. Duplicate SubViewport/world/presentation ownership matched the resize-dependent symptom. Coordinator rejected another overlay/full viewport rewrite and narrowed v1.0.21 to one native resize/world/presentation owner plus neutral Blender-like workspace presentation.

v1.0.21 target retest partially passed: initial viewport improved, but right-panel resize still changed presentation and whole-window resize left black seams. This narrowed follow-up work rather than invalidating the architecture.

### 2026-09-10 — v1.0.22 acceptance fixes

User evidence showed generated 3D could appear while Stage-C candidate state remained `none`, then disappear after restart. Root cause was duplicate historical Generate-3D event ownership. v1.0.22 therefore installed a final single generation owner while preserving the migrated Core candidate/Apply path. The release also contained client-fill/resize and starter-scene/presentation guards.

These fixes remain `FIXED - NEEDS USER VERIFICATION` until the released flow passes on the reference machine.

### 2026-09-10 — MS-027 bounded UI fallback

User-directed workspace modernization was accepted as an opportunistic fallback only while Stage-C acceptance was externally blocked. Bounded slices were sequenced instead of a monolithic rewrite:

- workspace splitter persistence / UI scale / tooltips;
- direct viewport tool strip reusing existing tool state;
- synchronized scene hierarchy;
- view cube + selection-centered orbit;
- unified AI command/history dispatcher;
- resource telemetry;
- density/polish.

A development-only compile regression (MS-028) from inaccessible tool-strip composition was fixed narrowly; this reinforced exact-head validation before further fallback work.

New presentation work should prefer stable version-neutral components instead of normalizing new `Main.V10xx...` layers.

## Release ownership history

### 2026-09-10 — Coordinator became exclusive release owner

User reassigned release readiness/publication from Dev to Coordinator. Dev prepares/validates checkpoints; Coordinator decides release chunk size and owns release-control/publication.

### 2026-09-10 — Continuous-development release model superseded stop-at-candidate behavior

User clarified that release-worthy checkpoints do **not** freeze development. Dev records useful checkpoint SHAs and continues. Coordinator chooses a sufficiently large/coherent release boundary at the **current semantic-version branch HEAD**, creates/uses the next forward version branch, then freezes only the prior source branch by creating the release request.

Never rewind a moving version branch to publish an older checkpoint. A failed publication leaves the prior source frozen until the concrete failure is diagnosed.

## 2026-09-11 — v1.0.23 release chunk / v1.0.24 continuous development

v1.0.23 accumulated the planned MS-027 sequence and became a sufficiently coherent testable release chunk. Coordinator froze v1.0.23 and created v1.0.24 from the exact release boundary so Dev could continue independently.

v1.0.24 then advanced a bounded MS-020 reliability seam while Stage-C user evidence remained unavailable:

- one heavyweight runtime owner;
- shared ownership for component/runtime mutation;
- truthful cancellation retaining ownership until actual termination;
- compact durable heavyweight-job lifecycle/tombstone state;
- restart reconciliation of abandoned running/cancelling jobs;
- fail-closed corrupt/unsupported state;
- bounded journal reads before decode/parse.

Validated checkpoint: `e3dfba9aa26d7045b4bf9602920a484c443789c7`.

This proves the ordered restart-reconciliation seam sufficiently for current needs. **Do not broaden MS-020 into a generalized persistent queue merely because acceptance remains blocked.**

## 2026-09-11 — v1.0.23 publication failure history and successful repair

### Failure 1 — partial release identity

The first v1.0.23 publication attempt passed substantial validation and reached the Windows build path, but generated 1.0.22 release identity/installer naming because the frozen source had not been advanced consistently. Publication was correctly blocked.

A first repair advanced launcher/installer identity only. That repair was incomplete.

### Failure 2 — strict audit exposed the incomplete repair

The second autonomous retry failed during strict release audit before Windows export. Exact request/source SHA validation, C#/Core, Python/runtime/job and geometry checks passed. The audit still expected 1.0.22 and rejected the partially updated source.

Inspection proved the full defect. Before complete repair:

- launcher = 1.0.23;
- installer = 1.0.23;
- updater = 1.0.22;
- Godot C# assembly = 1.0.22;
- Windows file/product metadata = 1.0.22;
- backend API = 1.0.22;
- editor displayed version = 1.0.22;
- release-audit expected version = 1.0.22.

Coordinator classified this as one release-source identity defect, **not** a reason to weaken the audit.

### Complete repair and publication

Coordinator advanced the complete audited v1.0.23 identity set to 1.0.23 without broadening application scope. Final source candidate:

`bda683264448fc8b51c7c538db61f8c0487a699a`

Exact-head `build` run `34575269020` and `core-foundation` run `34575269038` both passed. Coordinator then updated the existing release-control request to that exact SHA.

Autonomous release run `34575505010` completed successfully through:

- request/source SHA and no-existing-release/tag checks;
- exact candidate checkout;
- C#/Core build and tests;
- Python/runtime dependency checks;
- core/job regressions;
- geometry regressions;
- strict release audit;
- verified Godot 4.7.2 .NET/templates;
- full Windows release build;
- versioned package/ZIP/SHA verification;
- silent `Miniscuplter-Setup-1.0.23.exe` smoke install;
- staged/uploaded verified assets;
- immediate immutable-target recheck;
- lightweight `v1.0.23` tag creation;
- GitHub Release publication.

GitHub latest release now reports `v1.0.23` targeting exactly `bda683264448fc8b51c7c538db61f8c0487a699a`; the tag ref resolves to the same commit.

**Lesson:** release version identity is a coordinated contract across all user/tool-visible surfaces. Future release preparation must advance and audit them as one set. Preserve the strict audit.

v1.0.23 is now immutable; development continues only on v1.0.24.

## 2026-09-11 — Post-MS-020 sequencing / v1.0.24 release decision

### Direction assessment

**PRESERVE** Stage-C as the critical path and the selective-refactor architecture.

The explicitly ordered MS-020 restart-reconciliation work is complete enough to stop infrastructure expansion. Since Stage-C acceptance is still externally blocked, the next fallback should move outward into one bounded MS-019 migration seam rather than deepen broker generality.

### Next fallback objective

Retire/delegate exactly one duplicate historical authority at an already-migrated Stage-C seam, with focused regression coverage proving the replacement owner.

Preferred first target: a remaining historical Stage-C generation/persistence owner already superseded by the final v1.0.22/v1.0.23 acceptance owner. If inspection proves that seam is already inert, choose the smallest equivalent duplicate authority in transform/selection/persistence. Preserve compatibility until replacement behavior is proven. Stop after one seam.

### v1.0.24 release chunk decision

**KEEP ACCUMULATING.**

The current v1.0.24 MS-020 seam is useful and coherent but not a sufficiently substantial next release by itself. v1.0.23 has just become the user-testable stable increment. A useful checkpoint is evidence, not a freeze.

Reconsider a v1.0.24 boundary after one additional coherent authority-retirement increment, or sooner if important target-machine fixes materially increase release value.

### User dependency

No new product decision is required. Reference-machine Stage-C verification of released v1.0.23 remains the primary external dependency and immediately preempts fallback work when evidence arrives.
