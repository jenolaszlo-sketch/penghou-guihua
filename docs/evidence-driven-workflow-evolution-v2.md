# Penghou evidence-driven workflow evolution

Status: **deferred V2 design; not a V1 release requirement**
Reviewed: **2026-09-26**
Planning home: Penghou.Guihua; delivery ownership remains with each project.

This document replaces the pasted draft as the working architecture proposal.
It records direction, constraints, and acceptance gates, not implemented APIs.
V2.1, V2.2, and V2.3 below are cross-project delivery stages, not NuGet
versions, Fuwen IR versions, or replacements for existing roadmap numbering.

## 1. Delivery boundary

Finish each project's current V1 commitments first. Existing retry, validation,
bounded repair, revision comparison, supervision, and evidence work keeps its
current priority. This proposal adds no V1 acceptance criteria, package
dependency, database migration, or new runtime status.

Consult this design when touching existing identity, provenance, dependency,
and compatibility boundaries. A review should ask whether history remains
identifiable, definitions remain pinned, schemas can evolve, and planning stays
outside execution authority. A "yes" means the seam is adequate; a possible
future improvement alone does not justify delaying V1.

The V2 objective is to make future plans better using attributable execution
evidence while keeping every activation explicit:

```text
plan -> admit -> execute -> observe -> evaluate -> decide
  ^                                               |
  +-- bounded evidence lookup <- record <- revise -+
```

The first useful result is outcome-aware repair. Experiments and historical
preferences follow only after that path survives recovery and concurrency.

## 2. Review findings and existing foundations

The draft's direction is sound, but the following corrections are necessary.

| Draft issue | Revised decision |
| --- | --- |
| One list mixes success, failure, rejection, and supersession | Keep execution lifecycle, failure classification, evaluation, acceptance decision, and revision disposition separate. |
| "Current replay" can mean silently running the newest graph | Recovery is pinned to the admitted definition. A newer plan requires a new admitted revision and explicit activation. |
| A linear A -> B -> C -> D -> E example suggests E can be independent | Use an actual independent branch; compute affected dependencies and revalidation separately. |
| Qingniao chooses the delegate/model | Qingniao resolves the caller-selected provider. Host/Guihua policy proposes delegation choices; Baize owns model-routing mechanics. |
| Zhinu appears to be a generic evidence/learning store | Zhinu owns execution receipts; Hongxian/Siming retain attributed temporal evidence; other stores keep their existing authorities. |
| Rejection automatically means permanent dead end | Suppression is scoped, versioned, expiring, and conditional on equivalent context and evaluation policy. |
| Compensation makes speculative external operations safe | Compensation can fail and cannot undo observation. Begin with isolated, read-only or pure candidates and a separate authorized commit. |
| One experiment yields a winner and general preference | Allow inconclusive, none acceptable, incomplete, and cancelled outcomes; one win is an observation, not a general rule. |

Repository evidence inspected for this review:

- Zhinu already has durable step states (`Completed`, `Failed`, and others),
  dependency-aware restart receipts, fork lineage, compensation, and
  at-least-once delegates. Its roadmap separately tracks single-owner
  workflow-instance/execution-generation cutover. A fork is not proof that
  this cutover contract is complete.
- Fuwen already has typed immutable artifacts, immutable revision envelopes,
  semantic comparison, bounded repeat and keyed fan-out, context snapshots,
  admission, and execution fingerprints. Start with those constructs and
  typed evaluator/selector activities, not a new DSL vocabulary.
- Guihua already has patches, preservation validation, a bounded planning
  loop, deciders, and a Zhinu execution adapter. The adapter currently uses
  forks and process-local tracking; durable transition/recovery authority
  must not be inferred from that dictionary.
- Qingniao already separates immutable candidate revisions, deterministic
  validation, model review, provider handles, and bounded correction. Its
  current architecture explicitly requires caller-supplied provider identity.
- Hongxian already has evidence envelopes, verified projections, bounded
  recall, and derivation contracts. Its historical-execution/retrospective
  profile is the natural extension point, not a new parallel learning store.
