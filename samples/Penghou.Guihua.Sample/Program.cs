// Minimal end-to-end walk through the Penghou.Guihua planning kernel:
// describe a design, fingerprint it, patch it, assemble the next design
// deterministically, and persist an artifact revision. The collaborators an
// application supplies (model-backed author/proposer/decider, a durable
// execution host, artifact storage) are out of scope here; see the core
// tests and the Baize/Zhinu adapters for those.

using Penghou.Fuwen;
using Penghou.Guihua;

// 1. Describe the execution design: two dependent steps and their bindings.
var design = new PlanningDesign(
    new PlanningGraph
    {
        WorkflowName = "sample",
        InputType = "string",
        OutputType = "string",
        Steps =
        [
            new PlanningStep
            {
                Id = "implement_todos",
                Title = "Implement todos",
                DependsOn = [],
                RequiredArtifacts = [],
                AcceptanceCriteria = [],
            },
            new PlanningStep
            {
                Id = "implement_billing",
                Title = "Implement billing",
                DependsOn = ["implement_todos"],
                RequiredArtifacts = ["contracts/billing@1"],
                AcceptanceCriteria = [],
            },
        ],
    },
    new PlanningBindings
    {
        WorkflowName = "sample",
        Nodes =
        [
            Bind("implement_todos", "1", 'a', []),
            Bind("implement_billing", "1", 'a', ["contracts/billing@1"]),
        ],
    });

var fingerprint = PlanningDesignIdentity.Compute(design);
Console.WriteLine($"design fingerprint: {fingerprint}");

// 2. Propose a patch that rebinds billing to the v2 activity descriptor.
//    The patch is pinned to the exact base fingerprint, so a stale patch
//    cannot be applied to a design that has since moved.
var billing = design.Bindings.Nodes.Single(node => node.StepId == "implement_billing");
var patch = new WorkflowPatch
{
    BaseDesignFingerprint = fingerprint,
    DerivedFromArtifacts = ["contracts/billing@2"],
    Rationale = "Use the v2 billing generator.",
    AddSteps = [],
    ReplaceSteps = [],
    RemoveStepIds = [],
    AddBindings = [],
    ReplaceBindings =
    [
        billing with
        {
            Binding = billing.Binding with { Descriptor = Activity("2", 'b') },
        },
    ],
    DependencyEdits = [],
};

// 3. Deterministic assembly. Stale bases, unknown references, orphaned
//    dependencies, cycles, and binding gaps all throw instead of guessing.
var updated = WorkflowPatchApplier.Apply(design, patch);
Console.WriteLine($"updated fingerprint: {PlanningDesignIdentity.Compute(updated)}");
Console.WriteLine(
    "billing descriptor: " +
    updated.Bindings.Nodes.Single(node => node.StepId == "implement_billing")
        .Binding.Descriptor.Version);

// 4. Content-addressed artifact catalog with provenance.
var root = Path.Combine(
    Path.GetTempPath(),
    "penghou-guihua-sample",
    Guid.NewGuid().ToString("N"));
try
{
    var catalog = new PlanningArtifactCatalog(new FileSystemArtifactRepository(root));
    var contracts = new PlanningArtifactKey("contracts", "billing");
    var published = await catalog.PublishAsync(
        new PublishPlanningArtifactRequest<string>(
            "sample-workflow",
            contracts,
            SchemaVersion: 1,
            ProducedBy: "sample",
            Payload: "billing-v2"));
    Console.WriteLine($"published {published.Key.Kind}/{published.Key.Name}@{published.Revision}");

    var current = await catalog.GetCurrentAsync("sample-workflow", contracts);
    var payload = current is null
        ? "<missing>"
        : await catalog.ReadPayloadAsync<string>(current);
    Console.WriteLine($"current payload: {payload}");
}
finally
{
    if (Directory.Exists(root))
        Directory.Delete(root, recursive: true);
}

static PlanningNodeBinding Bind(
    string stepId,
    string version,
    char digest,
    string[] contextArtifacts) => new()
    {
        StepId = stepId,
        Binding = new PlanningBinding
        {
            Role = "implement",
            Capability = "code.modify",
            ModelProfile = "implementation",
            ContextArtifacts = contextArtifacts,
            Descriptor = Activity(version, digest),
        },
    };

static DescriptorReference Activity(string version, char digest) =>
    new(
        DescriptorKind.Activity,
        "sample.execute",
        version,
        new ContentDigest("sha256", "descriptor/v1", new string(digest, 64)));
