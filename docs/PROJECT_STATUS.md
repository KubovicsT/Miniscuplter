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

Stage-C reference-machine acceptance remains P0:

`accepted 2D baseline → local 3D generation → candidate → Apply → save → close/reopen → same durable object/revision → Move/Rotate/Scale/sculpt → cleanup → exact STL export`

Also verify viewport resize/presentation, starter-scene removal, storage containment, provider/resource behavior and cancellation/recovery. New serious target-machine evidence immediately preempts fallback work.

## Next execution direction

The single Coordinator-authorized v1.0.26 viewport-drag authority seam is complete and fully validated. Do not invent a second MS-019 seam or broaden MS-020/MS-027 from this status alone. Consume new target-machine evidence first; otherwise follow the next Coordinator sequencing recorded in HANDOFF/ROADMAP after repository bootstrap.

## User dependency

No product/design decision is required. Test the latest published immutable release `v1.0.25` on the reference machine, especially the full Stage-C acceptance path, save/reopen persistence, mapped Move/Rotate/Scale/sculpt, viewport resizing/presentation, storage containment and cancellation/recovery.