- Cangjie supplies retained context and immutable snapshots; Hetu supplies
  publication-bound code facts; Baize supplies invocation/routing provenance.
  Marang is the supervisory facade; Guyabano owns product/workspace policy.

These are local source/roadmap observations, not claims that every planned
integration or currently checked-in capability has shipped in a package.

## 3. Ownership and integration contract

| Component | Owns in this proposal | Must not acquire |
| --- | --- | --- |
| Guihua | Bounded evidence-informed proposals, dead-end policy inputs, patch rationale, experiment proposals | Durable execution state, domain acceptance policy, evidence storage |
| Fuwen | Typed intent, compilation, immutable revision/lineage, semantic comparison, compatibility diagnostics | Live evidence lookup during compilation, physical reuse authorization |
| Zhinu | Authoritative run/attempt facts, pinned recovery, fenced transitions, durable experiment accounting through runtime/host contracts | Strategy ranking, autonomous replanning, domain quality judgments |
| Qingniao | Execute the selected delegation, preserve exact candidate/attempt/handle evidence and bounded supervision | Hidden provider reselection, general experiment/workflow orchestration |
| Baize | Provider/model routing mechanics, actual resolved invocation identity, usage and cost provenance | Experience store, workflow winner selection, knowledge promotion |
| Hongxian + Siming | Attributed evidence stream, idempotent correlation, verified projections, recall and derivation receipts; Siming owns integrity | Shadow Zhinu state, claims upgraded into facts, cross-store transactions |
| Cangjie | Scoped retained preferences/candidate lessons and immutable context snapshots | Automatic promotion, activation authority, temporal execution ledger |
| Hetu | Exact code publication/query references and bounded structural impact evidence | Workflow dependency authority or permission to reuse output |
| Marang | Authorized, bounded proposal/decision/explanation surfaces | A second planner, scorer, evidence authority, or runtime |
| Guyabano/host | Domain criteria, evaluator trust, budgets, provider choice, workspace isolation, approvals and promotion | Reimplemented generic planning, execution or session kernels |

Only executions requiring delegation pass through Qingniao. Direct activities
and inference remain valid. Optional adapters connect evidence to Hongxian and
context to Cangjie/Hetu; core packages do not gain those dependencies.

Existing cryptographic and storage contracts in Siming, Nuwa's representation
repair, and LatticeDB's provider functionality need no new workflow-learning
feature in this proposal. Revisit them only for a demonstrated integration gap.

## 4. Separate facts, judgments, and decisions

Conceptual dimensions, not proposed enum changes:

| Dimension | Examples | Authority |
| --- | --- | --- |
| Execution lifecycle | pending, running, waiting, completed, failed, cancelled | Existing executor/runtime |
| Execution failure classification | transient, condition-blocked, unknown; safe/unsafe to retry | Typed provider fact plus host retry policy |
| Evaluation assertion | quality below threshold, tests passed, cost high, inconclusive, evaluator fault | Identified evaluator and exact evaluated subject |
| Acceptance decision | pending, accepted, rejected, needs review | Versioned host acceptance policy over required evidence |
| Revision/use disposition | current, superseded, reused, invalidated, revalidation required | Admitted transition and authoritative reuse receipt |

A completed step can have a rejected output and later be superseded. Its
execution remains completed. Evaluations do not rewrite step status or immutable
terminal Qingniao results. A plan failure is a diagnosis/decision linked to
evidence, not a new engine failure state.

A timeout is not necessarily safe to retry: an external operation may already
have been accepted. Reconnect/reconcile its durable handle first. "Permanent"
means blocked under recorded conditions, not forbidden forever; credentials,
capabilities, versions, and dependencies can change.

Acceptance starts unknown/pending where evaluation is required. A missing
evaluation, evaluator crash, unavailable artifact, or conflicting mandatory
evidence cannot silently become acceptance or rejection of the underlying
activity. Host policy defines required evaluators, disagreement resolution,
timeouts, and explicit human overrides. Deterministic validation cannot be
overridden by a model asserting success.

