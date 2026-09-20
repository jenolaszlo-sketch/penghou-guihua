using Penghou.Baize;

namespace Penghou.Guihua.Baize;

public interface IPromptBuilder<in TContext>
{
    Task<LlmRequest> BuildAsync(TContext context, CancellationToken cancellationToken = default);
}