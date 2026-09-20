#pragma warning disable xUnit1030
using System.Text.Json;
using FluentAssertions;
using Penghou.Fuwen;
using Penghou.Fuwen.Compiler;

namespace Penghou.Guihua.Tests;

/// <summary>
/// Planning loop mechanics with scripted collaborators: sequencing, bounds,
/// checkpoints, resume, thrash, staleness, repair, and stage handoff. Model
/// integration lives in the Baize tests.
/// </summary>
public sealed class PlanningLoopTests : IDisposable
{
    private const string WorkflowId = "loop-1";
    private const string Goal = "Use the v2 generator.";

    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "penghou-guihua-loop-tests",
        Guid.NewGuid().ToString("N"));

    private static DescriptorReference ActivityRef(string version, char digest) =>
        new(DescriptorKind.Activity, "guihua.execute", version,
            new ContentDigest("sha256", "descriptor/v1", new string(digest, 64)));

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
                    RequiredArtifacts = ["contracts/billing@1"],
                    AcceptanceCriteria = [],
                },
            ],
        },
        new PlanningBindings
        {
            WorkflowName = "implementation",
            Nodes =
            [
                NodeBinding("implement_a", "1", 'a', []),
                NodeBinding("implement_billing", "1", 'a', ["contracts/billing@1"]),
            ],
        });

    private static PlanningNodeBinding NodeBinding(
        string stepId, string version, char digest, string[] artifacts) => new()
        {
            StepId = stepId,
            Binding = new PlanningBinding
            {
                Role = "implement",
                Capability = "code.modify",
                ModelProfile = "implementation",
                ContextArtifacts = artifacts,
                Descriptor = ActivityRef(version, digest),
            },
        };

    private static WorkflowPlan EmptyPlan()
    {
        var str = new PrimitiveType(FuwenPrimitiveKind.String);
        return new WorkflowPlanBuilder("demo", "1", str, str, "routing/1")
            .AddNode(new ActivityNode(
                "implement_a",
                StructuralNodeIdentity.Create("demo", "implement_a"),
                ActivityRef("1", 'a'),
                [],
                str))
            .AddNode(new ReturnNode(
                "return_result",
                StructuralNodeIdentity.Create("demo", "return_result"),
                new NodeOutputBinding(StructuralNodeIdentity.Create("demo", "implement_a"), [])))
            .SetExecutionOrder(new WorkflowExecutionOrder([
                new WorkflowExecutionRegion("demo", [
                    new WorkflowExecutionPhase([StructuralNodeIdentity.Create("demo", "implement_a")]),
                    new WorkflowExecutionPhase([StructuralNodeIdentity.Create("demo", "return_result")]),
                ]),
            ]))
            .Build();
    }

    private static WorkflowPatch BillingV2Patch(PlanningDesign design)
    {
        var billing = design.Bindings.Nodes.Single(node => node.StepId == "implement_billing");
        return new WorkflowPatch
        {
            BaseDesignFingerprint = PlanningDesignIdentity.Compute(design),
            DerivedFromArtifacts = ["contracts/billing@2"],
            Rationale = "Use the v2 generator for billing.",
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

    private static WorkflowPatch EmptyPatch(PlanningDesign design) => new()
    {
        BaseDesignFingerprint = PlanningDesignIdentity.Compute(design),
        DerivedFromArtifacts = ["contracts/billing@2"],
        Rationale = "No workflow change.",
        AddSteps = [],
        ReplaceSteps = [],
        RemoveStepIds = [],
        AddBindings = [],
        ReplaceBindings = [],
        DependencyEdits = [],
    };

    private static PlanningDecision ExpandFor(
        PlanningDecisionContext observation, params string[] motivating) => new()
        {
            DesignFingerprint = observation.DesignFingerprint,
            ArtifactRevisions = observation.FreshArtifactRevisions,
            WorkflowVersion = observation.WorkflowVersion,
            Action = PlanningAction.Expand,
            MotivatingArtifacts = motivating,
            ProduceStages = [],
            Rationale = "Absorb the revision.",
        };

    private static PlanningDecision FinishFor(PlanningDecisionContext observation, string reason) => new()
    {
        DesignFingerprint = observation.DesignFingerprint,
        ArtifactRevisions = observation.FreshArtifactRevisions,
        WorkflowVersion = observation.WorkflowVersion,
        Action = PlanningAction.Finish,
        MotivatingArtifacts = [],
        ProduceStages = [],
        Rationale = "Goal met.",
        FinishReason = reason,
    };

    [Fact]
    public async Task Loop_expands_once_then_finishes()
    {
        var ct = TestContext.Current.CancellationToken;
        var design = Design();
        var harness = await CreateHarnessAsync(ct);
        var decider = new ScriptedDecider(
        [
            obs => ExpandFor(obs, "contracts/billing@2"),
            obs => FinishFor(obs, "Billing uses v2; goal met."),
        ]);
        var loop = harness.Loop(
            decider,
            (current, _) => (BillingV2Patch(current), WorkflowPatchApplier.Apply(current, BillingV2Patch(current))),
            (_, _, _) => ("dsl-v2", EmptyPlan()));

        var outcome = await loop.RunAsync(
            WorkflowId, Goal, design, "prior-dsl", "catalogue summary", "stub", 4000, ct);

        outcome.Status.Should().Be(PlanningLoopStatus.Finished);
        outcome.Reason.Should().Contain("goal met");
        outcome.Checkpoint.Mutations.Should().Be(1);
        outcome.Checkpoint.StructuralIterations.Should().Be(1);
        outcome.Checkpoint.ModelCalls.Should().Be(4);
        outcome.Checkpoint.Iteration.Should().Be(1);
        outcome.Checkpoint.DecisionLog.Should().HaveCount(2);
        outcome.FinalDesign.Bindings.Nodes.Single(node => node.StepId == "implement_billing")
            .Binding.Descriptor.Version.Should().Be("2");
        harness.Host.Executions.Should().Be(1);

        decider.Observations.Should().HaveCount(2);
        var first = decider.Observations[0];
        first.FreshArtifactRevisions.Should().ContainSingle()
            .Which.Should().Be("contracts/billing@2");
        first.SupersededPins.Should().ContainSingle()
            .Which.Should().Be("contracts/billing@2 supersedes pinned contracts/billing@1");
    }

    [Fact]
    public async Task Loop_stops_at_max_iterations()
    {
        var ct = TestContext.Current.CancellationToken;
        var design = Design();
        var harness = await CreateHarnessAsync(ct, PlanningLoopPolicy.Default with { MaxIterations = 1 });
        var decider = new ScriptedDecider([obs => ExpandFor(obs, "contracts/billing@2")]);
        var loop = harness.Loop(
            decider,
            (current, _) => (BillingV2Patch(current), WorkflowPatchApplier.Apply(current, BillingV2Patch(current))),
            (_, _, _) => ("dsl-v2", EmptyPlan()));

        var outcome = await loop.RunAsync(
            WorkflowId, Goal, design, "prior-dsl", "catalogue summary", "stub", 4000, ct);

        outcome.Status.Should().Be(PlanningLoopStatus.Exhausted);
        outcome.Reason.Should().Contain("Max iterations");
        outcome.Checkpoint.Mutations.Should().Be(1);
        harness.Host.Executions.Should().Be(1);
    }

    [Fact]
    public async Task Loop_converges_on_consecutive_no_ops()
    {
        var ct = TestContext.Current.CancellationToken;
        var design = Design();
        var harness = await CreateHarnessAsync(ct);
        var decider = new ScriptedDecider(
        [
            obs => ExpandFor(obs, "contracts/billing@2"),
            obs => ExpandFor(obs, "contracts/billing@2"),
        ]);
        var loop = harness.Loop(
            decider,
            (current, _) =>
            {
                var patch = EmptyPatch(current);
                return (patch, WorkflowPatchApplier.Apply(current, patch));
            },
            (_, _, _) => throw new InvalidOperationException("author must not run on no-ops"));

        var outcome = await loop.RunAsync(
            WorkflowId, Goal, design, "prior-dsl", "catalogue summary", "stub", 4000, ct);

        outcome.Status.Should().Be(PlanningLoopStatus.Finished);
        outcome.Reason.Should().Contain("No-op convergence");
        outcome.Checkpoint.ConsecutiveNoOps.Should().Be(2);
        outcome.Checkpoint.Mutations.Should().Be(0);
        harness.Host.Executions.Should().Be(0);
    }

    [Fact]
    public async Task Loop_resumes_after_crash_rejects_divergence_and_replays_terminal()
    {
        var ct = TestContext.Current.CancellationToken;
        var design = Design();
        var harness = await CreateHarnessAsync(ct);
        var propose = (PlanningDesign current, IReadOnlyList<string> _) =>
            (BillingV2Patch(current), WorkflowPatchApplier.Apply(current, BillingV2Patch(current)));

        var crashingDecider = new ScriptedDecider(
        [
            obs => ExpandFor(obs, "contracts/billing@2"),
            obs => throw new InvalidOperationException("simulated crash"),
        ]);
        var crashingLoop = harness.Loop(crashingDecider, propose, (_, _, _) => ("dsl-v2", EmptyPlan()));
        var crashed = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            crashingLoop.RunAsync(
                WorkflowId, Goal, design, "prior-dsl", "catalogue summary", "stub", 4000, ct));
        crashed.Message.Should().Contain("simulated crash");
        harness.Host.Executions.Should().Be(1);

        var movedLoop = harness.Loop(new ScriptedDecider([]), propose, (_, _, _) => ("dsl-v2", EmptyPlan()));
        harness.Host.Version = "v9";
        var diverged = await movedLoop.RunAsync(
            WorkflowId, Goal, design, "prior-dsl", "catalogue summary", "stub", 4000, ct);
        diverged.Status.Should().Be(PlanningLoopStatus.Failed);
        diverged.Reason.Should().Contain("moved");

        harness.Host.Version = "v2";
        var finishingLoop = harness.Loop(
            new ScriptedDecider([obs => FinishFor(obs, "Resumed and done.")]),
            propose, (_, _, _) => ("dsl-v2", EmptyPlan()));
        var resumed = await finishingLoop.RunAsync(
            WorkflowId, Goal, design, "prior-dsl", "catalogue summary", "stub", 4000, ct);
        resumed.Status.Should().Be(PlanningLoopStatus.Finished);
        resumed.Checkpoint.Iteration.Should().Be(1);
        harness.Host.Executions.Should().Be(1);

        var replayLoop = harness.Loop(new ScriptedDecider([]), propose, (_, _, _) => ("dsl-v2", EmptyPlan()));
        var replayed = await replayLoop.RunAsync(
            WorkflowId, Goal, design, "prior-dsl", "catalogue summary", "stub", 4000, ct);
        replayed.Status.Should().Be(PlanningLoopStatus.Finished);
        replayed.Reason.Should().Contain("Resumed and done.");
    }

    [Fact]
    public async Task Loop_reobserves_on_a_stale_basis()
    {
        var ct = TestContext.Current.CancellationToken;
        var design = Design();
        var harness = await CreateHarnessAsync(ct);
        var decider = new ScriptedDecider(
        [
            obs => ExpandFor(obs, "contracts/billing@2") with { DesignFingerprint = "stale" },
            obs => FinishFor(obs, "Fresh basis; nothing to do."),
        ]);
        var loop = harness.Loop(decider, (_, _) => throw new InvalidOperationException("unused"), (_, _, _) => throw new InvalidOperationException("unused"));

        var outcome = await loop.RunAsync(
            WorkflowId, Goal, design, "prior-dsl", "catalogue summary", "stub", 4000, ct);

        outcome.Status.Should().Be(PlanningLoopStatus.Finished);
        decider.Calls.Should().Be(2);
        harness.Host.Ensures.Should().Be(1);
        harness.Host.Observes.Should().Be(1);
        outcome.Checkpoint.ModelCalls.Should().Be(2);
    }

    [Fact]
    public async Task Loop_repairs_a_malformed_decision()
    {
        var ct = TestContext.Current.CancellationToken;
        var design = Design();
        var harness = await CreateHarnessAsync(ct);
        var decider = new ScriptedDecider(
        [
            obs => FinishFor(obs, string.Empty),
            obs => FinishFor(obs, "Nothing left to do."),
        ]);
        var loop = harness.Loop(
            decider,
            (_, _) => throw new InvalidOperationException("unused"),
            (_, _, _) => throw new InvalidOperationException("unused"));

        var outcome = await loop.RunAsync(
            WorkflowId, Goal, design, "prior-dsl", "catalogue summary", "stub", 4000, ct);

        outcome.Status.Should().Be(PlanningLoopStatus.Finished);
        outcome.Reason.Should().Contain("Nothing left to do.");
        decider.Calls.Should().Be(2);
        decider.Observations[1].PreviousFailure.Should().Contain("must state a reason");
    }

    [Fact]
    public async Task Loop_produces_stages_before_patching()
    {
        var ct = TestContext.Current.CancellationToken;
        var design = Design();
        var harness = await CreateHarnessAsync(ct);
        var produce = new[]
        {
            new PlannedStage
            {
                StageId = "research",
                Name = "research-notes/main",
                DependsOn = [],
                InputArtifacts = [],
            },
        };
        var decider = new ScriptedDecider(
        [
            obs => ExpandFor(obs, "contracts/billing@2") with { ProduceStages = produce },
            obs => FinishFor(obs, "Research published; nothing structural to do."),
        ]);
        var loop = harness.LoopWithStages(
            decider,
            (current, _) =>
            {
                var patch = EmptyPatch(current);
                return (patch, WorkflowPatchApplier.Apply(current, patch));
            },
            (_, _, _) => throw new InvalidOperationException("author must not run on no-ops"));

        var outcome = await loop.RunAsync(
            WorkflowId, Goal, design, "prior-dsl", "catalogue summary", "stub", 4000, ct);

        outcome.Status.Should().Be(PlanningLoopStatus.Finished);
        outcome.Checkpoint.Mutations.Should().Be(0);
        harness.Host.Executions.Should().Be(0);
        var stored = await harness.Catalog.GetCurrentAsync(
            WorkflowId, new PlanningArtifactKey("research-notes", "main"), ct);
        stored.Should().NotBeNull();
        decider.Observations.Should().HaveCount(2);
        decider.Observations[1].FreshArtifactRevisions.Should().Contain("research-notes/main@1");
    }

    [Fact]
    public async Task Loop_rejects_production_on_finish_decisions()
    {
        var ct = TestContext.Current.CancellationToken;
        var design = Design();
        var harness = await CreateHarnessAsync(ct);
        var produce = new[]
        {
            new PlannedStage
            {
                StageId = "research",
                Name = "research-notes/main",
                DependsOn = [],
                InputArtifacts = [],
            },
        };
        var decider = new ScriptedDecider(
        [
            obs => FinishFor(obs, "Done.") with { ProduceStages = produce },
        ]);
        var loop = harness.LoopWithStages(
            decider,
            (_, _) => throw new InvalidOperationException("unused"),
            (_, _, _) => throw new InvalidOperationException("unused"));

        var outcome = await loop.RunAsync(
            WorkflowId, Goal, design, "prior-dsl", "catalogue summary", "stub", 4000, ct);

        outcome.Status.Should().Be(PlanningLoopStatus.Failed);
        outcome.Reason.Should().Contain("must not request artifact production");
    }

    [Fact]
    public async Task Loop_rejects_production_without_a_configured_runner()
    {
        var ct = TestContext.Current.CancellationToken;
        var design = Design();
        var harness = await CreateHarnessAsync(ct);
        var produce = new[]
        {
            new PlannedStage
            {
                StageId = "research",
                Name = "research-notes/main",
                DependsOn = [],
                InputArtifacts = [],
            },
        };
        var decider = new ScriptedDecider(
        [
            obs => ExpandFor(obs, "contracts/billing@2") with { ProduceStages = produce },
        ]);
        var loop = harness.Loop(
            decider,
            (_, _) => throw new InvalidOperationException("unused"),
            (_, _, _) => throw new InvalidOperationException("unused"));

        var outcome = await loop.RunAsync(
            WorkflowId, Goal, design, "prior-dsl", "catalogue summary", "stub", 4000, ct);

        outcome.Status.Should().Be(PlanningLoopStatus.Failed);
        outcome.Reason.Should().Contain("no stage runner is configured");
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private async Task<Harness> CreateHarnessAsync(
        CancellationToken ct, PlanningLoopPolicy? policy = null)
    {
        var catalog = new PlanningArtifactCatalog(new FileSystemArtifactRepository(_root));
        await catalog.PublishAsync(
            new PublishPlanningArtifactRequest<string>(
                WorkflowId,
                new PlanningArtifactKey("contracts", "billing"),
                1,
                "test",
                "billing-v1"),
            ct);
        await catalog.PublishAsync(
            new PublishPlanningArtifactRequest<string>(
                WorkflowId,
                new PlanningArtifactKey("contracts", "billing"),
                1,
                "test",
                "billing-v2"),
            ct);
        return new Harness(catalog, policy ?? PlanningLoopPolicy.Default);
    }

    private sealed class Harness(PlanningArtifactCatalog catalog, PlanningLoopPolicy policy)
    {
        public ScriptedHost Host { get; } = new();
        public PlanningArtifactCatalog Catalog => catalog;

        public PlanningLoop Loop(
            ScriptedDecider decider,
            Func<PlanningDesign, IReadOnlyList<string>, (WorkflowPatch Patch, PlanningDesign Applied)> propose,
            Func<string, WorkflowPlan, WorkflowPatch, (string Dsl, WorkflowPlan Plan)> author) =>
            BuildLoop(decider, propose, author, stageRunner: null);

        public PlanningLoop LoopWithStages(
            ScriptedDecider decider,
            Func<PlanningDesign, IReadOnlyList<string>, (WorkflowPatch Patch, PlanningDesign Applied)> propose,
            Func<string, WorkflowPlan, WorkflowPatch, (string Dsl, WorkflowPlan Plan)> author)
        {
            var stageCatalogue = PlanningStageCatalogue.Create(
            [
                new PlanningStageDefinition
                {
                    Id = "research",
                    ArtifactKind = "research-notes",
                    SystemPack = "research/system.sbn",
                    UserPack = "research/user.sbn",
                    InputKinds = [],
                    OutputSchema = "ResearchNotes",
                    MaxAttempts = 1,
                    ModelProfile = "research",
                },
            ]);
            var runner = new PlanningStageRunner(
                stageCatalogue,
                new Dictionary<string, IPlanningStageExecutor>(StringComparer.Ordinal)
                {
                    ["research"] = new ScriptedStageExecutor(),
                },
                catalog);
            return BuildLoop(decider, propose, author, runner);
        }

        private PlanningLoop BuildLoop(
            ScriptedDecider decider,
            Func<PlanningDesign, IReadOnlyList<string>, (WorkflowPatch Patch, PlanningDesign Applied)> propose,
            Func<string, WorkflowPlan, WorkflowPatch, (string Dsl, WorkflowPlan Plan)> author,
            PlanningStageRunner? stageRunner) =>
            new(
                decider,
                new ScriptedProposer(propose),
                new ScriptedAuthor(author),
                Host,
                catalog,
                policy,
                new ScriptedCompiler(),
                stageRunner);

        private sealed class ScriptedProposer(
            Func<PlanningDesign, IReadOnlyList<string>, (WorkflowPatch Patch, PlanningDesign Applied)> propose)
            : IWorkflowPatchProposer
        {
            public Task<ProposedRevision> ProposeAsync(
                string goal,
                PlanningDesign current,
                string executionPlan,
                IReadOnlyList<string> changedArtifacts,
                string catalogueSummary,
                string model,
                int maxTokens = 4000,
                CancellationToken cancellationToken = default)
            {
                var (patch, applied) = propose(current, changedArtifacts);
                return Task.FromResult(new ProposedRevision(true, patch, applied, 1, []));
            }
        }

        private sealed class ScriptedAuthor(
            Func<string, WorkflowPlan, WorkflowPatch, (string Dsl, WorkflowPlan Plan)> author)
            : IWorkflowAuthor
        {
            public Task<AuthoredRevision> AuthorFromPatchAsync(
                string goal,
                string executionPlan,
                string priorDsl,
                WorkflowPlan priorPlan,
                WorkflowPatch patch,
                string catalogueSummary,
                string model,
                int maxTokens = 4000,
                CancellationToken cancellationToken = default)
            {
                var (dsl, plan) = author(priorDsl, priorPlan, patch);
                return Task.FromResult(new AuthoredRevision(true, dsl, plan, 1, []));
            }
        }
    }

    private sealed class ScriptedStageExecutor : IPlanningStageExecutor
    {
        public string StageId => "research";

        public Task<StageExecutionResult> ExecuteAsync(
            StageExecutionInput input,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new StageExecutionResult(
                true,
                JsonSerializer.SerializeToElement(new { note = "researched" }),
                []));
    }

    private sealed class ScriptedCompiler : IPlanningDslCompiler
    {
        public Task<WorkflowPlan?> CompileAsync(string dsl, CancellationToken cancellationToken = default) =>
            Task.FromResult<WorkflowPlan?>(EmptyPlan());
    }

    private sealed class ScriptedDecider(
        IReadOnlyList<Func<PlanningDecisionContext, PlanningDecision>> script) : IPlanningDecider
    {
        private int next;
        public int Calls { get; private set; }
        public List<PlanningDecisionContext> Observations { get; } = [];

        public Task<PlanningDecisionResult> DecideAsync(
            PlanningDecisionContext observation,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            Observations.Add(observation);
            var decision = script[Math.Min(next, script.Count - 1)](observation);
            next++;
            return Task.FromResult(new PlanningDecisionResult(true, decision, 1, []));
        }
    }

    private sealed class ScriptedHost : IPlanningExecutionHost
    {
        public string Version = "v1";
        public int Ensures { get; private set; }
        public int Observes { get; private set; }
        public int Executions { get; private set; }

        public Task<WorkflowExecutionSnapshot> EnsureWorkflowAsync(
            string workflowId,
            string dsl,
            string inputJson,
            CancellationToken cancellationToken = default)
        {
            Ensures++;
            return Task.FromResult(new WorkflowExecutionSnapshot(Version, false, "ensured"));
        }

        public Task RegisterAsync(
            string workflowId,
            string version,
            string dsl,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<WorkflowExecutionSnapshot> ObserveAsync(
            string workflowId,
            CancellationToken cancellationToken = default)
        {
            Observes++;
            return Task.FromResult(new WorkflowExecutionSnapshot(Version, false, "observed"));
        }

        public Task<RevisionExecutionResult> ExecuteRevisionAsync(
            string workflowId,
            string dsl,
            WorkflowPatch patch,
            CancellationToken cancellationToken = default)
        {
            Executions++;
            Version = "v2";
            return Task.FromResult(new RevisionExecutionResult(Version, "executed", null));
        }
    }
}
