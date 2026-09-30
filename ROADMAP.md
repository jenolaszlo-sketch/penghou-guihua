# Penghou.Guihua roadmap

## Objective

Guihua is the reusable planning and workflow-evolution kernel for Fuwen
workflows: artifacts record knowledge, patches propose deltas against known
bases, deterministic validation owns admission, and a bounded loop governs how
much evolution is allowed and when it stops.

The architectural law:

> Guihua owns reusable workflow planning mechanics. Applications own the
> meaning of the workflows being planned.

Progress lives here so consumers (Guyabano, Marang) can see what the kernel
covers and what still blocks their integration.

## Current baseline — 0.1.0-preview.2

`0.1.0-preview.2` consumes `Penghou.Fuwen` `0.1.0-preview.11`. The kernel was
extracted from Guyabano in `0.1.0-preview.1` and is preview-grade: public
contracts may still change.

Delivered:

- **Kernel (`Penghou.Guihua`)** — planning records and graph
  (`PlanningDesign`, `PlanningGraph`, `PlanningGraphFragment`,
  `PlanningBindings`, `PlanningNodeBinding`, `PlanningStep`), patch mechanics
  (`WorkflowPatch`, `WorkflowPatchApplier`, `PatchPreservationValidator`),
  stages (`PlanningStageCatalogue`, `PlanningStageDefinition`,
  `PlanningStagePlan`, `PlanningStageRunner`, `PlanningStageValidator`),
  artifacts (`IPlanningArtifactCatalog`, `PlanningArtifactCatalog`,
  `ArtifactEnvelope`, `ArtifactReference`, `ArtifactWriteRequest`,
  `PlanningArtifactKey`, `PlanningArtifactVersion`, `PlanningArtifactImpact`,
  `CanonicalJsonContentHash`), the bounded loop (`PlanningLoop`,
  `PlanningLoopPolicy`, `PlanningLoopBudget`, `PlanningLoopOutcome`,
  `PlanningCheckpoint`), decisions (`IPlanningDecider`, `PlanningDecision`,
  `PlanningDecisionResult`, `PlanningAction`), identity
  (`PlanningDesignIdentity`), and graph fragments
  (`PlanningGraphFragmentAssembler`, `PlanningGraphSummary`).
- **Baize adapter (`Penghou.Guihua.Baize`)** — `WorkflowAuthor`,
  `WorkflowPatchProposer`, `PlanningStagePlanProposer`, `LlmPlanningDecider`,
  prompt infrastructure (`PromptBuilderBase`, `IPromptBuilder`,
  `IPromptTemplateEngine`) and embedded prompt packs.
- **Zhinu adapter (`Penghou.Guihua.Zhinu`)** — `ZhinuPlanningExecutionHost`
  hosting planning revisions over durable workflows.

## Consumer integration status

- **Guyabano — integrated.** Consumes `0.1.0-preview.2` across nine projects
  (~76 files): artifact catalog, envelope, repository, stage catalogue and
  runner, prompt builders, planning graph and bindings, staged publisher. It
  drives the kernel; the application stage kinds, prompts, and schemas stay in
  Guyabano.
