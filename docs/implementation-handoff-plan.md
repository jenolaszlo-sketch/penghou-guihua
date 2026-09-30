# Implementation and model handoff plan

Date: 2026-09-28. Status: **planned; no implementation completed by this plan**.
Inputs: [review](architecture-boundary-review-2026-09-28.md),
[roadmap](../ROADMAP.md), [architecture](architecture.md).
Companion: [Qingniao plan](../../Penghou.Qingniao/docs/implementation-handoff-plan.md)
(sibling checkout).

## Objective and scope

Harden the existing preview before extending adaptive planning. Deliver
deterministic admission, enforceable planning bounds, recoverable transitions,
and useful explanations without moving execution authority into Guihua.

Preserve completed roadmap work: preview.2 extraction/integration, API analyzers,
coverage thresholds and the kernel sample. Do not rebuild these deliverables.
Treat the existing Guyabano integration as a compatibility constraint; the
roadmap records its status, but this planning pass did not inspect that consumer
or verify new upstream releases.

Guihua proposes workflow changes; Fuwen represents, compares and admits them;
Zhinu authorizes durable execution transitions; Qingniao supervises a selected
delegation; the host owns domain criteria, authorization and provider selection.
No core dependency on Qingniao, no new evidence database, no autonomous workflow
activation, and no shared generic engine extracted from the two repositories.

All task IDs below are initially **not started**. “G01” etc. identify findings
in the review; “GH-01” etc. identify implementation packages in this plan.

## Roadmap reconciliation

| Existing roadmap item | Planned delivery | Completion boundary |
| --- | --- | --- |
| Current preview / Gap C engineering health | GH-00, GH-10 | Preserve analyzers and coverage; verify migration and both supported runtimes |
| Gap A.1 external proposal admission | GH-03 | Public provider-neutral bounded admission receipt, independent of Baize |
| Gap A.2 transition preview | GH-08 | Bounded comparison with pinned bases; no activation side effects |
| Gap A.3 activation receipt | GH-06 | Requires verified Fuwen/Zhinu transition contracts; a fork alone is insufficient |
| Gap A.4 reuse/invalidation explanation | GH-07, GH-08 | Distinguish predicted impact from actual execution-authority receipts |
| Gap B Fuwen interoperability | GH-00, GH-03, GH-06, GH-10 | Pin supported releases, record breaking changes and publish order |
| Review correctness gaps | GH-01 through GH-07 | Targeted regressions plus applicable existing suites |
| Second consumer / Marang | GH-08, GH-09 | Host integration follows upstream gates; MCP/auth remain in Marang |
| V2.1 / V2.2 / V2.3 | Deferred after this plan's V1 gates | No avoidance store, experiments, historical preference learning or automatic strategy selection |

Adding typed current-run facts in GH-07 does not implement V2 historical
evidence-informed planning. Likewise, an admission receipt is not authorization
to activate a revision.

## Ordering and assignment

```text
GH-00 baseline
  -> GH-01 decision/stage admission
  -> GH-02 call accounting
  -> GH-03 revision admission
  -> GH-05 checkpoint identity/history
  -> GH-07 typed execution observations
  -> GH-06 recoverable execution transitions -> GH-09 composition/refactor
GH-00 -> GH-04 catalog head -----------------> GH-05
GH-03 + verified Fuwen comparison ----------> GH-08 preview -> GH-09
GH-01..GH-09 delivered or explicitly gated --> GH-10 release verification
```

GH-04 may run independently of GH-01/GH-02 after GH-00. GH-08 can begin once
GH-03 and the Fuwen comparison contract are available; it need not wait for
activation. GH-06 requires GH-03/GH-05/GH-07 and the upstream gate below.

Serialize changes to `PlanningLoop.cs`, `PlanningLoopContracts.cs`,
`PlanningLoopSeams.cs` and public API baselines. “Independent” means a separate
bounded assignment/checkout; it does not authorize concurrent edits to shared
files. Assign one package per model and merge prerequisites before dependents.

## Task packages

### GH-00 — Capture the baseline and verify dependency assumptions

**Depends on:** none. **Maps to:** Gap B/C and all handoffs.
**Read/change:** roadmap, project files, CI, API baselines, changelog, this plan.

- Record actual HEAD, dirty files, SDK, dependency versions and test commands.
  Preserve pre-existing roadmap/V2 edits and review documents.
- Run the current baseline; record any pre-existing failure before changes.
- Inspect the available released Fuwen comparison/admission and Zhinu transition
  APIs just before integration. Record exact package/API evidence, not merely
  an upstream roadmap checkbox or local unpublished source.
