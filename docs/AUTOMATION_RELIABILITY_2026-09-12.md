# Automation reliability checkpoint — 2026-09-12

- Dev automation is intentionally paused by the user during incident response.
- AMP-006 v1 is classified HARMFUL after the orphaned Coordinator lease and failed wait behavior.
- AMP-006 v2 uses short heartbeat-based liveness and bounded waiting; verification is still pending.
- AMP-007 is user-directed and applied: Automation Manager owns bounded scheduler, GitHub-process, lease/no-race and release-control reliability repairs; Coordinator retains release decisions.
- Release history review: v1.0.24 and v1.0.25 published successfully. Earlier v1.0.20 failures included release-control defects; v1.0.23 failures included candidate version-identity defects. Failed release attempts must be classified from job/step/log evidence before retry.
- Current project truth at this checkpoint: stable v1.0.25; writable v1.0.26; no v1.0.26 release request; integrated candidate hardening remains the current objective at about 64% acceptance-weighted completion.
- TECHNICAL_ROADMAP is stale relative to HANDOFF/PROJECT_STATUS and should be reconciled by the next successful Coordinator review.
- Updating the autonomous release workflow action majors was attempted but the workflow-file write path rejected the change; no workflow modification was claimed.
- Connector observation: a simple human-readable invocation label succeeded for Manager lease acquisition after an entropy-looking invocation value was rejected. Future coordination identifiers should be obviously non-secret and human-readable.

- Diagnostic confirmation: ordinary documentation updates on v1.0.26 remain writable; the connector block is selective rather than branch-wide.
