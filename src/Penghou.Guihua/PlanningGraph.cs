using System.Text.Json.Serialization;
using Penghou.Fuwen;

namespace Penghou.Guihua;

/// <summary>
/// One semantic execution step: what to do and what it depends on. The step
/// knows nothing provider-specific; <see cref="PlanningBinding"/>
/// records how it is performed. Fuwen identifiers are safe: <see cref="Id"/>
/// uses lowercase letters, digits, and underscores only.
/// </summary>
public sealed record PlanningStep
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("dependsOn")]
    public required IReadOnlyList<string> DependsOn { get; init; }

    [JsonPropertyName("requiredArtifacts")]
    public required IReadOnlyList<string> RequiredArtifacts { get; init; }

    [JsonPropertyName("acceptanceCriteria")]
    public required IReadOnlyList<string> AcceptanceCriteria { get; init; }
}

/// <summary>
/// How one execution step is performed: capability, model profile, pinned
/// context artifacts, and the resolved descriptor reference the Fuwen author
/// must use verbatim.
/// </summary>
public sealed record PlanningBinding
{
    [JsonPropertyName("role")]
    public required string Role { get; init; }

    [JsonPropertyName("capability")]
    public required string Capability { get; init; }

    [JsonPropertyName("modelProfile")]
    public required string ModelProfile { get; init; }

    [JsonPropertyName("contextArtifacts")]
    public required IReadOnlyList<string> ContextArtifacts { get; init; }

    [JsonPropertyName("descriptor")]
    public required DescriptorReference Descriptor { get; init; }
}

/// <summary>One step's binding, keyed by step id.</summary>
public sealed record PlanningNodeBinding
{
    [JsonPropertyName("stepId")]
    public required string StepId { get; init; }

    [JsonPropertyName("binding")]
    public required PlanningBinding Binding { get; init; }
}

/// <summary>Semantic execution graph: executable work and its dependencies.</summary>
public sealed record PlanningGraph
{
    [JsonPropertyName("workflowName")]
    public required string WorkflowName { get; init; }

    [JsonPropertyName("inputType")]
    public required string InputType { get; init; }

    [JsonPropertyName("outputType")]
    public required string OutputType { get; init; }

    [JsonPropertyName("steps")]
    public required IReadOnlyList<PlanningStep> Steps { get; init; }
}

/// <summary>Per-step bindings for one execution graph.</summary>
public sealed record PlanningBindings
{
    [JsonPropertyName("workflowName")]
    public required string WorkflowName { get; init; }

    [JsonPropertyName("nodes")]
    public required IReadOnlyList<PlanningNodeBinding> Nodes { get; init; }
}

/// <summary>Execution design: the semantic graph plus its bindings.</summary>
public sealed record PlanningDesign(
    PlanningGraph Graph,
    PlanningBindings Bindings);
