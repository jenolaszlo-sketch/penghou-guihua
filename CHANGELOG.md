# Changelog

Notable changes to Penghou.Guihua are recorded here. The project follows
[Semantic Versioning](https://semver.org/) for package versions. Preview
releases may still revise public contracts.

## 0.1.0-preview.2

- Consume `Penghou.Fuwen` `0.1.0-preview.11` across the core, Baize, and Zhinu
  packages (aligned with the Fuwen complex-inference line).

## 0.1.0-preview.1

- Initial extraction of the planning and workflow-evolution kernel from
  Guyabano: planning records, patches, preservation validation, stage
  definitions, graphs, artifact identity/provenance, bounded loops, and
  decisions (`Penghou.Guihua`).
- Model-backed workflow authoring, patch proposing, and planning decisions
  (`Penghou.Guihua.Baize`).
- Durable planning-revision execution over Zhinu workflows
  (`Penghou.Guihua.Zhinu`).
