#pragma warning disable xUnit1030
using System.Text.Json;
using FluentAssertions;
using Penghou.Baize;
using Penghou.Baize.Router;
using Penghou.Fuwen;
using Penghou.Fuwen.Compiler;
using Penghou.Guihua;
using Penghou.Guihua.Baize;

namespace Penghou.Guihua.Baize.Tests;

/// <summary>
/// Model-backed authoring against scripted routers: pack rendering, plan
/// translation admission, patch repair, exhaustion, and decision parsing.
/// </summary>
public sealed class WorkflowAuthorTests
{
    private static string Digest(char c) => new string(c, 64);

    private static ContentDigest Cdigest(char c) => new("sha256", "descriptor/v1", Digest(c));

    private static DescriptorReference ActivityRef(string version, char digest) =>
        new(DescriptorKind.Activity, "guihua.execute", version, Cdigest(digest));

    private static ITrustedCatalogue Catalogue()
    {
        var str = new PrimitiveType(FuwenPrimitiveKind.String);
        TrustedCatalogueDescriptor Descriptor(string version, char digest) => new(
            ActivityRef(version, digest),
            callableContract: new CallableContract(
                new CallableSignature([new CallableParameter("task", str)], str),
                CallableEffect.Read, CallableIdempotency.Idempotent, CallableRetrySafety.Safe));
        return new InMemoryTrustedCatalogue([Descriptor("1", 'a'), Descriptor("2", 'b')]);
    }

    private static PlanningDesign Design() => new(
        new PlanningGraph
        {
            WorkflowName = "implementation",
            InputType = "string",
            OutputType = "string",
            Steps =
            [
                new PlanningStep
                {
                    Id = "implement_a",
                    Title = "Implement todos",
                    DependsOn = [],
                    RequiredArtifacts = [],
                    AcceptanceCriteria = [],
                },
                new PlanningStep
                {
                    Id = "implement_billing",
                    Title = "Implement billing",
                    DependsOn = ["implement_a"],
                    RequiredArtifacts = [],
                    AcceptanceCriteria = [],
                },
            ],
        },
        new PlanningBindings
        {
            WorkflowName = "implementation",
            Nodes =
            [
                NodeBinding("implement_a", "1", 'a'),
                NodeBinding("implement_billing", "1", 'a'),
            ],
        });

    private static PlanningNodeBinding NodeBinding(string stepId, string version, char digest) => new()
    {
        StepId = stepId,
        Binding = new PlanningBinding
        {
            Role = "implement",
            Capability = "code.modify",
            ModelProfile = "implementation",
            ContextArtifacts = [],
            Descriptor = ActivityRef(version, digest),
        },
    };

    private static string ActivityLine(string node, string version, char digest, string task) =>
        $"  activity {node} = activity \"guihua.execute@{version}#{Digest(digest)}\" (task: {task};) -> string;";

    private static string PriorDsl() =>
        "workflow implementation(input: string) -> string {\n" +
        ActivityLine("implement_a", "1", 'a', "input") + "\n" +
        ActivityLine("implement_billing", "1", 'a', "implement_a") + "\n" +
        "  return implement_billing;\n" +
        "}";

    private static string CleanDsl() =>
        "workflow implementation(input: string) -> string {\n" +
        ActivityLine("implement_a", "1", 'a', "input") + "\n" +
        ActivityLine("implement_billing", "2", 'b', "implement_a") + "\n" +
        "  return implement_billing;\n" +
        "}";

    private static string DriftedDsl() => CleanDsl().Replace(
        "(task: input;)", "(task: \"classify\";)", StringComparison.Ordinal);

    private static WorkflowPatch BillingV2Patch(PlanningDesign design)
    {
        var billing = design.Bindings.Nodes.Single(node => node.StepId == "implement_billing");
        return new WorkflowPatch
        {
            BaseDesignFingerprint = PlanningDesignIdentity.Compute(design),
            DerivedFromArtifacts = ["contracts/billing@2"],
            Rationale = "Use the v2 executor for billing.",
            AddSteps = [],
            ReplaceSteps = [],
            RemoveStepIds = [],
            AddBindings = [],
            ReplaceBindings =
            [
                billing with
                {
                    Binding = billing.Binding with { Descriptor = ActivityRef("2", 'b') },
                },
            ],
            DependencyEdits = [],
        };
    }

