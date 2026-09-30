using FluentAssertions;
using Penghou.Guihua.Baize;

namespace Penghou.Guihua.Baize.Tests;

public sealed class ScribanPromptTemplateEngineTests
{
    [Fact]
    public async Task ParseFailureIdentifiesTemplateAndPhase()
    {
        var engine = new ScribanPromptTemplateEngine(new StaticPromptLoader("{{ 1 + }}"));

        var action = () => engine.RenderAsync(
            "planning/broken.sbn", new { }, TestContext.Current.CancellationToken);

        var failure = (await action.Should().ThrowAsync<PromptTemplateException>()).Which;
        failure.TemplateName.Should().Be("planning/broken.sbn");
        failure.Phase.Should().Be("parse");
    }

    [Fact]
    public async Task RenderFailureIdentifiesTemplateAndPreservesCause()
    {
        var engine = new ScribanPromptTemplateEngine(new StaticPromptLoader("{{ Value }}"));

        var action = () => engine.RenderAsync(
            "planning/runtime.sbn", new ThrowingModel(), TestContext.Current.CancellationToken);

        var failure = (await action.Should().ThrowAsync<PromptTemplateException>()).Which;
        failure.TemplateName.Should().Be("planning/runtime.sbn");
        failure.Phase.Should().Be("render");
        failure.InnerException.Should().NotBeNull();
    }

    private sealed class StaticPromptLoader(string source) : IPromptLoader
    {
        public Task<string> LoadAsync(
            string promptName,
            CancellationToken cancellationToken = default) => Task.FromResult(source);
    }

    private sealed class ThrowingModel
    {
        public string Value => throw new InvalidOperationException("Model getter failed.");
    }
}