Required evaluation gates block dependent progression until their decision
commits. Late advisory evidence can trigger a future proposal, but cannot
retroactively undo completed external effects.

## 5. Versioned evidence profile

Define a small cross-project profile over existing envelopes and opaque
references. Do not create one giant shared package or nest mutable projections
inside an immutable execution record.

The following are conceptual minimum fields; adapters map existing names.

```text
EvidenceRecord
  SchemaVersion, EvidenceId, ProducerId, ProducerVersion, IdempotencyKey
  Nature: receipt | measurement | assertion | decision | derivation
  Subject:
    scope / session reference
    workflow instance reference, PlanRevisionId, execution fingerprint
    execution generation reference, WorkflowRunId
    structural node identity, runtime item/iteration identity
    node generation, attempt identity, implementation/version identity
    artifact identity + exact revision/content digest where applicable
  OccurredAt, RecordedAt, source sequence/checkpoint
  CorrelationId, CausationId, supporting/contradicting evidence references
  Payload or protected bounded payload reference
```

Preserve distinctions between semantic execution generation, a node's
generation, retry attempt, and lease/fencing generation. In particular,
Zhinu `LeaseGeneration` is not `PlanRevisionId`. Adapter mappings must explain
Qingniao's `ExecutionEpoch` and `NodeGeneration` rather than assume identical
names imply identical authority.

Additional profiles:

- **Evaluation:** evaluator identity/version, rubric/schema version, evaluated
  artifact/input snapshot, method, raw metric with units, classification,
  optional score/uncertainty with defined semantics, evidence references,
  evaluation time, and corrections/retractions linked to earlier evaluations.
- **Decision:** alternatives considered, which were actually executed, selected
  action or no-selection, reason code and concise explanation, policy/version,
  input evidence snapshot/checkpoint, actor/authority, exact target revision,
  constraints and budget, and an idempotent decision operation identity.
- **Measurements:** elapsed duration versus summed candidate work, attempt and
  correction counts, token usage, tool/provider calls, and cost currency/unit/
  pricing revision. Distinguish measured, estimated, partial and unavailable;
  unavailable never means zero. Charge failed, losing, cancelled, evaluator,
  repair and planning work as well as successful work.
- **Context:** versioned input, constraint, acceptance-policy, capability,
  environment, model/provider, prompt/tool and retrieved-context fingerprints
  where relevant. Record fingerprint algorithm/canonicalization version.
  Missing fields make equivalence uncertain, not automatically true.
- **Relations:** supersedes, replaces, evaluates, corrects, experiment/candidate
  membership, selected-by and derived-from. Label causal assertions as claims
  unless supported by an authoritative observation; correlation is not proof
  that an activity caused later cost or failure.

Evidence append and decision publication are idempotent: identical retries
return the original receipt; conflicting reuse is rejected. Decisions keep the
snapshot they used even when later evaluations disagree.

Sensitive content belongs behind authorized, bounded artifact/payload
references. Scope isolation applies to storage, retrieval and aggregation.
Digests alone are not anonymization. Retention or deletion can make evidence
unavailable; record that limitation rather than inventing reproducibility.
Audit immutability does not require indefinite retention of raw prompts.

## 6. Recovery, inspection, and evolution

Use explicit operation names rather than four overloaded "replay" modes.

| Operation | Meaning |
| --- | --- |
| Recover/resume | Continue the same admitted definition and durable generation, reusing committed work and reconnecting external operations. |
| Inspect historical execution | Read recorded definitions, receipts, artifacts and context snapshots without invoking activities. Zhinu's current-state recovery is not an event-sourced simulator. |
| Restart | Explicitly invalidate/re-execute allowed work under existing restart semantics. |
| Repair/replan | Compile and admit a new immutable revision; preview impact; activate through a fenced transition. |
| Re-optimize | Optional policy-authorized proposal, subject to the same transition rules even if the prior workflow succeeded. |
| Re-execute for comparison | Start explicit new work with its own identity, effects policy and budget; no promise of reproducing nondeterministic external outputs. |

Do not automatically substitute "latest" source, policy, model or evidence on
recovery. A historical projection rebuild must not execute tools.

