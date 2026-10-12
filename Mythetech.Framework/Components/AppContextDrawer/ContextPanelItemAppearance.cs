using MudBlazor;

namespace Mythetech.Framework.Components.AppContextDrawer;

/// <summary>
/// How a drawer wants its rail items drawn. The drawer cascades one of these to its items.
/// </summary>
internal sealed record ContextPanelItemAppearance(bool FullWidth, Color Color, Color TooltipColor)
{
    /// <summary>
    /// What an item uses when it is rendered outside a drawer.
    /// </summary>
    public static ContextPanelItemAppearance Default { get; } = new(false, Color.Surface, Color.Primary);
}