    private static async Task<WorkflowPlan> CompileAsync(ITrustedCatalogue catalogue, string dsl)
    {
        var ct = TestContext.Current.CancellationToken;
        var compiled = await new FuwenSourceCompiler(catalogue)
            .CompileAsync(dsl, cancellationToken: ct);
        compiled.Succeeded.Should().BeTrue(
            string.Join("; ", compiled.Diagnostics.Select(d => $"{d.Code}:{d.Message} path:{d.Path}")));
        return compiled.Plan!;
    }

    [Fact]
    public async Task Authoring_pack_renders_the_resolved_execution_plan()
    {
        var ct = TestContext.Current.CancellationToken;
        var design = Design();
        var builder = new WorkflowAuthoringPromptBuilder(
            new ScribanPromptTemplateEngine(new EmbeddedPromptLoader()));
        var request = await builder.BuildAsync(
            new WorkflowAuthoringPromptContext(
                "Implement ticket classification.",
                "activity guihua.execute@1#aaa",
                4000,
                ExecutionPlan: PlanningGraphSummary.Render(design)),
            ct);

        var systemText = TextOf(request, "system");
        var userText = TextOf(request, "user");
        systemText.Should().Contain("Resolved execution plan");
        systemText.Should().Contain("implement_billing");
        systemText.Should().Contain("do not add steps");
        systemText.Should().Contain("never starts a node");
        systemText.Should().Contain("caps model output tokens");
        userText.Should().Contain("preserve every step");
    }

    [Fact]
    public async Task Author_from_plan_admits_a_translation()
    {
        var ct = TestContext.Current.CancellationToken;
        var design = Design();
        var catalogue = Catalogue();
        var router = new ScriptedRouter([PriorDsl()]);
        var author = new WorkflowAuthor(
            router,
            new WorkflowAuthoringPromptBuilder(
                new ScribanPromptTemplateEngine(new EmbeddedPromptLoader())),
            catalogue,
            maxAttempts: 3);

        var result = await author.AuthorFromPlanAsync(
            "Implement ticket classification.",
            PlanningGraphSummary.Render(design),
            "activity guihua.execute@1#aaa",
            "stub-author",
            4000,
            ct);

        result.Succeeded.Should().BeTrue(string.Join("; ", result.Diagnostics));
        result.Admission.Should().NotBeNull();
        var nodes = result.Admission!.Compilation.Definition!.ReadPlan().Nodes
            .OfType<ActivityNode>()
            .ToDictionary(node => node.Name, StringComparer.Ordinal);
        nodes["implement_billing"].Activity.Version.Should().Be("1");
    }

    [Fact]
    public async Task Author_from_patch_repairs_drift_and_admits()
    {
        var ct = TestContext.Current.CancellationToken;
        var design = Design();
        var catalogue = Catalogue();
        var priorPlan = await CompileAsync(catalogue, PriorDsl());
        var patch = BillingV2Patch(design);
        var merged = WorkflowPatchApplier.Apply(design, patch);
        var router = new ScriptedRouter([DriftedDsl(), CleanDsl()]);
        var author = new WorkflowAuthor(
            router,
            new WorkflowAuthoringPromptBuilder(
                new ScribanPromptTemplateEngine(new EmbeddedPromptLoader())),
            catalogue,
            maxAttempts: 3);

        var result = await author.AuthorFromPatchAsync(
            "Implement ticket classification.",
            PlanningGraphSummary.Render(merged),
            PriorDsl(),
            priorPlan,
            patch,
            "activity guihua.execute@1#aaa",
            "stub-author",
            4000,
            ct);

        result.Succeeded.Should().BeTrue(string.Join("; ", result.Diagnostics));
        result.Attempts.Should().HaveCount(2);
        result.Attempts[0].Diagnostics.Should().ContainSingle()
            .Which.Should().Contain("outside the patch scope");
        TextOf(router.Requests[1], "user").Should().Contain("outside the patch scope");
        var nodes = result.Admission!.Compilation.Definition!.ReadPlan().Nodes
            .OfType<ActivityNode>()
            .ToDictionary(node => node.Name, StringComparer.Ordinal);
        nodes["implement_billing"].Activity.Version.Should().Be("2");
    }

