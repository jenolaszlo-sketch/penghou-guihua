# Architecture, boundaries, and usability review

Reviewed: 2026-09-28. Source baseline: `50501cbc3f2cbfe4613175f0dc9fd15dfa52598a`.
Companion review: [Qingniao](../../Penghou.Qingniao/docs/architecture-boundary-review-2026-09-28.md)
(sibling-checkout link).

This is a review and proposed backlog, not an implementation or approved ADR.
The existing modifications to `ROADMAP.md` and untracked
[evidence-driven evolution proposal](evidence-driven-workflow-evolution-v2.md)
were read and preserved. That proposal is deferred V2 direction; the correctness
issues below do not depend on adopting it.

## Assessment

Keep Guihua and Qingniao separate. Their similar loops operate on different
subjects: Guihua proposes changes to a workflow design; Qingniao supervises one
delegated execution and its candidate revisions. Shared words such as retry,
revision, budget, checkpoint, and evidence do not imply shared ownership.

The package dependencies already support this separation: Guihua core depends
on Fuwen, its Baize and Zhinu integrations are separate, and neither reviewed
core references the other. The main weaknesses are behavioral: admission checks
can live in replaceable collaborators, execution happens before some validation
or checkpoint writes, and evidence crossing the execution seam is mostly prose.

Preserve the existing deterministic patch application, pinned design
fingerprints, preservation validator, content integrity checks, and injectable
planning collaborators. Improve the contracts around them before creating more
packages or general-purpose framework abstractions.

## Ownership contract to make explicit

| Concern | Guihua | Qingniao | Host / adjacent component |
| --- | --- | --- | --- |
| Workflow structure | Propose and validate design deltas | Does not edit the surrounding graph | Fuwen compiles/admit plans; host authorizes activation |
| One delegated activity | Propose its intended binding | Resolve the exact authorized provider; execute and supervise | Host selects provider and workspace policy |
| Repair | Repair a proposal or author a new plan revision | Reconnect an attempt or perform bounded candidate correction | Zhinu owns durable scheduling and recovery |
| Acceptance | Validate planning mechanics | Execute host verification verdicts over exact candidates | Host owns domain success criteria |
| Evidence | Consume exact attributed references and maintain planning provenance | Produce delegation, attempt, candidate, validation and review evidence | Execution store owns run facts; storage adapters retain payloads |
| Budgets | Planning iterations, mutations and planning call allocation | Delegated calls, duration and correction allocation | Parent host allocates a shared end-to-end allowance |
| Checkpoints | Planner state tied to a pinned design and transition | Supervisor decision tied to delegation/checkpoint/revision | Durable store provides fencing and committed transitions |

Recommended composition:

```text
host -> Guihua proposal -> Fuwen admission -> Zhinu activity
                                          -> Qingniao -> selected executor
execution receipts + candidate evidence -> host projection -> Guihua decision
```

Direct deterministic activities and direct Baize inference remain valid.
Qingniao is optional, not the executor for every planning operation. Guihua
must not automatically relaunch work whose Qingniao acceptance is ambiguous.

Do not merge planning artifacts with candidate artifacts. A plan revision,
workflow version, execution run, node generation, attempt, candidate revision,
and content digest each identify something different. An adapter should retain
all applicable identities rather than translate them into one generic version.

## Findings

Priority: P1 = address before relying on the affected guarantee; P2 = important
correctness, design, or usability improvement. “Reproduced” means an isolated
probe ran; “source-confirmed” means the cited control flow was inspected.

### G01 — P1: the hard model-call budget is not enforced at each invocation

**Evidence:** [PlanningLoop.cs](../src/Penghou.Guihua/PlanningLoop.cs),
`DecideAdmittedAsync` / `DecideAsync` (374–450), `ExpandAsync` (593–656);
[WorkflowAuthor.cs](../src/Penghou.Guihua.Baize/WorkflowAuthor.cs),
`AuthorCoreAsync`; [WorkflowPatchProposer.cs](../src/Penghou.Guihua.Baize/WorkflowPatchProposer.cs).

The outer loop checks the allowance, but semantic repair and stale-basis
re-observation invoke the decider again without reserving a call. Proposer and
author adapters can each make several calls before reporting their total.
Stage execution has no usage field in `StageExecutionResult`. Token accounting
only adds `RevisionExecutionResult.ReportedTokens`; it omits planner usage, and
the Zhinu implementation currently returns null.

