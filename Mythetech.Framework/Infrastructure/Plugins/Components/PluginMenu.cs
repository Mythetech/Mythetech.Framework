namespace Mythetech.Framework.Infrastructure.Plugins.Components;

/// <summary>
/// Base class for plugin components that render in a menu slot.
/// Menu components are typically displayed in the application's navigation area.
/// </summary>
public abstract class PluginMenu : PluginComponentBase
{
    /// <summary>
    /// Icon to display for this menu (rendered in MudIcon)
    /// </summary>
    public abstract string Icon { get; }
    
    /// <summary>
    /// Display name for this plugin menu
    /// </summary>
    public abstract string Title { get; }
    
    /// <summary>
    /// Display order for this menu item (lower values appear first)
    /// </summary>
    public virtual int Order => 100;
    
    /// <summary>
    /// Optional tooltip for the menu item
    /// </summary>
    public virtual string? Tooltip => null;
    
    /// <summary>
    /// Menu items to display in this plugin's submenu.
    /// If null or empty, the plugin won't appear in StandardPluginMenu.
    /// </summary>
    public virtual PluginMenuItem[]? Items => null;
    
    /// <summary>
    /// CSS class to apply to each menu item (e.g., "rounded")
    /// </summary>
    public virtual string? ItemClass => null;
    
    /// <summary>
    /// CSS class to apply to the menu list container
    /// </summary>
    public virtual string? MenuListClass => null;
}
