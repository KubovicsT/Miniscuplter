# Miniscuplter Handoff

> Operational baton for the next development run. Inspect actual Git/release/CI state first; repository state wins over this document if they differ.

Last updated: 2026-09-10

## Current state

- **Repository:** `KubovicsT/Miniscuplter`
- **Latest published stable:** `v1.0.19`
- **Stable release application commit:** `52f3b95fb6addc0f9f1e7123b75068da4ef1513c`
- **Current development branch:** `v1.0.20`
- **v1.0.20 base:** exact released v1.0.19 commit above
- **Latest application/code commit:** `a9f7e0989d5486bdee27f054edb58713aa00cc41`
- **Overall completion estimate:** 56% acceptance-weighted.

The mandatory release/version reconciliation was clean at the start of this run: v1.0.19 remains the immutable published release and v1.0.20 remains a distinct forward-only development branch. Documentation commits after the application commit advance branch HEAD; resolve the exact HEAD and latest release/CI state before editing.

## What this run accomplished

### MS-018 / MS-020 — end-to-end Stage-C transport identity

The production Stage-C editor bridge already created a `GenerationJobBinding`, but the prior HTTP request still relied mainly on image/output paths. This run carried the binding identity through the existing AIClient/backend path without bypassing its request semaphore, cancellation ownership, or backend-reset recovery.

Changes:

- `ai_backend/job_progress.py`
  - `begin()` now accepts optional job context and stores a deep copy with the job.
  - snapshots deep-copy the context so callers cannot mutate the authoritative in-memory record.
  - idle progress exposes an empty context consistently.
- `ai_backend/app.py`
  - `Generate3DRequest` accepts optional `generation_job_id`, `project_id`, `project_revision`, `input_image_revision_id`, and `output_object_id`.
  - Stage-C identity is all-or-nothing; partial identity is rejected.
  - a Stage-C `generation_job_id` must agree with the `X-Miniscupter-Job-Id` transport header.
  - the Core GenerationJobId becomes the backend job ID for Stage-C generation.
  - the complete context is retained in backend job progress and echoed in the verified `/generate-3d` response.
  - legacy requests with no Stage-C context remain compatible.
- `Scripts/AIClient.cs`
  - added typed `AiStageCGenerationContext` and `AiStageC3DResult`.
  - `Generate3DStageCAsync()` sends the complete context through the existing serialized/cancellable request path.
  - `PostJsonTextAsync()` can use an explicitly supplied stable job ID instead of generating a transport-only ID.
  - the client verifies that the backend echoed exactly the expected project/job/input/output identity and rejects a missing or mismatched context before returning the result to the editor.
- `Scripts/Main.V1020StageCBridge.cs`
  - production 3D submission now converts the Core `GenerationJobBinding` to the typed AIClient context and calls `Generate3DStageCAsync()`.
  - candidate registration occurs only after transport identity verification and STL validation.
  - the actual provider comes from the verified response; the old secondary progress lookup is no longer needed for provider identity.
  - pending candidate fields are assigned only after ProjectStore save succeeds, reducing false UI state after save failure.
- `tools/job_progress_tests.py`
  - verifies Stage-C context retention, source-context isolation, snapshot isolation, and equality between stable generation job ID and stored backend job ID.

Relevant commits:

- `ebfa4fce7691e71cbfaffc16832e171dd83bd839` — Carry Stage C context in job progress
- `1429d9bc4c45db2cbca0a9b99b3bf2bb6f93d674` — Propagate Stage C identity through 3D backend jobs
- `ba99de7e59303c33805565df5159d2fa83a89bc1` — Verify Stage C identity in AIClient
- `4885f80d825213d66af957397a03045a80432328` — Bind Stage C editor generation to transport identity
- `a9f7e0989d5486bdee27f054edb58713aa00cc41` — Test Stage C job context retention

Durable architecture rule added as `MD-022`: Stage-C generation identity is end-to-end and fail-closed. Never register a result whose transport identity is absent or mismatched.

## Validation state

For application HEAD `a9f7e0989d5486bdee27f054edb58713aa00cc41`:

- `core-foundation` run `34493357060`: **SUCCESS**.
- broader build run `34493356971` had already passed Python compilation/dependency resolution, core logic tests, job-progress tests, real geometry regressions, release audit, and the editor/launcher/updater/Core C# build when this handoff was written.
- portable package build/layout/SHA verification also passed; installer-definition packaging was still finishing at the last check. Reconcile the final conclusion at the start of the next run.

No real CUDA inference or target-machine GUI acceptance occurred in CI. No v1.0.20 release was made.

## Current unresolved priorities

1. **MS-009 — viewport/grid/model/gizmo:** v1.0.19 is published but still needs target-PC verification. A reported blank viewport immediately outranks planned work.
2. **MS-018 — Stage-C thin slice:** baseline→qualified generation→identity-verified candidate→explicit apply→restart visibility is materially integrated; cleanup/export and full target-machine qualification remain.
3. **MS-013 — storage containment:** v1.0.19 hardening still needs representative target-machine verification.
4. **MS-020 — final Job Broker:** Stage-C request/result identity is now carried through production transport, but durable queue/resource ownership/crash recovery and save-failure consistency are still incomplete.
5. **MS-022 — provider qualification:** readiness and real-success recording exist; a GTX 1080 lightweight/default provider still needs real inference evidence.
6. **MS-019 — legacy `Main.V*.cs`:** continue migrating only the current vertical slice before broadening.

## Exact next task

Continue the same Stage-C vertical slice:

1. Reconcile exact v1.0.20 HEAD, latest release, and the final result of build `34493356971` plus newer documentation-only CI.
2. Harden `V1020SaveSessionAsync()` so a failed ProjectStore save cannot leave the in-memory compatibility session ahead of durable state. Preferred behavior: after save failure, load the last recoverable ProjectStore state and `ReplaceFromLoad()` (or equivalently restore the pre-save state) before surfacing the failure. Orphaned immutable asset files are acceptable; falsely committed project state is not.
3. Add deterministic coverage for save-failure recovery. Also add a transport mismatch regression at the lowest-cost layer that proves a wrong echoed ProjectId/ImageRevisionId/ObjectId cannot proceed to candidate registration without needing real GPU inference.
4. Continue the persisted Stage-C object through the minimum cleanup path needed for MS-018, ensuring cleanup creates a new immutable mesh revision and changes active object state transactionally rather than overwriting the generated revision.
5. Bind Cleanup & Export to an explicit project object/revision scope and validated STL export. STL remains interchange/output, not project authority.
6. Keep cancellation/resource recovery on the existing AIClient/backend ownership path. Do not create parallel request mechanisms.
7. When real target hardware is available, collect v1.0.19 viewport/storage acceptance plus one lightweight/default 3D provider inference qualification on GTX 1080/16 GB.

## Release policy

Do not modify v1.0.19. Do not publish v1.0.20 yet: the full Stage-C cleanup/export path and target-machine acceptance remain incomplete. Before publication require all normal C#/Python/Core/job/geometry/audit gates, a real Godot Windows export, artifact/hash verification and installer smoke test. After publication verify GitHub latest-release state and create/use v1.0.21 before further application development.

## User input

No product/design decision currently blocks autonomous engineering. Real-machine verification is useful when available but independent engineering can continue without it.
