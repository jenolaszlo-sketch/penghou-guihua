namespace Penghou.Guihua.Baize;

public interface IPromptLoader
{
    Task<string> LoadAsync(
        string promptName,
        CancellationToken cancellationToken = default);
}