Repair sequence:

1. Persist the exact outcome/evaluation and acceptance decision.
2. Guihua consumes a bounded, immutable evidence/context snapshot and proposes
   a patch against an exact base revision and objective.
3. Fuwen validates and compiles the candidate, preserving required acceptance
   criteria and capabilities. Changing those requires explicit host authority.
4. The host obtains a Zhinu transition preview bound to the current generation,
   source/candidate plan identities, artifact revisions and policy.
5. Reuse analysis distinguishes data dependencies, control dependencies,
   validation dependencies and external-effect dependencies. Fuwen comparison
   suggests correspondence; runtime provenance and host policy decide reuse.
6. Authorize the exact preview, quiesce as required, and atomically commit
   cutover plus reuse/invalidation decisions in Zhinu. Stale previews or
   competing activation commands conflict instead of applying to new state.
7. Recover the sole active generation after crashes. Preserve late old results
   as evidence without letting them schedule successors.
8. Mirror committed receipts into Hongxian by an owning-store outbox or durable
   cursor and idempotent forward reconciliation. Expose lag explicitly; there
   is no transaction spanning Zhinu, Hongxian and artifact stores.

Example with real independence:

```text
A -> B -> C -> D
     |
     +------> E

replace C with C2:
A/B: reuse if compatible
C: retain history; superseded in new revision
C2: execute
D: invalidate/re-execute if it consumed C or its control outcome
E: reuse only if its actual inputs, effects and validation remain compatible
```

A changed quality rubric may require reevaluating an artifact without
regenerating it. A changed input or producer implementation may require new
output. Loop items, keyed fan-out siblings, child workflows, waits and durable
external handles need their existing identity-specific compatibility rules.
Unknown dependency coverage requires conservative invalidation or explicit
review, not a claim that a branch is independent.

Invalidation does not undo side effects. Idempotency identity for a replacement
must distinguish new semantics while ambiguous retries of the same external
operation retain its original identity. Compensation is a separate recorded
operation and can fail.

## 7. Context-sensitive dead ends and bounded decisions

A rejected output is not automatically proof the entire approach is bad.
A versioned host policy determines whether evidence supports suppression,
repair, another bounded attempt, an experiment or escalation.

An avoidance record includes the approach/version, task class, material context,
acceptance/evaluator versions, supporting and contradicting evidence, scope,
creation/expiry or revalidation conditions, and the reason for avoidance.
Exact equality and policy-defined compatibility are different operations.
Do not use embedding similarity alone to authorize reuse or suppression.

Unknown or stale evidence falls back to the declared baseline policy or
supervisor action. Changed conditions permit reconsideration. Explicit human
overrides are recorded with the replacement decision and later outcome; an
override is authoritative for that decision, not an automatically generalizable
training label.

Bound total replans, repeated repair oscillation, added cost, time, calls,
tokens and invalidated work across the logical workflow. Restart, cutover and
nested experiments do not reset these budgets. Stop with an explainable
unresolved outcome when no admitted action remains.

## 8. Comparative execution

An experiment is an explicit bounded workflow region, initially composed using
existing keyed fan-out, typed evaluator/selector activities and immutable
artifacts. Add dedicated Fuwen syntax only after two real consumers demonstrate
that those primitives cannot express a necessary contract safely.

Record before execution:

- Experiment identity/question, base input and baseline candidate.
- Stable candidate identities, immutable candidate definitions and exact
  differences: implementation, prompt, model/profile, tools, context, input or
  topology. Qingniao candidate revision IDs remain distinct from experiment
  candidate IDs; record their mapping.
- Shared requirements, frozen evaluation rubric, evaluator versions and
  independence limitations, constraints, selection/tie policy and stop policy.
- Scope, isolation/effect policy, maximum candidates/concurrency, cumulative
  cost/work/time/token limits and who authorized them.
- Candidate/provider handle references, selection decision, actual metrics and
  all relevant evidence, including partial or unsuccessful attempts.

