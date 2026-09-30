using FluentAssertions;
using Penghou.Guihua.Baize;

namespace Penghou.Guihua.Baize.Tests;

public sealed class FilePromptLoaderTests : IDisposable
{
    private readonly string root = Path.Combine(
        Path.GetTempPath(), "guihua-prompt-loader-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task RejectsSiblingWhoseNameStartsWithPromptRoot()
    {
        var prompts = Path.Combine(root, "prompts");
        var sibling = Path.Combine(root, "prompts-other");
        Directory.CreateDirectory(prompts);
        Directory.CreateDirectory(sibling);
        await File.WriteAllTextAsync(
            Path.Combine(sibling, "secret.sbn"), "outside", TestContext.Current.CancellationToken);

        var loader = new FilePromptLoader(prompts);
        var action = () => loader.LoadAsync(
            "../prompts-other/secret.sbn", TestContext.Current.CancellationToken);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*escapes*");
    }

    [Fact]
    public async Task RejectsCaseVariantSiblingOnCaseSensitiveSystems()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var prompts = Path.Combine(root, "prompts");
        var sibling = Path.Combine(root, "PROMPTS");
        Directory.CreateDirectory(prompts);
        Directory.CreateDirectory(sibling);
        await File.WriteAllTextAsync(
            Path.Combine(sibling, "secret.sbn"), "outside", TestContext.Current.CancellationToken);

        var loader = new FilePromptLoader(prompts);
        var action = () => loader.LoadAsync(
            "../PROMPTS/secret.sbn", TestContext.Current.CancellationToken);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*escapes*");
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
