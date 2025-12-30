namespace Mythetech.Framework.Infrastructure.Plugins.Components;

/// <summary>
/// Base class for plugin components that render in a context/side panel slot.
/// Context panels are typically displayed alongside the main content area.
/// </summary>
public abstract class PluginContextPanel : PluginComponentBase
{
    /// <summary>
    /// Icon to display for this panel (rendered in MudIcon)
    /// </summary>
    public abstract string Icon { get; }
    
    /// <summary>
    /// Title displayed in the panel header
    /// </summary>
    public abstract string Title { get; }
    
    /// <summary>
    /// Display order for this panel (lower values appear first)
    /// </summary>
    public virtual int Order => 100;
    
    /// <summary>
    /// Whether this panel should be visible by default
    /// </summary>
    public virtual bool DefaultVisible => true;
}

