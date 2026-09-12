# Miniscuplter Handoff Diagnostic

## Release / branch state
- Stable: v1.0.27.
- Writable: v1.0.28; no release freeze.
- Current bootstrap has two stale 1.0.27 identity surfaces.

## CURRENT — complete v1.0.28 bootstrap
Outcome: update the two remaining identity surfaces to 1.0.28 and restore exact-head green CI.
Auto-proceed: YES.

## NEXT — P0 Generate 3D runtime ownership
Outcome: diagnose and fix the heavyweight-runtime ownership conflict recorded by Coordinator evidence.
Auto-proceed: YES.

## NEXT — P0 accepted-baseline persistence
Outcome: preserve and restore accepted-baseline state across project reopen; track as MS-031.
Auto-proceed: YES.

## NEXT — MS-009 viewport grid
Outcome: restore the visible non-occluding viewport grid.
Auto-proceed: YES.

## NEXT — workspace corrections
Outcome: apply the already specified console, telemetry and orientation-gizmo corrections.
Auto-proceed: YES.

## NEXT — integration checkpoint
Run integrated validation and stop for Coordinator review.
Auto-proceed: NO.

## Preemption
Any new update/backend/generation/persistence/storage/cancellation/data-safety failure outranks lower work.

Diagnostic update marker: existing-file replacement path.
