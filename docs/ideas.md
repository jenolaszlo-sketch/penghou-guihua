# Guihua ideas to revisit

Suggestions from the 2026-09-30 review that need a concrete consumer or an
upstream contract before implementation:

- **Remote prompt registry.** `IPromptLoader` and `FilePromptLoader` already
  permit local customization. Remote packs would need version pinning,
  integrity, authorization, and an audit of which prompt was used for each
  admitted revision.
- **Cloud artifact adapters.** A SQLite or object-storage implementation of
  `IArtifactRepository` is plausible after GH-04 defines an authoritative
  catalog head and recovery semantics. A second backend alone would not make
  concurrent planners safe.
- **Guihua planning CLI.** A playground may help once the provider-neutral
  admission and preview APIs stabilize. The existing sample is the cheaper
  integration proof for now.
- **Automatic patch rollback.** Reversing a DSL file cannot undo external
  effects from a running Zhinu workflow. Keep immutable revisions and explicit
  host-controlled transition or compensation receipts; revisit a rollback
  command only when its effect boundary is defined.
- **Guihua-owned interactive approval host.** Approval is useful, but the
  application owns caller identity, UI, and activation authority. GH-08 can
  expose a pinned preview and approval requirement without another host class
  in the kernel.
