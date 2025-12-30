namespace Mythetech.Framework.Infrastructure.Plugins;

/// <summary>
/// Cached metadata about a plugin component, extracted at load time.
/// Avoids needing to instantiate components just to read their properties.
/// </summary>
public record PluginComponentMetadata
{
    /// <summary>
    /// The component type to render via DynamicComponent
    /// </summary>
    public required Type ComponentType { get; init; }
    
    /// <summary>
    /// Display title (from PluginContextPanel.Title or PluginMenu.Title)
    /// </summary>
    public required string Title { get; init; }
    
    /// <summary>
    /// Icon string (from PluginContextPanel.Icon or PluginMenu.Icon)
    /// </summary>
    public required string Icon { get; init; }
    
    /// <summary>
    /// Display order (lower = first)
    /// </summary>
    public int Order { get; init; } = 100;
    
    /// <summary>
    /// Optional tooltip
    /// </summary>
    public string? Tooltip { get; init; }
}