- Create a short dependency gate record in this document or a linked ADR:
  capability, owning repo, inspected version, evidence/test, available/missing,
  required downstream task, and next action.

**Done when:** a successor knows which tasks can start and which are blocked.
No upstream upgrade or feature reimplementation is required just to close this
baseline package.

### GH-01 — Admit complete decisions before effects

**Depends on:** GH-00. **Maps to:** G02, G06; current kernel correctness.
**Primary files:** `PlanningLoop.cs`, `PlanningStageValidator.cs`,
`PlanningStageRunner.cs`, corresponding core tests.

- Move Finish/stage incompatibility and applicable pre-execution checks into
  deterministic decision admission.
- Stop duplicate/invalid stage identities before topological dictionaries are
  built. Introduce one checked artifact-pin parser rather than repeated slicing.
- Keep malformed proposals in the bounded diagnostic/repair path.

**Required tests:** rejected Finish+ProduceStages makes zero executor calls and
publishes nothing; duplicate stage names return diagnostics; malformed/empty
revision pins never throw slicing or parsing errors; valid plans still execute.

**Done when:** regressions prove absence of effects, not only a failure status.

### GH-02 — Enforce one run-scoped model-call allowance

**Depends on:** GH-01. **Maps to:** G01; bounded loop guarantee.
**Primary files:** loop policy/contracts, loop, stage result seam, Baize proposer/
author/decider implementations and tests.

- Reserve allowance before each actual model call, including malformed-output
  repair, stale re-observation, author/proposer retries and stage work.
- Define failure/interruption accounting and unknown usage explicitly.
  Pass bounded allowances to adapters; a post-hoc `ModelCalls` count alone
  cannot enforce a hard limit.
- Separate hard invocation limits from best-effort reported token usage.
  Persist accounting intent or conservative spent allowance where replay could
  otherwise restore it; GH-05/GH-06 complete recovery integration.
- Record migration for custom implementations of changed interfaces.

**Required tests:** a one-call budget cannot perform a second repair call;
nested adapter attempts consume the same allowance; failed/null responses count;
unknown tokens remain unknown; zero remaining allowance launches no work.

**Done when:** every actual invocation route has an enforcement point. If a
custom stage executor cannot report/enforce its allocation, expose that limitation
rather than claiming a hard bound.

### GH-03 — Make revision admission a kernel responsibility

**Depends on:** GH-02. **Maps to:** G04; Gap A.1/B.
**Primary files:** `WorkflowPatchApplier.cs`, `PatchPreservationValidator.cs`,
loop seams, Baize author/proposer, new cohesive admission types and tests.

- Accept externally supplied patches against exact design/evidence bases without
  requiring Baize. Bound payloads and diagnostics; return a typed receipt.
- Apply the patch in the kernel; validate the actual authored plan/DSL against
  scope and preservation invariants. Do not trust collaborator success flags
  or a supplied `Applied` object as admission.
- Pin catalogue/admission-policy/definition identities in an immutable admitted
  revision value. Reuse Fuwen's authoritative semantics instead of copying them.
- Test nested conditional/repeat/fan-out preservation and prompt references.
  Explicitly reject unsupported shapes if current APIs cannot prove preservation.
- Snapshot admitted collections; preserve existing fingerprint version semantics.

**Required tests:** stale patch, unrelated applied design, inconsistent DSL/plan,
unauthorized untouched-node or nested-prompt change cannot reach execution;
valid external proposal receives reproducible bounded admission evidence.

**Done when:** swapping a proposer/author implementation cannot bypass mandatory
checks. New public contracts and migration notes are intentional and reviewed.

### GH-04 — Replace timestamp-selected catalog heads

**Depends on:** GH-00; independent storage lane. **Maps to:** G07.
**Primary files:** artifact repository/catalog interfaces, filesystem implementation,
catalog/index types and storage tests.

- Use a monotonic generation and explicit head update independent of wall time.
  Specify atomic replacement/CAS semantics and the supported writer model.
- Keep immutable revisions while avoiding scans of every historical full index
  to find the head. Avoid designing a new general database.
- Define legacy-store migration/recovery, partial writes, corruption and stale
  head behavior. Do not silently reinterpret content hashes.
- Check physical path containment and storage identity handling while touching
  filesystem operations; avoid weakening existing integrity validation.

**Required tests:** restart with clock rollback; two competing writers or explicit
single-writer refusal; interrupted payload/head write; old-store read/migration;
hash mismatch; latest generation remains readable after each supported failure.

**Done when:** head authority and recovery are documented and testable. Use a
focused operation-count/scaling test if needed, not an unsolicited benchmark
project (roadmap Gap C defers benchmarks).

### GH-05 — Bind and restore complete planner state