**Reproduced:** set `MaxModelCalls = 1`, return a finish decision without a reason,
then a valid decision. The run records **2 calls**, exceeding its hard limit.

**Recommendation:** reserve from a run-scoped allowance before every actual model
invocation, including repair and stages. Pass a bounded allowance into adapters;
record usage even on failure and interruption. Preserve “unknown” usage instead
of treating it as zero. Keep the explicitly best-effort token policy distinct
from hard call limits and from Qingniao worker-call budgets.

**Acceptance:** one remaining call permits at most one actual invocation through
any repair path; retries after restart cannot restore spent allowance.

### G02 — P1: a rejected finish decision can already have produced artifacts

**Evidence:** [PlanningLoop.cs](../src/Penghou.Guihua/PlanningLoop.cs),
`RunAsync` (106–129), `ValidateDecision` (457–505);
[PlanningLoopTests.cs](../tests/Penghou.Guihua.Tests/PlanningLoopTests.cs),
`Loop_rejects_production_on_finish_decisions`.

Stage production runs before the switch that rejects `Finish + ProduceStages`.
The existing test checks the failure reason but not the absence of side effects.

**Reproduced:** the loop returns `Failed` while `research-notes/main@1` has already
been published as valid.

**Recommendation:** admit the complete decision, including action/stage
compatibility and applicable bounds, before invoking any executor.

**Acceptance:** a rejected decision makes zero executor calls and publishes no
stage output. The invalid decision can receive bounded repair feedback.

### G03 — P1: execution and planner checkpointing leave recovery and concurrency gaps

**Evidence:** [PlanningLoop.cs](../src/Penghou.Guihua/PlanningLoop.cs),
`RestoreAsync` (190–281), `ExpandAsync` (663–697);
[ZhinuPlanningExecutionHost.cs](../src/Penghou.Guihua.Zhinu/ZhinuPlanningExecutionHost.cs),
`EnsureWorkflowAsync` (42–81), `ExecuteRevisionAsync` (121–190),
`ResolveAsync` (193–213).

Initial execution completes before the first checkpoint. Revision execution
completes before the next checkpoint. A crash between those writes can leave a
real run without the corresponding planning state. A revision mismatch on
restart fails closed, which is good, but cannot reconcile the loop's own
already-accepted transition.

`EnsureWorkflowAsync` checks only its process-local dictionary; a fresh adapter
can start another run even when the store already contains one. Concurrent
calls can both pass `ContainsKey`. A `ConcurrentDictionary` protects individual
operations, not the admit/register/start/execute sequence. Revision calls also
lack an expected-base transition token.

**Recommendation:** persist a transition intent with stable request key,
expected run/version/fingerprint and proposed admitted revision before execution.
Return a durable transition receipt/run ID as soon as accepted; observe it
separately. Reconcile that receipt before starting or forking again. The
execution authority should enforce fencing; a local semaphore alone does not
solve multi-process recovery.

**Acceptance:** fault injection after start/fork and before checkpoint causes
one execution, not a duplicate or unrecoverable orphan. Concurrent equivalent
requests reconnect; conflicting requests receive a typed conflict. Include
failed and waiting runs, not only completed runs.

### G04 — P1: replaceable planning collaborators also own essential admission checks

**Evidence:** [PlanningLoop.cs](../src/Penghou.Guihua/PlanningLoop.cs),
`ExpandAsync` (598–668);
[PlanningLoopSeams.cs](../src/Penghou.Guihua/PlanningLoopSeams.cs);
[WorkflowAuthor.cs](../src/Penghou.Guihua.Baize/WorkflowAuthor.cs),
`AuthorCoreAsync`.

The loop trusts `ProposedRevision.Applied` and `AuthoredRevision.Plan` after a
success flag. The Baize implementations apply patches and check preservation,
but another implementation can return a stale patch, unrelated applied design,
or a DSL/plan pair that differs from its claimed result. The Zhinu adapter
re-admits DSL but does not establish that it preserves the approved patch scope.

This is an extension-boundary weakness, not a claim that the supplied Baize
adapter deliberately bypasses validation.

**Recommendation:** the kernel applies the proposed patch itself and owns
mandatory preservation checks over the actual candidate plan. Pass an admitted
revision value containing its DSL/definition identity and receipt across the
execution seam. Host policy and catalogue identity must be explicit; avoid
independently hard-coding `CapabilityGrantPolicy("policy/1", [])` in author and
execution adapter.

