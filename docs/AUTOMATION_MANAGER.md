# Miniscuplter Automation Manager

> Current process/automation state. Product truth remains in PROJECT_CHARTER and the current-state documents; historical process evidence remains in GitHub issues and Git history.

Last manager migration: 2026-09-17 Europe/Budapest

## Production role model

- User / PROJECT_CHARTER owns product intent, hard constraints, and difficult-to-reverse decisions.
- Coordinator is technical director, architecture/strategy owner, normal integrator, and exclusive release owner. It delegates routine implementation and performs little routine coding.
- LocalDev is the primary routine implementation worker for bounded, explicit, reversible tasks. It writes only isolated `localdev/*` branches, never merges, and never releases.
- Dev Work is the specialist GPT implementation worker for hard work LocalDev cannot safely or reliably complete. It never publishes releases.
- Automation Manager owns scheduler/process/control-plane reliability, LocalDev health, prompt governance, and safe reversible process tuning. It does not own product architecture or routine implementation.
- Daily Report remains reporting-only and stays paused under the existing user direction.

## Fixed LocalDev control plane

Exactly two long-lived issues are used in `KubovicsT/Miniscuplter`:

- queue #93 — `LOCALDEV WORK QUEUE — Coordinator → LocalDev`;
- journal #92 — `LOCALDEV WORK JOURNAL — execution & results`.

Coordinator appends stable-ID task batches to queue comments. LocalDev appends claims, results, timings, validation, branches, commits, and escalations to journal comments. Production never creates one issue per task.

The production worker is `KubovicsT/LocalDev` commit `78ddff17caa3c7a46da110a32113d8a89dba2523` or newer. Start it with `run-production.bat`. Production reads only #93/#92, polls every 180 seconds while idle, preserves the no-progress watchdog, enforces allowed-file scope, pushes isolated branches, and never merges or releases.

Benchmark #91 is archived. Benchmark launchers require explicit benchmark mode and production rejects benchmark label/issue-number arguments.

## Live automations

- Coordinator Work — prompt revision `.032`; enabled; exact schedule 01:40, 03:40, 05:40, 07:40, 09:40, 11:40, 13:40, 15:40, 17:40, 19:40, 21:40, 23:40 Europe/Budapest.
- Dev Work — prompt revision `.029`; hourly at `:00`; normally disabled. Coordinator enables it only for an explicit hard-work batch in issue #23; Dev self-disables when the batch is exhausted or only Coordinator-owned work remains.
- Manager Work — prompt revision `.033`; enabled; exact schedule 06:40 and 18:40 Europe/Budapest.
- Daily Report — unchanged and disabled under the existing reporting pause.

There is no Coordinator/Dev mutual-exclusion baton. Coordinator remains enabled while Dev may also be enabled for a hard batch. Enabled status is not proof of active execution. Before shared-branch mutation, Coordinator and Dev check actual conflicting activity. LocalDev may run concurrently because its branches are isolated.

## Coordinator operating cycle

Each two-hour run prioritizes:

1. review new LocalDev diffs and evidence;
2. triage escalations;
3. integrate accepted work into the authorized writable branch;
4. requeue bounded corrections or split unclear tasks;
5. route environment/tooling failures to Manager and hard reasoning/architecture work to Dev;
6. recalculate queue coverage;
7. add enough safe work for roughly 2.5–3 hours when available;
8. perform release-readiness/release work as appropriate.

A LocalDev `COMPLETE` is not acceptance. Coordinator inspects the actual diff, acceptance criteria, validation, scope, semantics, and current writable state.

## Manager inspection checklist

Every 12-hour cycle inspects:

- worker/watchdog health and production version;
- ready depth, estimated coverage, and queue-empty idle time;
- throughput and median task duration;
- escalation, repair, semantic rejection, and stale/conflict rates;
- Coordinator review backlog and age of complete-but-not-integrated branches;
- Dev enablement, batch quality, run length, and hard-task throughput;
- Coordinator review/decomposition/replenishment effectiveness;
- schedule collisions and actual shared-branch mutation risk;
- queue/journal parse, append, retry, and dedup health;
- CI/build reliability and prompt drift;
- benchmark leakage;
- tasks that are too coarse or artificially microscopic;
- model/hardware fit.

Manager may make safe reversible prompt/process/config/documentation corrections consistent with this architecture. It reports meaningful changes, repeated failure patterns, sustained under-supply/review backlog, recommended model/hardware changes, delegation-boundary concerns, and unresolved control-plane risk.

## Current migration state

- Control-plane smoke task `LD-20260917-0000` successfully completed through queue comment, claim, and terminal journal result without product-code mutation.
- Initial production fill contains 28 real bounded tasks, `LD-20260917-0001` through `LD-20260917-0028`.
- This is below the numerical initialization target of 50 because completed benchmark branches were not duplicated and the known candidate-history presentation router remains too broad for an unsafe LocalDev tail slice. The initial tasks are medium-sized and estimated to cover roughly 2.5–3 hours; actual throughput evidence must replace this estimate.
