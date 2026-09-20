namespace Penghou.Guihua.Baize;

public interface ILlmPromptContext
{
    double Temperature { get; }
    int MaxTokens { get; }
}