Select only among candidates satisfying hard constraints. Outcomes include
selected, tied/inconclusive, none acceptable, budget exhausted, failed,
cancelled and pending required evaluation. A losing candidate is not an
execution failure. A candidate that never ran is a considered alternative, not
negative empirical evidence. Do not use the final successful artifact to hide
repair effort or failed candidates.

A repair followed by success is observational before/after evidence. It is not
a controlled comparison when inputs, context or rubric changed. Confidence
must not imply statistical significance without a defined sampling method.
Repeated retry attempts of one case are not independent samples.

### Effects, accounting and recovery

Effect classification has independent dimensions: mutability, external
visibility, idempotency, reversibility/compensation, and uncertainty. "Pure" is
not safe merely because a candidate says so.

The first experiment slice permits only host-enforced pure/read-only work or
isolated candidate workspaces with restricted capabilities. Proposal and
selection happen before a separate authorized commit/publish/send/payment
operation. No automatic live irreversible experiments; compensation or approval
alone does not make duplicated effects safe.

Reserve/check aggregate allowance before scheduling candidates and evaluations.
Host adapters must enforce the declared limits or reject admission. Account for
parallel in-flight work and provider cancellation lag; where a strict monetary
ceiling cannot be guaranteed, report that limit and decline strict-cap admission
or use a conservative supported reservation. Unknown cost cannot authorize
unbounded additional work.

Candidate starts, evaluation and selection have stable operation IDs.
Ambiguous provider acceptance is reconciled before starting again.
Selection commits once against exact candidate revisions/evaluation inputs;
recovery returns that decision rather than choosing again. Cancellation
preserves partial evidence and accounting and never implies rollback.

## 9. Reuse of experience and later learning

Hongxian projects evidence into bounded contextual aggregates and recall
results; Guihua/host policy consumes a pinned snapshot. Baize can receive
host-supplied routing signals; Qingniao executes the chosen provider.

A preference includes scope, task/context compatibility rules, strategy and
versions, supporting/contradicting references, independent case count,
observation window, uncertainty method where supplied, last validation,
freshness/expiry, derivation/policy version and limitations.

No universal score is required. Report tradeoffs between quality, cost, latency,
reliability and corrective effort with comparable units and rubrics. Selection
bias, shared evaluator bias, input changes and provider drift limit what can be
learned. The selected option's additional observations must not erase uncertainty
about alternatives that stopped receiving traffic.

V2.3 begins with deterministic thresholds and explicit contextual preferences.
Unavailable, contradictory, stale or scope-incompatible recall uses baseline
policy. Strong relevant evidence can skip an experiment but never skip
authorization, artifact validation or required acceptance checks.

Later, application policy may promote observations into candidate lessons and
reviewed preferences through Cangjie-compatible references. Keep supporting
and contradicting evidence, validity conditions, promotion/demotion decisions,
supersession and immutable retrieval snapshots. Cangjie does not itself become
a long-term knowledge repository or promotion authority.

Topology optimization, credit attribution, evaluator calibration, cross-workflow
generalization, drift detection and exploration scheduling remain later V2+
research driven by measured need. Inferred contributions and counterfactuals
remain attributed claims. No reinforcement-learning platform or hidden
self-modification is required.

## 10. Delivery stages and acceptance gates

| Stage | Required dependency gate | Deliverable and proof |
| --- | --- | --- |
| V2.1: outcome-aware repair | Existing V1 gates complete; Fuwen admission/comparison, Zhinu authoritative transition/recovery, host acceptance/evidence integration ready | A completed activity is rejected by an attributed evaluator; one authorized replacement activates; dependent work reruns and an independent branch is retained. Original result, judgment and transition remain inspectable. |
| V2.2: comparative execution | V2.1 plus enforceable isolation, aggregate accounting and durable selection | Two bounded candidates run with separate artifacts; fixed criteria select once or report no winner; recovery neither repeats accepted external work nor drops losing evidence. |
| V2.3: evidence-aware reuse | V2.2 evidence profile plus Hongxian recall/aggregate and context compatibility contracts | A later compatible case uses a pinned preference to skip the experiment; changed version, stale/conflicting evidence or different scope uses declared fallback. |
| Later V2+ | Demonstrated value from earlier slices and representative data | Promotion/demotion, calibration, drift, topology and cross-workflow learning, each with its own evidence and policy gate. |

