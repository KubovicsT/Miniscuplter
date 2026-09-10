# Miniscuplter Decisions Log

> Durable product/architecture decisions. This prevents future sessions from reopening settled questions without new evidence. Add a new decision when a choice materially affects product scope, architecture, compatibility, storage, privacy, hardware requirements, or release process.

Last reconciled: 2026-09-10

## Decision statuses

- `ACCEPTED` — current project rule; change only with explicit rationale.
- `SUPERSEDED` — retained for history, replaced by a newer decision.
- `OPEN` — requires a decision; record options and recommendation.

---

## MD-001 — Local-first product

- **Status:** ACCEPTED
- **Decision:** The normal Miniscuplter creation workflow must run locally. Paid cloud inference is not required.
- **Rationale:** The product is specifically intended to make AI-assisted 3D creation accessible on the user's own low/medium-spec PC without recurring inference fees or cloud dependency.
- **Implications:** Provider selection, performance work, model storage, runtime repair and UX must account for local hardware limits.

## MD-002 — Miniscuplter creates models; it is not a slicer

- **Status:** ACCEPTED
- **Decision:** The app ends at a finished/validated 3D asset. Do not add support generation, slicing, G-code, printer profiles or printer-management scope as core product work.
- **Rationale:** Those functions distract from the differentiating AI-assisted model-creation goal and are served by dedicated downstream tools.

## MD-003 — Four-stage user workflow

- **Status:** ACCEPTED
- **Decision:** User-facing workflow is `2D → 3D → Rig & Pose → Cleanup & Export`.
- **Rationale:** This matches the mental model of a non-artist progressing from idea to usable asset.
- **Implications:** Historical development versions/milestones must not define visible UI organization.

## MD-004 — Reference hardware is a real acceptance target

- **Status:** ACCEPTED
- **Decision:** A Windows PC around GTX 1080 8 GB VRAM + 16 GB system RAM is a primary low/medium-spec acceptance machine.
- **Rationale:** The target user owns hardware in this class. Success on workstation-class GPUs alone does not satisfy the product mission.
- **Implications:** Long runtime is acceptable if clearly communicated, but avoid configurations that simply thrash RAM/VRAM indefinitely or fail without guidance.

## MD-005 — Major refactor with selective rewrites

- **Status:** ACCEPTED
- **Origin:** GPT-6 Astra takeover audit, 2026-09-06
- **Decision:** Replace weak foundations instead of permanently extending legacy compatibility layers. Preserve useful subsystems and migration behavior, not historical architecture for its own sake.
- **Keep by default:** Godot/.NET shell, local Python boundary, useful mesh/sculpt algorithms, verified model-download infrastructure, launcher/update compatibility.
- **Replace over time:** `Main.V*.cs` as authoritative state architecture, widget/name identity, mesh-only undo, STL-as-project-store, request-lifetime-only job handling, duplicated provider truth, version-derived UI composition.

## MD-006 — Stable identity and immutable revisions

- **Status:** ACCEPTED
- **Decision:** Project objects, images, mesh revisions, rigs, attachments, selections and AI candidates use stable IDs. Mesh/image revisions are immutable historical states with provenance.
- **Rationale:** Needed for safe undo, persistence, stale-result detection and candidate comparison.
- **Implication:** Display names are labels only, never identity.

## MD-007 — STL is export/interchange, not internal authoritative storage

- **Status:** ACCEPTED
- **Decision:** Internal project storage uses a versioned indexed/binary representation capable of preserving object/revision metadata. STL remains a final export format.
- **Rationale:** STL duplicates vertices and cannot represent the richer state needed for selections, rigging, attachments, provenance and revision-aware editing.

## MD-008 — Transactional full-state history

- **Status:** ACCEPTED
- **Decision:** Undo/redo should operate on complete project transactions containing affected IDs and before/after state. AI Apply is a normal transactional command.
- **Rationale:** Mesh-only undo cannot safely restore transforms, dependent selections, attachments, topology-bound metadata or revision relationships.

## MD-009 — Authoritative local Job Broker is the target execution model

- **Status:** ACCEPTED
- **Decision:** Long-running AI/geometry work should ultimately run through a job system with persistent IDs, immutable input revisions, structured stages, resource ownership, cancellation, isolated outputs and stale-result protection.
- **Rationale:** Multi-minute local GPU jobs cannot safely be modeled as disposable HTTP button callbacks.
- **Interim behavior:** Existing progress polling/backend reset mechanisms are bridges, not final architecture.

## MD-010 — Never show fake inference percentages

