using System.Reflection;

namespace Penghou.Guihua.Baize;

/// <summary>
/// Loads prompt packs embedded in this assembly (<c>prompts/&lt;pack&gt;/&lt;file&gt;.sbn</c>).
/// Names are relative with forward slashes and may not escape the prompts root.
/// </summary>
public sealed class EmbeddedPromptLoader(Assembly? assembly = null) : IPromptLoader
{
    private readonly Assembly assembly = assembly ?? typeof(EmbeddedPromptLoader).Assembly;

    public Task<string> LoadAsync(string promptName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(promptName))
            throw new ArgumentException("Prompt name cannot be empty.", nameof(promptName));
        if (promptName.StartsWith('/') || promptName.Contains('\\') || promptName.Contains(".."))
            throw new InvalidOperationException($"Prompt name must be relative: {promptName}");

        var resourceName = $"{assembly.GetName().Name}.prompts." +
            promptName.Replace('/', '.').Replace('-', '_');
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"Embedded prompt '{promptName}' was not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEndAsync(cancellationToken);
    }
}
