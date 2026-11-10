using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Mythetech.Framework.Components.AppContextDrawer;

/// <summary>
/// Renders the open panel's content behind a key, so panels built from one loop don't share component instances.
/// </summary>
internal sealed class ActivePanelContent : ComponentBase
{
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, ChildContent);
}
