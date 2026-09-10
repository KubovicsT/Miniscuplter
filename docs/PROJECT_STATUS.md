# Miniscuplter Project Status

> Fast-moving project dashboard. Inspect actual Git/release/CI state before trusting this file.

Last reconciled: 2026-09-10

## Current release / development state

- **Latest published stable release:** `v1.0.20`
- **Stable release target commit:** `e63cb0601cdbb91af5be191458ecdbcb7c0b7944`
- **Current development branch:** `v1.0.21`
- **v1.0.21 branch base:** exact published v1.0.20 target `e63cb0601cdbb91af5be191458ecdbcb7c0b7944`
- **Overall completion:** **58% acceptance-weighted**

v1.0.20 is published and immutable. All further application/documentation work belongs on v1.0.21 or later.

## Current phase

The Coordinator-defined bounded Stage-C foundation slice shipped in v1.0.20:

`accepted 2D baseline → revision-bound 3D candidate → explicit Apply → Core-authoritative transform → one immutable sculpt/edit commit → save/reload → revision-bound cleanup → exact validated STL export`

The remaining Stage-C acceptance work is real reference-machine qualification, not additional speculative architecture.

## v1.0.20 release validation

Autonomous-release run `34508060099` completed successfully for exact candidate `e63cb0601cdbb91af5be191458ecdbcb7c0b7944`.

Passed gates:

- exact semantic-version request and branch/SHA binding;
- C# restore/build and Core tests;
- Python compilation and dependency resolution;
- core/job regressions;
- geometry regressions and release audit;
- verified Godot 4.7.2 .NET/runtime/template download;
- real Windows Godot export;
- versioned release output and SHA verification;
- installer creation;
- silent installer smoke-install;
- immutable target recheck immediately before publication;
- tag creation and GitHub Release publication.

Published assets include the Windows installer, portable ZIP and ZIP SHA-256 file.

## Current priorities

1. **MS-018:** run the released v1.0.20 Stage-C thin slice on the reference PC and record pass/fail evidence.
2. **MS-009:** verify viewport/grid/model/gizmo visibility and interaction; any reproduced blank viewport becomes immediate P0 on v1.0.21.
3. **MS-013:** verify storage containment during representative workflows.
4. **MS-022:** qualify one intended lightweight/default 3D provider on GTX 1080 / 16 GB with elapsed-time and practical RAM/VRAM evidence.
5. **MS-004:** exercise cancellation/recovery where practical during the same target-machine session.
6. **MS-019 / MS-020:** broader architecture work remains sequenced after target-machine evidence unless a concrete acceptance blocker requires immediate work.

## Immediate engineering priority

Do not invent target-machine success from CI. v1.0.20 is the immutable test build and v1.0.21 is the forward fix branch.

The next Dev Cycle should first inspect any new user/reference-machine evidence. If a reproducible v1.0.20 defect exists, document/reuse its MS issue and fix forward on v1.0.21. If no target evidence is yet available, avoid broadening the Coordinator roadmap and keep the repository ready for that acceptance pass.

## User input currently required

Reference-machine testing of released v1.0.20 is now the important dependency. No product/design decision is required unless testing exposes a difficult-to-reverse tradeoff.
