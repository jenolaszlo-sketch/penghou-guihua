namespace Penghou.Guihua.Baize;

/// <summary>A named prompt template failed during loading, parsing, or rendering.</summary>
public sealed class PromptTemplateException : Exception
{
    public PromptTemplateException(
        string templateName,
        string phase,
        string detail,
        Exception? innerException = null)
        : base($"Prompt template '{templateName}' failed during {phase}: {detail}", innerException)
    {
        TemplateName = templateName;
        Phase = phase;
    }

    public string TemplateName { get; }

    public string Phase { get; }
}
