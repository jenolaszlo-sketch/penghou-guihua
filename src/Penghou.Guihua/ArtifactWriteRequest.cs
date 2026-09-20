namespace Penghou.Guihua;

public sealed record ArtifactWriteRequest<TPayload>(
    string WorkflowId,
    string Kind,
    int SchemaVersion,
    string StageKey,
    ArtifactStatus Status,
    TPayload Payload,
    IReadOnlyList<ArtifactReference>? Inputs = null)
{
    public string? SessionId { get; init; }
}