**Depends on:** GH-02/GH-03/GH-04. **Maps to:** G08 and part of G03.
**Primary files:** `PlanningCheckpoint.cs`, restore/save logic, identity tests.

- Bind caller/workflow request, normalized goal/input digest, admitted definition,
  catalogue, policy version and spent allowance to a checkpoint.
- Reject semantic reuse of an ID; require explicit new lineage when intended.
- Restore decision history or reference bounded append-only history.
- Version persisted schemas; specify how old checkpoints are accepted, migrated,
  or rejected with actionable diagnostics. Never silently upgrade replay meaning.

**Required tests:** changed goal/policy/admission basis cannot replay old success;
history survives resume; missing/corrupt state fails closed; spent allowance is
not reset; supported historical checkpoints behave as documented.

**Done when:** a checkpoint reproduces the admitted planning basis rather than
merely containing DSL that compiles against today's catalogue.

### GH-07 — Preserve typed execution facts across the host seam

**Depends on:** GH-03/GH-05. **Maps to:** G05; Gap A.4 groundwork.
**Primary files:** `PlanningLoopContracts.cs`, loop observation, Zhinu snapshot
mapping and tests.

- Distinguish execution lifecycle, evaluation availability, acceptance decision
  and revision disposition. Prefer small typed values plus bounded summaries.
- Carry exact run/revision/evidence references and explicit truncation/unknowns.
- Define “planning stopped” separately from “user goal accepted”.
  Host policy supplies domain acceptance; Guihua enforces the configured gate.
- Keep the mapping provider-neutral and compatible with Qingniao evidence through
  a host adapter, without adding a Qingniao core reference.

**Required tests:** completed-but-rejected, waiting, cancelled, evaluator failure,
missing evidence and no-op convergence remain distinguishable; text saying
“passed” cannot override a deterministic failure.

**Done when:** execution and planning consumers no longer need to parse status
from prose. Historical recall/ranking remains deferred V2.

### GH-06 — Reconcile accepted transitions instead of repeating them

**Depends on:** GH-03/GH-05/GH-07 plus verified upstream transition authority.
**Maps to:** G03; Gap A.3/B.
**Primary files:** execution-host seam, `ZhinuPlanningExecutionHost.cs`,
loop recovery and Zhinu tests.

- Persist a transition intent/key and expected base before execution; capture the
  accepted run/transition receipt before waiting for completion.
- Separate start/activate from observe/wait. Reconcile an accepted transition
  after restart rather than treating every version mismatch as an orphan.
- Enforce duplicate/conflicting request behavior in the authoritative store;
  process-local locking/dictionaries are insufficient.
- Recover existing initial runs in Ensure; do not start another simply because
  the adapter instance is new.

**Required tests:** crashes after start/fork/activation acceptance and before
checkpoint; concurrent equivalent/conflicting transitions; stale writer;
waiting/failed runs; late completion; exactly one accepted activation and no
renewed allowance on retry.

**Gate:** if released Zhinu lacks atomic generation cutover/fencing or Fuwen
cannot pin the admitted revision, record the upstream capability request and
leave activation unsupported. Local contracts, fail-closed behavior and tests
may proceed; do not call a fork “cutover”.

**Done when:** the end-to-end acceptance tests pass against the supported released
authority, not only fakes. External implementation work needs its own task.

### GH-08 — Add bounded transition preview and explanations

**Depends on:** GH-03 and verified Fuwen comparison; receipt enrichment after
GH-06/GH-07. **Maps to:** Gap A.2/A.4, usability.
**Primary files:** new preview records/services, graph summaries, public sample.

- Preview base/candidate identities, added/changed/removed steps, affected
  artifacts, required approval, estimated or unknown work and truncation.
- Distinguish predicted reusable work from actual runtime reuse receipts.
- Bind preview/approval to an exact base; an old preview never authorizes a newer
  revision. Keep authorization and approval UI in the application.
- Use Fuwen comparison; do not implement another executable plan comparer.

**Required tests:** deterministic bounded output, stale basis, nested changes,
unknown cost, predicted-versus-confirmed reuse and zero execution effects.

**Done when:** a consumer can explain and inspect a proposal before activation.
Marang's public endpoint is a separately owned downstream task.

### GH-09 — Refactor around proven invariants and demonstrate composition

**Depends on:** GH-01 through GH-08 for the complete sample; independent pure
extractions may follow their relevant fixes. **Maps to:** structure/usability,
second consumer.
**Primary files:** loop and internal collaborators; samples; usage documentation.

- Extract admission, allowance, recovery and transition coordination from the
  large loop. Prefer explicit state transition methods to mutable flag bundles.
