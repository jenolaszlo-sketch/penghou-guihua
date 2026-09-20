using Penghou.Fuwen;

namespace Penghou.Guihua;

/// <summary>Compiles Fuwen DSL text into plans. The loop never touches compiler types directly.</summary>
public interface IPlanningDslCompiler
{
    Task<WorkflowPlan?> CompileAsync(string dsl, CancellationToken cancellationToken = default);
}

/// <summary>One patch proposal: the delta plus its deterministic assembly.</summary>
public sealed record ProposedRevision(
    bool Succeeded,
    WorkflowPatch? Patch,
    PlanningDesign? Applied,
    int ModelCalls,
    IReadOnlyList<string> Diagnostics);

/// <summary>Proposes workflow deltas against known design bases.</summary>
public interface IWorkflowPatchProposer
{
    Task<ProposedRevision> ProposeAsync(
        string goal,
        PlanningDesign current,
        string executionPlan,
        IReadOnlyList<string> changedArtifacts,
        string catalogueSummary,
        string model,
        int maxTokens = 4000,
        CancellationToken cancellationToken = default);
}

/// <summary>One authored revision: admitted DSL plus its plan and cost.</summary>
public sealed record AuthoredRevision(
    bool Succeeded,
    string Dsl,
    WorkflowPlan? Plan,
    int ModelCalls,
    IReadOnlyList<string> Diagnostics);

/// <summary>Authors executable revisions from merged designs.</summary>
public interface IWorkflowAuthor
{
    Task<AuthoredRevision> AuthorFromPatchAsync(
        string goal,
        string executionPlan,
        string priorDsl,
        WorkflowPlan priorPlan,
        WorkflowPatch patch,
        string catalogueSummary,
        string model,
        int maxTokens = 4000,
        CancellationToken cancellationToken = default);
}