**Acceptance:** fake collaborators returning success with a mismatched applied
design, stale base, changed untouched node, or inconsistent DSL cannot execute.

### G05 — P2: the execution seam loses facts needed for useful decisions

**Evidence:** [PlanningLoopContracts.cs](../src/Penghou.Guihua/PlanningLoopContracts.cs),
`WorkflowExecutionSnapshot`, `RevisionExecutionResult`;
[ZhinuPlanningExecutionHost.cs](../src/Penghou.Guihua.Zhinu/ZhinuPlanningExecutionHost.cs),
`Snapshot` / `EvidenceBody` (339–357).

A version, completion boolean, and truncated output string do not distinguish
waiting, failure, cancellation, unavailable evaluation, or rejected candidate.
There are no exact run/attempt/evidence references in this seam. Both start and
revision execution wait for completion, making long-lived supervised work hard
to expose through the planner.

**Recommendation:** return typed execution status and immutable evidence
references plus bounded human-readable summaries. Add host acceptance policy
separately from execution completion. Treat `Finished` as “planning stopped”;
a model finish reason or no-op convergence is not proof that the user's goal
passed its acceptance criteria.

**Acceptance:** a Qingniao `NeedsSupervisor`, failed deterministic validation,
and completed-but-rejected candidate remain distinguishable in Guihua.
Missing or truncated evidence cannot silently imply acceptance.

### G06 — P2: malformed stage proposals can throw instead of returning diagnostics

**Evidence:** [PlanningStageValidator.cs](../src/Penghou.Guihua/PlanningStageValidator.cs),
`Validate` (66, 131), `Order` (140–143), `HasCycle` (176–177).

Duplicate names are recorded as validation errors, but validation still calls
`HasCycle`, which invokes `ToDictionary` on the duplicate names and throws.
Malformed model output therefore escapes the normal repair-feedback path.

**Recommendation:** run graph algorithms only after basic identity validation,
using the validated node map. Parse `kind/name@revision` through one checked
value object before slicing or integer conversion in the runner. Reject null,
empty, malformed and duplicate collections at the deserialization boundary.

**Acceptance:** duplicate instances produce stable diagnostics with zero
executor calls; malformed revision pins never cause slicing/parse exceptions.

### G07 — P2: catalog head selection depends on wall-clock order

**Evidence:** [PlanningArtifactCatalog.cs](../src/Penghou.Guihua/PlanningArtifactCatalog.cs),
`WriteIndexAsync` (316–340);
[FileSystemArtifactRepository.cs](../src/Penghou.Guihua/FileSystemArtifactRepository.cs),
`ReadLatestAsync` (169–225).

Index generations are stored, but latest selection uses `CreatedAt`.
The per-instance monotonic timestamp guard resets on restart. Clock rollback
can make a newer generation older by timestamp, causing later reads to use the
previous index. This risk exists even under the documented single-writer rule.
Multiple catalog instances are explicitly outside that rule and need fencing.

Each publication also stores a full historical index, and latest reads scan and
deserialize all index files. Growing workflows accumulate repeated history.

**Recommendation:** use an explicit generation/CAS head or a storage-native
ordered head independent of wall-clock time. Keep append-only revisions but
avoid re-reading every prior full index. If the filesystem implementation stays
single-writer, enforce or prominently surface that constraint.

**Acceptance:** restart with a clock behind the last write retains the newest
generation; conflicting writers are rejected; catalog read cost does not grow
with the size of every historical snapshot.

### G08 — P2: checkpoint identity and audit continuity are incomplete

**Evidence:** [PlanningCheckpoint.cs](../src/Penghou.Guihua/PlanningCheckpoint.cs);
[PlanningLoop.cs](../src/Penghou.Guihua/PlanningLoop.cs),
`RestoreAsync`, `LoopState.Resume` (771–800), `SaveAsync` (839–873).

Checkpoint lookup uses workflow ID without binding the original goal and policy.
A reused ID can return an old terminal outcome for a different goal, or resume
under different policy/catalogue assumptions. Recompiling saved DSL against a
current catalogue does not by itself prove the original admitted identity.

`Resume` restores counters and known revisions but never copies
`checkpoint.DecisionLog` into `DecisionLog`; the next save loses previous
entries from the latest checkpoint, though old artifact revisions remain.