- **Marang — not integrated.** `docs/roadmap.md` ("Adaptive planning
  supervision direction") is the natural second consumer, but Marang today
  keeps a deliberately sealed `Implement/1` preset plus an opaque Fuwen
  reference (`WorkflowPlanResolution`, `WorkflowPlanReference`) and has no
  Guihua dependency. This is a recorded design boundary, not an oversight.

## Gaps and next work

### A — Marang adaptive-planning supervision seam

Marang's roadmap wants bounded operations to propose a replan, inspect a
transition preview, approve or reject activation, and explain reused or
invalidated work. Mapping those onto the kernel surfaces the following.

1. **Propose replan.** Guihua's proposers are Baize-backed
   (`WorkflowPatchProposer`, `PlanningStagePlanProposer`, `IWorkflowAuthor`).
   A remote supervisor (Codex) proposes out-of-band, but there is no public,
   provider-neutral entry point to *admit an externally supplied
   `WorkflowPatch` against a pinned design*. `WorkflowPatchApplier` and
   `PatchPreservationValidator` exist as mechanics; a bounded admission step
   that returns a receipt is missing.
2. **Inspect transition preview.** `PlanningGraphFragment`,
   `PlanningGraphFragmentAssembler`, `PlanningGraphSummary`, and
   `PlanningArtifactImpact` describe a design or an artifact change. There is
   no explicit two-revision (or plan-vs-candidate) preview descriptor with
   lineage, bounds, and truncation.
3. **Approve or reject activation.** `PlanningAction`
   (Expand/Revise/Validate/Finish) and `IPlanningDecider` cover loop decisions.
   Activation of a *candidate revision* is Fuwen-admission plus Zhinu
   generation-cutover, not Guihua. `IPlanningExecutionHost` /
   `ZhinuPlanningExecutionHost` is the seam, but there is no supervisor-facing
   receipt handoff.
4. **Explain reused or invalidated work.** No explicit reuse/invalidation
   explanation keyed to artifact versions and step provenance exists; the
   closest surfaces are `PlanningArtifactImpact` and
   `PlanningStagePlanProvenance`.

Prerequisite: per Marang's roadmap this work follows Fuwen admission plus plan
comparison and Zhinu's atomic generation-cutover foundation, so it is not
Guihua-only.

### B — Fuwen interoperability hardening

Migrating Guyabano to Fuwen `0.1.0-preview.11` required code changes because
the release was breaking for consumers:

- `FuwenContracts.ExecutionFingerprintVersionV1` → `ExecutionFingerprintVersion`
  and `FuwenContracts.IrVersionV7` → `IrVersion`;
- `WorkflowPlanBuilder.BuildV3/V4/V6/V7` → a single `Build()`;
- inference executors must now implement `IInferenceExecutorPreflight` (or
  `IInferenceExecutorManifest`) or registration throws
  `FuwenZhinuAdmissionException`.

Actions: record consumer-visible breaking changes in the Fuwen changelog,
publish a short consumer migration note per preview, and consider a
compatibility shim or analyzer before RC. The Guihua preview bump to
Fuwen `0.1.0-preview.11` was blocked only by an unpublished upstream package,
so publish order matters (Fuwen → Guihua → consumers).

### C — Engineering health

- **Public API baselines — done.** `Microsoft.CodeAnalysis.PublicApiAnalyzers`
  5.6.0 runs on pack with `PublicAPI.Shipped.txt` (empty during preview) and
  `PublicAPI.Unshipped.txt` per package (core 860, Baize 296, Zhinu 6
  entries). Any new, changed, or removed public API fails the build until the
  baseline is intentionally updated. Promote entries to Shipped only when the
  contract freezes.
- **Coverage thresholds — done.** CI enforces coverage per area via
  `coverlet.msbuild`: core ≥ 70, Baize ≥ 50, Zhinu ≥ 45 (line and branch).
  Current baselines: core 83.9/72.8, Baize 75.8/52.5, Zhinu 87.0/47.7.
- **Sample — done.** `samples/Penghou.Guihua.Sample` walks the public contract
  end to end: design → fingerprint → patch → deterministic assembly → artifact
  catalog with provenance.
- **No benchmarks.** The kernel is not performance-critical; revisit only if a
  consumer shows otherwise.

## Non-goals

Guihua is not a workflow execution engine (Zhinu), an LLM client (Baize), a
memory system (Cangjie), an evidence store (Hongxian/Siming), or a code graph
(Hetu). Application semantics — C4, contracts, code generation, services,
software roles, product artifact schemas — stay in the consumers.

## Next steps

1. **Done** — publish `0.1.0-preview.2` on Fuwen `0.1.0-preview.11` and
   migrate Guyabano (kernel + preflight + build/versioned-plan fixes).
2. **Now** — treat Guihua's `0.1.0-preview.2` as the advisory kernel version:
   Fuwen publishes first, then Guihua, then consumers; record each breaking
   change in the changelog.
3. **Next** — prove Marang as the second consumer only after Fuwen plan
   comparison and Zhinu generation cutover land; then add the provider-neutral
   patch admission/receipt, transition preview, and reuse/invalidation
   explanation in Gap A, keeping MCP transport and authorization in Marang.
4. **Pre-stable** — promote baselines to Shipped when the contract freezes
   (coverage thresholds, sample, and analyzer baselines already landed).

## V2 — Evidence-driven workflow evolution (deferred)

Status: **future work after V1; not a current release gate**. Added 2026-09-26.
V2.1/V2.2/V2.3 name cross-project delivery stages, not package or IR versions.
Existing near-term priorities and completed work retain their current status.

Architecture and shared acceptance gates: [reviewed V2 specification](docs/evidence-driven-workflow-evolution-v2.md).
Cross-repository links assume sibling checkouts.

### V2.1 — Evidence-informed repair proposals

- [ ] Define bounded provider-neutral evidence/decision inputs over current
  planning artifacts, `IPlanningDecider`, patches and preservation validation;
  application policy owns evaluator trust, task classification and acceptance.
- [ ] Distinguish execution failure, rejected output, evaluator failure and
  plan diagnosis. Propose retry, revalidation, replacement or escalation without
  rewriting terminal outcomes or weakening required criteria.
- [ ] Add scoped, versioned avoidance records with equivalent-context rules,
  evidence references, expiry/revalidation and explicit override. Missing or
  changed context must not permanently blacklist an approach.
- [ ] Bind each proposal to exact base revision, evidence snapshot, policy and
  cumulative replan/work budget. Detect repeated repair oscillation and stop
  with an explained unresolved result.
- [ ] Consume Fuwen comparison/admission and Zhinu transition previews/receipts
  through the existing execution-host seam. The current fork adapter and
  process-local tracking are not durable generation-cutover authority.

Prerequisites: existing Gap A receipt/preview work and the corresponding Fuwen/
Zhinu contracts, after current V1 priorities. Gate: a completed-but-rejected
artifact leads to one admitted replacement, preserving unaffected work and all
rationale, including recovery from an interrupted transition.

### V2.2 — Bounded comparative proposals

- [ ] Propose explicit candidate differences, shared constraints, fixed rubric,
  selection/tie/stop policy, isolation requirements and total budget.
- [ ] Compose existing Fuwen constructs first; accept selected, inconclusive,
  none-acceptable and partial outcomes. Selection is committed through runtime/
  host authority, not an unrecorded decider choice.
- [ ] Keep unexecuted alternatives and before/after repair comparisons distinct
  from controlled experimental evidence. Preserve losing and corrective work.

Gate: two isolated candidates yield an explained decision or no winner, and
recovery cannot grant a new budget or silently select again.

### V2.3 — Contextual preference reuse

- [ ] Consume pinned Hongxian recall/aggregate and Cangjie context snapshots
  through host ports, with independent-case counts, freshness, version/scope
  compatibility, contrary evidence and explicit fallback.
- [ ] Skip experiments only under versioned host policy; preserve hard
  authorization, capability, budget and acceptance gates.
- [ ] Record why a preferred strategy/provider was proposed. The host supplies
  the selected provider identity to Qingniao; Baize retains model routing.

Later V2+: topology proposals, attribution, calibration and promotion/demotion
policies require measured need and representative evidence. Guihua remains a
planning kernel, not an evidence database or autonomous runtime.

## Review hardening and implementation handoff — 2026-09-28

The [implementation handoff plan](docs/implementation-handoff-plan.md) turns the
[boundary review](docs/architecture-boundary-review-2026-09-28.md) into bounded
assignments with dependencies, acceptance tests and model handoff instructions.
Status: **planned; no implementation is marked complete by adding this plan**.

| Work | Task IDs | Relationship to existing roadmap |
| --- | --- | --- |
| Baseline, decision validation and call limits | GH-00–GH-02 | Harden the delivered bounded kernel; retain Gap C gates |
| Revision admission and reliable planning state | GH-03–GH-05 | Gap A.1/B plus catalog/checkpoint review findings |
| Typed facts and recoverable activation | GH-07, then GH-06 | Gap A.3/A.4; activation waits for released Fuwen/Zhinu authority |
| Preview, composition, refactoring and release evidence | GH-08–GH-10 | Gap A.2/A.4, second consumer, Gap B/C |

Start with GH-00, then GH-01; GH-04 can run as an independent storage assignment.
The plan records exact dependency ordering; task numbering alone is not ordering.
Keep existing Gap A upstream prerequisites, consumer compatibility and
Fuwen -> Guihua -> consumer publish order. V2 remains deferred and does not
become a V1 gate through this hardening work.

## Review follow-ups — 2026-09-30

- [ ] After GH-03 and a verified Fuwen source-edit contract, prototype
  deterministic syntax-aware patching for supported constructs. Preserve
  untouched source spans and validate the complete admitted plan; keep the
  existing author-and-preservation path for unsupported edits.
- [ ] In GH-03/GH-07, return bounded typed diagnostics with Fuwen rule IDs,
  source locations and target paths where available. Keep concise text for
  model repair, but do not make consumers parse it to locate a failure.
- [ ] In GH-08, offer a bounded Mermaid view of a pinned transition preview
  for reports and review UIs. Escape labels and mark truncated graphs; graph
  output has no authority to approve or activate a revision.
- [ ] Before multiple processes can plan one workflow, add a workflow-scoped
  lease or compare-and-swap catalog head. The in-process run guard prevents
  duplicate runs only on one `PlanningLoop` instance.

`FilePromptLoader` and `IPromptLoader` already support local prompt files. GH-04
retains the broader physical-path and multi-writer storage review; remote
prompt delivery and new storage providers need separate trust and deployment
requirements.

## Pending dependency: Penghou.Hufu

Recorded 2026-09-28. Hufu is currently a scaffold; its authority contracts, store,
and enforcement integration are pending. Plan with Hufu constraints, request missing authority, and handle decisions without self-authorization.
See [the project-specific integration note](docs/hufu-integration.md) for scope, dependencies,
and completion evidence. This records future work without changing current release gates.
