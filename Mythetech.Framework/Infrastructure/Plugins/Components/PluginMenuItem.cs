namespace Mythetech.Framework.Infrastructure.Plugins.Components;

/// <summary>
/// Represents a menu item that can be rendered in a plugin menu.
/// </summary>
public class PluginMenuItem
{
    /// <summary>
    /// Icon to display for this menu item (e.g., from MudBlazor Icons.Material)
    /// </summary>
    public required string Icon { get; init; }
    
    /// <summary>
    /// Display text for the menu item
    /// </summary>
    public required string Text { get; init; }
    
    /// <summary>
    /// Action to execute when the menu item is clicked.
    /// Receives the PluginContext for access to MessageBus and other services.
    /// </summary>
    public required Func<PluginContext, Task> OnClick { get; init; }
    
    /// <summary>
    /// Whether the menu item is disabled
    /// </summary>
    public bool Disabled { get; init; }
}

