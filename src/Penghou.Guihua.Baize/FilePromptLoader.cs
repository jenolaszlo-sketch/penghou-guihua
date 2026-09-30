namespace Penghou.Guihua.Baize;

public sealed class FilePromptLoader : IPromptLoader
{
    private readonly string _promptRoot;

    public FilePromptLoader(string promptRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(promptRoot);
        _promptRoot = Path.GetFullPath(promptRoot);
    }

    public async Task<string> LoadAsync(
        string promptName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(promptName))
            throw new ArgumentException("Prompt name cannot be empty.", nameof(promptName));

        if (Path.IsPathRooted(promptName))
            throw new InvalidOperationException($"Prompt name must be relative: {promptName}");

        var fullPath = Path.GetFullPath(Path.Combine(_promptRoot, promptName));
        var rootPrefix = Path.EndsInDirectorySeparator(_promptRoot)
            ? _promptRoot
            : _promptRoot + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(
                rootPrefix,
                OperatingSystem.IsWindows()
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal))
            throw new InvalidOperationException($"Prompt path escapes prompt root: {promptName}");

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Prompt file not found: {fullPath}");

        return await File.ReadAllTextAsync(fullPath, cancellationToken);
    }
}
