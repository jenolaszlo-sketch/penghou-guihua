# Penghou.Guihua: pending Penghou.Hufu integration

Status: **Pending integration; not implemented.** Recorded 2026-09-28.

Penghou.Hufu is the new reusable authority library and authority-store boundary.
It currently contains a buildable scaffold and design documents, with no public
authority API, enforcement implementation, or persistent authority store.
This note records future consumer work; it does not announce a package dependency,
a shipped security guarantee, or an additional current-release acceptance gate.

Hufu will own reusable grants, envelopes, authority requests and decisions,
attenuation, revocation, and durable authority records. Hosts retain identity,
policy, credentials, resource resolution, and approval surfaces. Zhinu retains
execution state and recovery; existing budget services retain accounting.

## Guihua's planned integration

- Provide trusted activity requirements and available/requestable authority to
  planning, while treating model rationale as an untrusted explanation.
- Preserve typed authority requirements in proposed Fuwen revisions and compare
  required authority and consequential effects with current host authorization.
- Produce structured, durable authority requests through Hufu/host contracts,
  bound to exact proposed revisions and attributable evidence.
- Feed denial and partial-grant outcomes into genuinely compliant alternatives;
  keep proposals separate from admitted and activated work.

Guihua proposes plans and requests; it cannot issue grants, approve its own
requests, activate a revision, or own the authority/execution store. An unchanged
capability set may still need a new effect decision. A denied operation must not
be re-expressed through CI or another delegate to obtain the same prohibited effect.

## Completion evidence

Missing authority stops affected work before I/O. Granted decisions bind the
exact proposal; stale decisions do not activate newer output. A denial can lead
to a valid alternative inside existing authorization without an approval loop.

## Dependency and design home

Implementation depends on Hufu's reviewed contracts, durable-store semantics,
and a proven host/resource-broker enforcement path. Continue current correctness
work independently; do not add placeholder dependencies or infer security from
the existence of Hufu's scaffold.

Canonical design (links assume sibling checkouts):

- [Hufu architecture](../../Penghou.Hufu/docs/architecture.md)
- [Authority specification](../../Penghou.Hufu/docs/workflow-authority-spec.md)
- [Hufu implementation roadmap](../../Penghou.Hufu/docs/roadmap.md)