**Recommendation:** bind a caller request identity, goal/input digest, catalogue
and admission fingerprint, and policy version to the checkpoint. Reject semantic
reuse or require an explicit new planning run. Restore log continuity or store
bounded events behind an explicit history reference.

**Acceptance:** changed goals conflict rather than replaying old success; changed
admission basis requires an explicit transition; resumed history includes
pre-restart decisions.

## Structure and OOP improvements

- Split the 877-line `PlanningLoop` along existing invariants: decision admission,
  revision assembly/admission, budget accounting, checkpoint recovery, and a thin
  orchestration driver. Do not build a generic workflow engine inside Guihua.
- Give run state transition methods such as `RecordUsage`, `AcceptRevision`,
  and `Complete` instead of independent public setters plus nullable failure
  flags. Prefer typed outcomes to `Succeeded + nullable payload` combinations.
- Snapshot collections at admitted boundaries. `IReadOnlyList<T>` does not
  prevent mutation through another reference to the original list, and
  `required` does not validate semantic correctness.
- Keep `PlanningGraph` a proposal representation. Establish and test its mapping
  to Fuwen plans, especially nested constructs; avoid maintaining two executable
  semantic models. `PatchPreservationValidator` currently indexes top-level
  nodes and top-level inference prompt references: add nested conditional,
  repeat and fan-out coverage before claiming general nested patch support.
- Isolate filesystem storage from pure planning code through the existing port;
  a separate storage package is worthwhile only if consumers need it. Do not
  extract a shared Guihua/Qingniao “engine” merely because both contain loops.
- Move model names, token limits and prompt-pack details toward adapter request
  options. Deterministic planners/stages should not need fake model identifiers
  to satisfy a core API. Share small internal Baize repair/diagnostic utilities
  only after the allowance and admission contracts are correct.

## Usability and usefulness

1. Add a complete composition sample: scripted planner, deterministic activity,
   one Qingniao delegation, evidence projection, rejected candidate, bounded
   correction, and explicit plan revision. Include a restart and a wait state.
   The current sample intentionally stops at patch/catalog mechanics.
2. Add a dry-run revision explanation showing affected steps, unchanged steps,
   expected reuse versus authoritative reuse receipts, required approval, and
   estimated/unknown cost. Keep preview separate from activation.
3. Surface structured progress and typed stop reasons, with exact artifact links
   and bounded summaries. A user should be able to answer “what changed, why did
   this stop, what can I do next?” without reading a checkpoint JSON file.
4. Document storage lifetime/single-writer assumptions, policy ownership, the
   difference between planning completion and accepted work, and how to recover
   a transition interrupted after execution acceptance.
5. Test both advertised target frameworks where practical: libraries build for
   .NET 8 and 10, but the current Guihua test projects target only .NET 10.

## Recommended delivery order and verification

1. Fix G01, G02 and G06 with focused behavioral regressions.
2. Establish admitted revision and durable transition contracts (G03/G04/G08).
3. Add typed evidence/status mapping and acceptance policy (G05).
4. Improve catalog head semantics and storage scaling (G07).
5. Refactor orchestration and add the composition/preview experience. Keep
   deferred V2 experiments and historical learning outside these V1 fixes.

Executed on Windows with .NET SDK 10.0.401:

```powershell
dotnet test Penghou.Guihua.slnx --configuration Release --no-restore --verbosity minimal
```

Existing tests passed: **53 core + 15 Baize + 6 Zhinu = 74**, all on .NET 10.
The command builds the required projects; this review did not run the complete
CI formatting, coverage-threshold or packaging workflow.

Two isolated .NET 10 probes reused temporary copies of the existing
`PlanningLoopTests` helpers, without editing repository tests:

| Probe | Required invariant | Observed |
| --- | --- | --- |
| Malformed then repaired decision, one-call budget | At most one invocation | Two recorded calls |
| Finish decision containing research stage | No published stage output | Valid `research-notes/main@1` |

The probes intentionally asserted the required behavior and failed. The other
findings are source analysis and proposed regression scenarios, not additional
executed reproductions. No live model calls or production workflow runs were
performed.

## Follow-up implementation plan

[Implementation and model handoff plan](implementation-handoff-plan.md) aligns these findings with the roadmap and defines bounded assignments, prerequisites and acceptance tests.
