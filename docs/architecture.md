# Guihua architecture

## Guihua is

```text
A reusable planning and workflow-evolution kernel for Fuwen workflows.
```

Planning is the progressive reduction of uncertainty into executable
structure: artifacts record knowledge, patches propose deltas against known
bases, deterministic validation owns admission, and a bounded loop governs
how much evolution is allowed and when it stops.

## Guihua is not

```text
a coding agent
a software architecture framework
a workflow execution engine
an LLM client
a memory system
an evidence store
```

Those responsibilities belong elsewhere in Penghou.

## Relationships

```text
Baize     generates
Guihua    plans and evolves
Fuwen     represents
Zhinu     executes
Hongxian  records
Cangjie   remembers
```

- **Baize** provides generative model access. Guihua proposes text through
  Baize-backed authors, proposers, and deciders; validation owns admission.
- **Fuwen** provides the workflow IR, compilation, admission, and plan
  identity. Guihua operates on immutable `WorkflowPlan` records and never
  invents workflow semantics.
- **Zhinu** provides durable execution, replay, and versioned mutation.
  Guihua's host adapter forks revisions and collects evidence; workflows
  never rewrite themselves.
- A future decision layer may provide bounded or statistical judgment
  through the `IPlanningDecider` abstraction without changing Guihua's
  core planning abstractions.

## The boundary

> Guihua owns reusable workflow planning mechanics. Applications own the
> meaning of the workflows being planned.

Concretely: Guihua knows steps, bindings, dependencies, patches,
fingerprints, budgets, checkpoints, and outcomes. It does not know C4,
contracts, code generation, services, software roles, or any product's
artifact schemas. Applications (Guyabano, Marang) register their own stage
definitions, prompts, executors, and policies on top of these mechanics.
