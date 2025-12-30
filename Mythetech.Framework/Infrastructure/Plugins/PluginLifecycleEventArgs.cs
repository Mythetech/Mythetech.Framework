namespace Mythetech.Framework.Infrastructure.Plugins;

/// <summary>
/// Event arguments for plugin lifecycle events (enabling/disabling)
/// </summary>
public class PluginLifecycleEventArgs : EventArgs
{
    /// <summary>
    /// The plugin being enabled or disabled
    /// </summary>
    public required PluginInfo Plugin { get; init; }
    
    /// <summary>
    /// Can be set to true by event handlers to cancel the enable/disable operation
    /// </summary>
    public bool Cancel { get; set; }
}