- Keep pure planning usable with deterministic implementations; move model/prompt
  options toward adapters without inventing unused abstractions.
- Add a runnable host-layer example using scripted planning, deterministic work,
  optional Qingniao delegation, waiting, evidence, rejection/correction and one
  explicit workflow revision. Align with QH-09 and the integration contract below.
- Document configuration, scope/lifetimes, stop reasons and recovery actions.

**Required tests:** existing behavior remains intact after each extraction;
public sample compiles/runs using packages; optional delegation can be removed;
domain schemas and transport types do not enter core.

**Done when:** improvements are demonstrable in consumer code. Do not demand a
new adapter package or move MCP implementation into Guihua.

### GH-10 — Verify compatibility, packaging and consumer handoff

**Depends on:** delivered packages; report gated items explicitly.
**Maps to:** Gap B/C, next steps and pre-stable gate.

- Test .NET 8 and 10 behavior for supported APIs; intentionally update test
  targets/CI if adding net8 coverage. Preserve the existing per-area coverage
  floors and public API analyzer enforcement.
- Update changelog, public baselines and a migration note for each changed
  contract/checkpoint/store schema.
- Build/format/test/pack and exercise an isolated package consumer. Verify
  Fuwen -> Guihua -> consumer release order against actual available packages.
- Produce a Guyabano migration checklist and a Marang integration handoff.
  Do not claim those repositories migrated without their tests.
- Keep baselines unshipped during preview; stable promotion is a separate
  explicit decision. Preparing packages does not authorize publishing them.

**Done when:** the release candidate is reviewable, reproducible and honest about
unsupported/gated activation. Do not mark the full activation milestone complete
if GH-06 is still waiting on upstream.

## Shared integration acceptance contract

Agree this contract with the Qingniao plan before implementing the shared sample:

1. Host supplies selected provider, immutable task inputs, workspace authorization,
   parent allocation and exact correlation; Qingniao does not reselect.
2. An ambiguous accepted delegation reconnects using its existing identity.
   Planner retries cannot create fresh semantic work implicitly.
3. Candidate correction changes candidate/node generation within the activity;
   workflow revision requires separate Fuwen admission and Zhinu transition.
4. Typed execution/evaluation/acceptance facts retain exact subjects. Summary
   truncation never removes the authoritative evidence reference.
5. A parent allowance accounts for planning, execution, review and correction.
   Replays cannot grant a new allowance or mutate terminal history.
6. One host-layer conformance scenario covers response loss, restart, waiting,
   rejected candidate and explicit plan replacement. No dependency cycle.

Core contract work in either repo may proceed against fakes before the complete
sample. Do not claim real cross-project recovery until the authoritative
integration gate passes.

## Model handoff instructions

Copy this assignment and substitute one task ID:

> Work in Penghou.Guihua on **GH-XX** from
> docs/implementation-handoff-plan.md. Read its prerequisites, the mapped review
> findings and applicable roadmap/ADR constraints. Inspect current HEAD and
> dirty files; preserve others' work. Implement only this package, add the listed
> behavioral regressions, and run the relevant checks. Verify upstream APIs before
> relying on them; record a precise blocker instead of cloning another project's
> authority. Update public API/persisted-schema migration notes when needed.
> Finish with changed files, commands/results, remaining risks, task status and
> the next eligible package. Do not publish packages or mark untested integration
> complete.

For every completed handoff record: task ID; starting/ending commit or uncommitted
diff; prerequisites actually verified; changed APIs/schemas; test evidence;
remaining upstream gates; and follow-up task IDs. States are planned, in progress,
blocked on named prerequisite, or complete. Update the roadmap only when its
original acceptance criteria are satisfied; a partial task is not a whole gap.

Suggested first assignments: **GH-00**, then **GH-01**; GH-04 is the independent
storage lane. Do not assign GH-06 as unconditional implementation work.

## Validation commands

From the repository root, after restore/build as appropriate:

```powershell
dotnet build Penghou.Guihua.slnx --configuration Release
dotnet format Penghou.Guihua.slnx --verify-no-changes --no-restore
dotnet test Penghou.Guihua.slnx --configuration Release --no-build
dotnet pack Penghou.Guihua.slnx --configuration Release --no-build --output artifacts
```

Use focused test filters during each task, then relevant full suites. GH-10 also
runs the exact coverage commands in `.github/workflows/ci.yml` (core 70%,
Baize 50%, Zhinu 45%, line and branch) and isolated consumer checks.
Review baseline: 53 core + 15 Baize + 6 Zhinu tests passed on .NET 10.
Counts are historical evidence, not a ceiling or future substitute for running
tests. This plan itself changes documentation only.
