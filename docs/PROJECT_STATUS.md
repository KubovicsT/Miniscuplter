# Miniscuplter Project Status

> Fast-moving project dashboard. Inspect actual Git/release/CI state before trusting this file.

Last reconciled: 2026-09-11

## Current release / development state

- **Latest published stable release:** `v1.0.25`.
- **Stable release target:** `a7fc4bcf5f771c18060e5aee7c98026131731c2a`.
- **Current writable development branch:** `v1.0.26`.
- **Current fully validated implementation checkpoint:** `d4aef5d55170ef45298e9db942cb250d78e911d2`.
- **Overall completion:** **57% acceptance-weighted**.

v1.0.25 is published and immutable. All new development belongs on v1.0.26 unless Coordinator establishes a newer forward branch or freezes v1.0.26.

## v1.0.25 release chunk

v1.0.25 published the complete bounded MS-019 transform-command authority set:

- mapped Move nudges derive position from durable Core project state;
- mapped Rotate Y nudges derive rotation from durable Core project state;
- mapped Scale nudges derive scale from durable Core project state;
- each operation commits through transactional Stage-C state and projects the committed result back to Godot;
- failures restore presentation from durable state;
- focused regression guards prevent these mapped commands from returning to generic scene-observed persistence.

Published immutable release boundary: `a7fc4bcf5f771c18060e5aee7c98026131731c2a`.

## v1.0.26 progress

The v1.0.26 forward branch is now fully bootstrapped with consistent `1.0.26` identity across audited launcher/updater/editor/backend/installer/Windows metadata/release-audit surfaces.

The Coordinator-authorized bounded MS-019 viewport-drag transform authority seam is also implemented and validated:

- Godot remains the live viewport input/presentation owner during Move/Rotate/Scale gestures;
- gesture start captures the mapped object's durable Core transform and active mesh revision plus the presentation start transform;
- release persists only the gesture delta/scale ratio onto the captured durable Core transform rather than copying the already-mutated scene transform into Core;
- a stale mesh revision or changed durable transform rejects the commit;
- failures restore Godot presentation from current durable Core state;
- focused regression coverage prevents viewport drag release from returning to generic scene-observed transform persistence.

Validated checkpoint: `d4aef5d55170ef45298e9db942cb250d78e911d2`.

Exact-head validation is green: Core foundation, C# editor/launcher/updater/Core tests, semantic-version identity, Python/runtime, execution/job regressions, geometry regressions, strict release audit, portable package/layout, ZIP SHA-256 sidecar and installer-definition compilation. Branch release/publication jobs were correctly skipped.

## Critical path

**MS-029 updater/launcher reliability is now P0.** User/reference-machine testing after an application update reproduced the updated launcher opening briefly and then closing. Repository inspection identifies the direct cause: the updater's successful health-probe path kills the launched updated launcher in `VerifyLauncherStartup` and does not perform a normal post-commit restart. This preempts the planned MS-019 ground-placement fallback until fixed and validated.

After MS-029, MS-009 is the next development blocker: released v1.0.25 still darkens the 3D viewport/grid after splitter resize. The user-directed MS-027 UI tranche follows once that renderer/resize regression is fixed, while Stage-C reference-machine testing continues:

`MS-027: multiline scrollable AI console → real interactive view cube → compact viewport tools → right-panel hover-help cleanup`

Stage-C acceptance remains concurrently required on the released build:

`accepted 2D baseline → local 3D generation → candidate → Apply → save → close/reopen → same durable object/revision → Move/Rotate/Scale/sculpt → cleanup → exact STL export`

Also verify viewport resize/presentation, starter-scene removal, storage containment, provider/resource behavior and cancellation/recovery. New serious target-machine evidence immediately preempts fallback work.

## Next execution direction

The single Coordinator-authorized v1.0.26 viewport-drag authority seam is complete and fully validated, but new reference-machine evidence has preempted fallback work. Dev must fix MS-029 with a bounded launcher/updater hotfix, regression coverage, and Windows update-path validation before returning to the mapped-object ground-placement seam. After the MS-029 checkpoint, Dev must fix MS-009's resize-induced viewport darkening before the bounded MS-027 user-acceptance tranche. Ground-placement work remains deferred. Coordinator will reassess release readiness after the critical updater/viewport regressions and the defined UI tranche have exact-head validation.

## User dependency

No product/design decision is required. The post-update close is sufficiently reproduced. Manually reopen the installed launcher once; if that manual launch also exits, report it because that would be a separate startup problem. Otherwise autonomous MS-029 hotfix work can continue. Continue testing the latest published immutable release `v1.0.25` on the reference machine, especially the full Stage-C acceptance path, save/reopen persistence, mapped Move/Rotate/Scale/sculpt, viewport resizing/presentation, storage containment and cancellation/recovery.
