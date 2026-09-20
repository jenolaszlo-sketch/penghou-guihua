using System;
using System.Collections.Generic;
using System.Text;

namespace Penghou.Guihua.Baize;

public interface IPromptTemplateEngine
{
    Task<string> RenderAsync(
        string templateName,
        object model,
        CancellationToken cancellationToken = default);
}