    [Fact]
    public async Task Author_from_patch_fails_when_drift_persists()
    {
        var ct = TestContext.Current.CancellationToken;
        var design = Design();
        var catalogue = Catalogue();
        var priorPlan = await CompileAsync(catalogue, PriorDsl());
        var patch = BillingV2Patch(design);
        var merged = WorkflowPatchApplier.Apply(design, patch);
        var router = new ScriptedRouter([DriftedDsl(), DriftedDsl()]);
        var author = new WorkflowAuthor(
            router,
            new WorkflowAuthoringPromptBuilder(
                new ScribanPromptTemplateEngine(new EmbeddedPromptLoader())),
            catalogue,
            maxAttempts: 2);

        var result = await author.AuthorFromPatchAsync(
            "Implement ticket classification.",
            PlanningGraphSummary.Render(merged),
            PriorDsl(),
            priorPlan,
            patch,
            "activity guihua.execute@1#aaa",
            "stub-author",
            4000,
            ct);

        result.Succeeded.Should().BeFalse();
        result.Attempts.Should().HaveCount(2);
        result.Attempts.Should().OnlyContain(attempt => attempt.Admitted);
    }

    private static string TextOf(LlmRequest request, string role) => string.Concat(request.Messages
        .Where(m => m.Role == role)
        .SelectMany(m => m.Parts)
        .OfType<LlmTextContent>()
        .Select(p => p.Text));

    private sealed class ScriptedRouter(IReadOnlyList<string> script) : ILlmRouter
    {
        private int next;
        public List<LlmRequest> Requests { get; } = [];

        public IAsyncEnumerable<LlmStreamEvent> StreamAsync(
            string model, LlmRequest request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            var index = Math.Min(next, script.Count - 1);
            next++;
            return StreamSingle(script[index]);
        }

        public IAsyncEnumerable<LlmStreamEvent> StreamAsync(
            ModelStrategy strategy, LlmRequest request, CancellationToken cancellationToken) =>
            StreamAsync(strategy.ToString(), request, cancellationToken);

        public IAsyncEnumerable<LlmStreamEvent> StreamAsync(
            string model, ILlmPromptBuilder builder, CancellationToken cancellationToken) =>
            StreamAsync(model, builder.Build(ModelStrategy.Auto), cancellationToken);

        public IAsyncEnumerable<LlmStreamEvent> StreamAsync(
            ModelStrategy strategy, ILlmPromptBuilder builder, CancellationToken cancellationToken) =>
            StreamAsync(strategy.ToString(), builder, cancellationToken);

        public IAsyncEnumerable<LlmStreamEvent> StreamRouteAsync(
            string route, ILlmPromptBuilder builder, CancellationToken cancellationToken) =>
            StreamAsync(route, builder, cancellationToken);

        public IAsyncEnumerable<LlmStreamEvent> StreamRouteAsync(
            string route, LlmRequest request, CancellationToken cancellationToken) =>
            StreamAsync(route, request, cancellationToken);

        public ResolvedEndpoint Resolve(string model) => throw new NotImplementedException();
        public Task<ResolvedEndpoint> ResolveAsync(string model, CancellationToken cancellationToken) => throw new NotImplementedException();
        public ResolvedEndpoint Resolve(ModelStrategy strategy) => throw new NotImplementedException();
        public Task<ResolvedEndpoint> ResolveAsync(ModelStrategy strategy, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<ResolvedEndpoint> ResolveRouteAsync(string route, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<LlmRouteExplanation> ExplainModelAsync(string model, LlmRequest? request = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<LlmRouteExplanation> ExplainStrategyAsync(ModelStrategy strategy, LlmRequest? request = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<LlmRouteExplanation> ExplainRouteAsync(string route, LlmRequest? request = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        private static async IAsyncEnumerable<LlmStreamEvent> StreamSingle(
            string delta,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            await Task.Yield();
            yield return new LlmStreamEvent(delta, null, "stop", null, null, null, null, null, null);
        }
    }
}
