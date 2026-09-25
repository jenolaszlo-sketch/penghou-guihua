# Penghou Guihua

[![CI](https://github.com/jenolaszlo-sketch/penghou-guihua/actions/workflows/ci.yml/badge.svg)](https://github.com/jenolaszlo-sketch/penghou-guihua/actions/workflows/ci.yml)
[![License](https://img.shields.io/github/license/jenolaszlo-sketch/penghou-guihua)](LICENSE)

Guihua is the reusable planning and workflow-evolution kernel for Fuwen workflows.

```text
Baize     generates
Guihua    plans and evolves
Fuwen     represents
Zhinu     executes
Hongxian  records
Cangjie   remembers
```

## Packages

- `Penghou.Guihua` — pure planning records, validation, mutation, graph
  description, artifact identity, provenance, and planning state. Depends
  only on `Penghou.Fuwen`.
- `Penghou.Guihua.Baize` — model-backed workflow authoring and decision
  implementations, repair loops, and generic prompt packs.
- `Penghou.Guihua.Zhinu` — durable execution integration for planning
  revisions over Zhinu workflows.

See `docs/architecture.md` for what Guihua is and is not, and how
applications (Guyabano, Marang) consume it. See `ROADMAP.md` for delivery
status and `samples/Penghou.Guihua.Sample` for a runnable walk through the
kernel (design → patch → catalog).
