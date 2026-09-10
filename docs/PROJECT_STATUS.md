# Miniscuplter Project Status

> Fast-moving project dashboard. Inspect actual Git/release/CI state before trusting this file.

Last reconciled: 2026-09-10

## Current release / development state

- **Latest published stable release:** `v1.0.21`
- **Stable release target commit:** `0c8877b7ac5a04f9f3362851a9a9725f8348cdf8`
- **Current development branch:** `v1.0.22`
- **v1.0.22 branch base:** exact published v1.0.21 target
- **Overall completion:** **58% acceptance-weighted**

v1.0.21 is published and immutable. All subsequent fixes belong on v1.0.22+.

## Current phase

The bounded Stage-C foundation shipped in v1.0.20. Reference-machine testing then reopened **MS-009** because v1.0.20 rendered the viewport but the resting grid/model were dark and the appearance changed after splitter resize settled.

v1.0.21 is the narrow evidence-driven viewport remediation release. It establishes one normal resize/world ownership path, removes routine legacy size/full-repair competition, replaces legacy tab full-repair behavior with lightweight native refresh, and uses a neutral gray studio-style viewport presentation with stronger diagnostics.

MS-009 remains **FIXED - NEEDS USER VERIFICATION**, not resolved, because the failure is render-driver/reference-machine dependent.

## v1.0.21 release validation

Autonomous release run `34513308015` passed against exact candidate `0c8877b7ac5a04f9f3362851a9a9725f8348cdf8`:

- exact request/SHA and branch-freeze validation: **PASS**;
- C# editor/launcher/updater/Core builds and Core regressions: **PASS**;
- Python compilation/runtime dependency resolution: **PASS**;
- core/job regressions: **PASS**;
- real geometry regressions and v1.0.21 release audit: **PASS**;
- verified Godot 4.7.2 .NET + export templates: **PASS**;
- real Windows release build/export: **PASS**;
- versioned release outputs and ZIP hash verification: **PASS**;
- generated installer silent smoke-install: **PASS**;
- immutable target recheck and GitHub publication: **PASS**.

GitHub latest release now reports `v1.0.21` at the exact candidate with installer, ZIP and SHA sidecar assets.

## Current priorities

1. **MS-009 — P0 verification dependency:** retest released v1.0.21 on the reference PC for launch, divider drag/settle, tab switching, grid/model/gizmo readability and stability.
2. **MS-018:** resume full Stage-C target qualification after viewport acceptance.
3. **MS-013:** storage containment verification.
4. **MS-022:** qualify one intended lightweight/default 3D provider on GTX 1080 / 16 GB.
5. **MS-004:** cancellation/recovery during a real job.
6. **MS-019 / MS-020:** broader architecture remains behind acceptance unless new evidence changes dependency order.

## Immediate engineering priority

Do not speculate further on the viewport before reference-machine evidence. Keep v1.0.22 available for fixes forward. If v1.0.21 still changes appearance after resize/settle or remains dark/unreadable, reopen MS-009 as active P0 on v1.0.22 using the diagnostics/render-probe evidence. If accepted, move immediately to MS-018 Stage-C qualification under the Coordinator ordering.

## User input currently required

Reference-machine verification of released v1.0.21 is now the critical dependency. No product/design decision is required.
