namespace Mythetech.Framework.Components.HoverStack;

/// <summary>
/// Context provided to HoverStack child content containing hover state information
/// </summary>
public class HoverContext
{
    /// <summary>
    /// Indicates whether the mouse is currently hovering over the HoverStack
    /// </summary>
    public bool IsHovering { get; internal set; }
}