- **Status:** ACCEPTED
- **Decision:** Show real step/percentage only when a provider exposes trustworthy progress. Otherwise show stages, activity and elapsed time and explicitly avoid claiming exact completion percentage.
- **Rationale:** Misleading progress is worse than an honest unknown duration.

## MD-011 — Provider readiness has multiple states

- **Status:** ACCEPTED
- **Decision:** `downloaded`, `installed`, `importable`, `device-tested` and `inference-tested` are distinct states. A provider registry should own these facts plus hardware/platform/license/benchmark metadata.
- **Rationale:** Historical failures showed that an installed model can still fail immediately from missing dependencies, CUDA/platform incompatibility or native build requirements.

## MD-012 — Prefer a qualified default bundle over maximum provider count

- **Status:** ACCEPTED
- **Decision:** Optional/experimental providers may remain available, but the normal user experience should route among a smaller set proven to work reliably on supported Windows hardware.
- **Rationale:** More adapters do not help if the primary workflow is unreliable.

## MD-013 — User intent should eventually dominate provider UX

- **Status:** ACCEPTED
- **Decision:** Normal users should increasingly select intent such as `Fast / Balanced / Quality` rather than understand every AI model implementation. Explicit provider selection remains available for expert/debug use.
- **Rationale:** Target users are not expected to be AI runtime experts.

## MD-014 — Miniscuplter-controlled storage is authoritative

- **Status:** ACCEPTED
- **Decision:** Generated images, masks, meshes, job artifacts, logs, caches and temp data should normally remain under the chosen Miniscuplter data root. Default packaged behavior should avoid silent `%APPDATA%` / `%TEMP%` / C: leakage.
- **Rationale:** User has limited C: space and explicitly wants the project/runtime data together.
- **Compatibility:** A deliberately user-configured alternate DataRoot may remain supported; once configured, it becomes authoritative.

## MD-015 — Preserve expensive/user data across application updates

- **Status:** ACCEPTED
- **Decision:** Normal app updates preserve projects, AI models, staged/partial downloads, `.venv`/provider environments, runtime caches, parts library, exports, settings and user data unless an explicit migration safely replaces them.
- **Rationale:** AI payloads are large and user work must never be disposable application content.

## MD-016 — Published releases are immutable

- **Status:** ACCEPTED
- **Decision:** Never modify the code/assets of an already published version to add fixes. New application changes use a new semantic version branch/release.
- **Normal flow:** `latest stable → next version branch → implement → validate → full Windows export → package/hash verification → installer smoke test → publish → user test`.

## MD-017 — Real user/GPU verification is distinct from CI

- **Status:** ACCEPTED
- **Decision:** CI/static/build success can prove compilation, packaging and deterministic regressions, but not all Godot render-driver or CUDA/model behavior. User-observed GUI/GPU issues remain `FIXED - NEEDS USER VERIFICATION` until appropriately tested.

## MD-018 — Migration before legacy removal

- **Status:** ACCEPTED
- **Decision:** The old implementation may be heavily refactored, but do not delete compatibility paths or old project readability until replacement migration/acceptance coverage exists.
- **Rationale:** Architectural cleanup must not destroy user projects or the functioning update path.

## MD-019 — Non-artist UX takes precedence over exposing implementation complexity

- **Status:** ACCEPTED
- **Decision:** Prefer guided workflows, visual candidate review, clear actions and safe defaults. Advanced/debug controls should exist where useful but not dominate the primary interface.

## MD-020 — AI regional improvement should support promptless Enhance

- **Status:** ACCEPTED
- **Decision:** In 2D regional editing, the user may select an obviously incorrect area and choose **Enhance** without specifying the exact repair. The system uses surrounding image/subject/style context to infer a coherent correction while preserving non-selected regions.
- **Rationale:** The target user may recognize that something is wrong without knowing how to describe the technically correct replacement.

## MD-021 — Completion is acceptance-based, not feature-count-based

- **Status:** ACCEPTED
- **Decision:** Project completion percentages are weighted against end-to-end product workstreams and acceptance criteria. Code presence alone earns partial credit when integration/reliability is unproven.

---

# Open decisions requiring user input

There is currently **no fundamental product decision blocking autonomous engineering**.

When a future decision qualifies for user input, add an `OPEN` entry here only if it materially changes one of the following:

- product scope;
- primary UX direction with meaningful tradeoffs;
- user-data safety;
- intentional backward compatibility break;
- credentials/payment/external service dependency;
- minimum hardware requirements;
- local-first/privacy assumptions;
- a difficult-to-reverse product choice.

Normal implementation choices should be made independently by the senior engineering agent, documented in commits/issues as appropriate, and not escalated simply because multiple coding approaches exist.
