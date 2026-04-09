using MudBlazor;

namespace Mythetech.Framework.Storybook.Shared;

/// <summary>
/// Default markdown styling for Mythetech storybook.
/// </summary>
public static class MythetechMarkdownStyling
{
    public static MudMarkdownStyling Default { get; } = new()
    {
        CodeBlock =
        {
            Theme = CodeBlockTheme.Vs2015,
        },
    };
}