These are dependency gates, not an instruction to implement missing
prerequisites during V1. Existing shipped primitives are reused rather than
relisted as unimplemented features. No blanket migration of legacy histories:
older evidence remains readable, explicitly incomplete, and ineligible for a
decision when required context is missing.

### Deterministic acceptance scenarios for future implementation

- Completed execution plus rejected evaluation preserves both facts; evaluator
  timeout/missing data cannot become a false rejection or acceptance.
- Safe transient retry stays within the same semantics; ambiguous provider
  acceptance reconnects; a versioned avoidance rule prevents unchanged repair
  loops and changed conditions permit reconsideration.
- Cutover command retries are idempotent; competing/stale previews conflict;
  failures before/after commit recover the correct owner; late old-generation
  completion cannot advance the old plan.
- Changed output invalidates true consumers; independent fan-out/branch work is
  retained; rubric-only change revalidates; unknown dependencies fail safely.
- Historical inspection/projector rebuild performs no execution; ordinary
  recovery cannot switch definitions or reread live preference policy.
- Evaluators may disagree; corrections append; human override includes authority
  and references without rewriting terminal results.
- Candidate identities and sealed artifacts cannot cross-contaminate; losers
  and partial work remain recorded; none acceptable/inconclusive are valid.
- Experiment retry/cancellation and parallel admission respect cumulative
  allowances; unknown cost is explicit; no unsafe external effect is duplicated.
- Evidence delivery duplicate/conflict/reorder and projection lag are visible;
  a rebuilt projection agrees at its verified checkpoint.
- Wrong scope, missing context, old model/rubric, stale or contradictory evidence
  cannot authorize a preferred route; unavailable recall has a tested fallback.
- Legacy records retain original meanings and fingerprint rules. Required new
  semantics fail admission on unsupported adapters instead of being ignored.

### Product measures

Compare against the same task set and baseline policy: repeated rejected work,
reuse/revalidation rate, additional corrective effort, total measured/unknown
cost, end-to-end latency, evaluator overhead, unresolved outcomes and human
overrides. Inspect why work ran, was reused, rejected or superseded; why a
candidate won or no winner existed; which exact evidence and policy influenced
the decision; and what was unavailable or stale.

## 11. Per-project roadmap map

The entries below are deferred V2 additions to existing roadmaps.

| Project | Local roadmap | V2 contribution |
| --- | --- | --- |
| Guihua | [ROADMAP](../ROADMAP.md) | Proposal/avoidance/experiment/reuse policy mechanics |
| Zhinu | [ROADMAP](../../Penghou.Zhinu/ROADMAP.md) | Durable outcome linkage, transitions, runtime accounting/selection receipts |
| Fuwen | [roadmap](../../Penghou.Fuwen/docs/roadmap.md) | Typed intent and evidence/revision contracts |
| Qingniao | [roadmap](../../Penghou.Qingniao/docs/roadmap.md) | Exact selected-provider execution and candidate evidence |
| Hongxian | [roadmap](../../Penghou.Hongxian/docs/roadmap.md) | Temporal evidence profiles, experiment/decision projections and recall |
| Cangjie | [ROADMAP](../../Penghou.Cangjie/ROADMAP.md) | Scoped contextual preferences, snapshots and promotion seam |
| Baize | [experience signals](../../Penghou.Baize/docs/roadmap-experience-signals.md) | Actual invocation metrics and advisory routing signals |
| Hetu | [ROADMAP](../../Penghou.Hetu/ROADMAP.md) | Publication-bound context and bounded impact evidence |
| Marang | [roadmap](../../Marang/docs/roadmap.md) | Supervisor decisions and bounded explanations |
| Guyabano | [ROADMAP](../../Guyabano/ROADMAP.md) | Domain evaluators, isolated candidates and first consumer proof |

Cross-repository relative links assume the normal sibling checkout layout.
The owning project roadmap is authoritative for implementation status.
