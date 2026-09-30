using Scriban;
using Scriban.Runtime;
using System.Collections.Concurrent;

namespace Penghou.Guihua.Baize;

public sealed class ScribanPromptTemplateEngine(IPromptLoader promptLoader) : IPromptTemplateEngine
{
    private readonly ConcurrentDictionary<string, Template> _compiledCache = new();

    public async Task<string> RenderAsync(
        string templateName,
        object model,
        CancellationToken cancellationToken = default)
    {
        var template = await GetCompiledTemplateAsync(templateName, cancellationToken);
        try
        {
            var scriptObject = new ScriptObject();
            scriptObject.Import(model);

            var context = new TemplateContext { MemberRenamer = member => member.Name };
            context.PushGlobal(scriptObject);

            return await template.RenderAsync(context);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new PromptTemplateException(templateName, "render", exception.Message, exception);
        }
    }

    private async Task<Template> GetCompiledTemplateAsync(string templateName, CancellationToken cancellationToken)
    {
        if (_compiledCache.TryGetValue(templateName, out var cached))
            return cached;

        string raw;
        try
        {
            raw = await promptLoader.LoadAsync(templateName, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new PromptTemplateException(templateName, "load", exception.Message, exception);
        }

        Template template;
        try
        {
            template = Template.Parse(raw, templateName);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new PromptTemplateException(templateName, "parse", exception.Message, exception);
        }

        if (template.HasErrors)
        {
            throw new PromptTemplateException(
                templateName, "parse", string.Join("; ", template.Messages));
        }

        _compiledCache[templateName] = template;
        return template;
    }
}
