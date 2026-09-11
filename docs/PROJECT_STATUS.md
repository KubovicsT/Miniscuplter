# Miniscuplter Project Status

> Fast-moving project dashboard. Inspect actual Git/release/CI state before trusting this file.

Last reconciled: 2026-09-11

## Current release / development state

- **Latest published stable release:** `v1.0.25`.
- **Stable release target:** `a7fc4bcf5f771c18060e5aee7c98026131731c2a`.
- **Current writable development branch:** `v1.0.26`.
- **Latest CI-green implementation checkpoint:** `1e6ed3541de6de4bec19838e71595e4b76dbedfa` (not release-worthy yet because MS-030's server-start contract remains incomplete).
- **Current planning HEAD:** `674255595923c7c89cec3dac52080484c2fb25ed`.
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

**MS-030 remains P0 and is not yet fixed.** Dev removed interpreter divergence and added Repair health validation, but end-to-end review found both Repair and editor still launch `python app.py`. The module defines FastAPI `app` but has no executable server entry point, so this command does not start Uvicorn. Repair's fixed-port health probe also needs protection against validating an unrelated pre-existing backend.

After the canonical backend-start contract is fixed, the authorized order is:

`MS-009 resize/render authority → MS-027 compact workspace tranche → integrated v1.0.26 release-candidate hardening`

**MS-029 code is fixed and CI-green.** Future updates can preserve the healthy launcher. Because v1.0.25 is immutable, its old updater may still close the launcher once while installing v1.0.26; a manual reopen is an accepted one-transition compatibility limitation rather than justification for risky retrofit machinery.

Stage-C acceptance remains the overarching milestone and resumes on the reference machine after a fixed release is published.

## Next execution direction

AMP-005 rolling execution is active. HANDOFF contains four substantial objectives with explicit dependencies, acceptance/preemption conditions and auto-proceed permission. Dev should continue across them without waiting between completed checkpoints. Ground-placement/MS-019 and unrelated feature breadth are deliberately deferred until the integrated release candidate reaches Coordinator review.

## User dependency

No product/design decision is required. The backend-health, resize and UI failures are sufficiently reproduced; do not keep retrying those paths on v1.0.25. Manually reopen the installed launcher once; if that manual launch also exits, report it because that would be a separate startup problem. Autonomous MS-030 diagnosis/fix work can continue; no further user action is required until a new build is ready. Continue testing the latest published immutable release `v1.0.25` on the reference machine, especially the full Stage-C acceptance path, save/reopen persistence, mapped Move/Rotate/Scale/sculpt, viewport resizing/presentation, storage containment and cancellation/recovery.
