using System.Text.Json;
using FluentAssertions;
using Penghou.Baize;
using Penghou.Baize.Router;
using Penghou.Fuwen;
using Penghou.Guihua;
using Penghou.Guihua.Baize;

namespace Penghou.Guihua.Baize.Tests;

public sealed class LlmPlanningDeciderTests
{
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
            ],
        },
        new PlanningBindings
        {
            WorkflowName = "implementation",
            Nodes =
            [
                new PlanningNodeBinding
                {
                    StepId = "implement_a",
                    Binding = new PlanningBinding
                    {
                        Role = "implement",
                        Capability = "code.modify",
                        ModelProfile = "implementation",
                        ContextArtifacts = [],
                        Descriptor = new DescriptorReference(
                            DescriptorKind.Activity, "guihua.execute", "1",
                            new ContentDigest("sha256", "descriptor/v1", new string('a', 64))),
                    },
                },
            ],
        });

    [Fact]
    public async Task Decision_pack_renders_the_observation()
    {
        var ct = TestContext.Current.CancellationToken;
        var design = Design();
        var fingerprint = PlanningDesignIdentity.Compute(design);

        var builder = new PlanningDecisionPromptBuilder(
            new ScribanPromptTemplateEngine(new EmbeddedPromptLoader()));
        var request = await builder.BuildAsync(
            new PlanningDecisionPromptContext(
                "Use the v2 generator.",
                PlanningGraphSummary.Render(design),
                fingerprint,
                "v1",
                false,
                "nothing executed yet",
                ["contracts/billing@2"],
                ["contracts/billing@2 supersedes pinned contracts/billing@1"],
                9, 5, 8, 29, null, 2000),
            ct);

        var systemText = TextOf(request, "system");
        var userText = TextOf(request, "user");
        systemText.Should().Contain(fingerprint);
        systemText.Should().Contain("contracts/billing@2");
        systemText.Should().Contain("supersedes pinned contracts/billing@1");
        systemText.Should().Contain("produceStages");
        systemText.Should().Contain("Iterations: 9");
        userText.Should().Contain("Use the v2 generator.");
        userText.Should().Contain("exactly one JSON object");
    }

    [Fact]
    public async Task Llm_decider_parses_a_decision()
    {
        var ct = TestContext.Current.CancellationToken;
        var design = Design();
        var fingerprint = PlanningDesignIdentity.Compute(design);
        var decision = new PlanningDecision
        {
            DesignFingerprint = fingerprint,
            ArtifactRevisions = ["contracts/billing@2"],
            WorkflowVersion = "v1",
            Action = PlanningAction.Expand,
            MotivatingArtifacts = ["contracts/billing@2"],
            ProduceStages = [],
            Rationale = "Absorb the revision.",
        };

        var router = new ScriptedRouter([JsonSerializer.Serialize(decision)]);
        var decider = new LlmPlanningDecider(
            router,
            new PlanningDecisionPromptBuilder(
                new ScribanPromptTemplateEngine(new EmbeddedPromptLoader())),
            "stub-decider");
        var observation = new PlanningDecisionContext(
            "Use the v2 generator.",
            PlanningGraphSummary.Render(design),
            fingerprint,
            ["contracts/billing@2"],
            ["contracts/billing@2 supersedes pinned contracts/billing@1"],
            "v1",
            false,
            "nothing executed yet",
            new PlanningLoopBudget(9, 5, 8, 29, null),
            null,
            null);

        var result = await decider.DecideAsync(observation, ct);

        result.Succeeded.Should().BeTrue(string.Join("; ", result.Diagnostics));
        result.Decision!.Action.Should().Be(PlanningAction.Expand);
        result.ModelCalls.Should().Be(1);
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
