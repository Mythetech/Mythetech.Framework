namespace Mythetech.Framework.Infrastructure.Plugins;

/// <summary>
/// Central state for managing plugins. UI components should depend on this.
/// Registered as a Singleton in DI.
/// </summary>
public class PluginState : IDisposable
{
    private readonly List<PluginInfo> _plugins = [];
    private bool _disposed;
    private bool _pluginsActive = true;
    
    /// <summary>
    /// Raised before a plugin is enabled. Handlers can set Cancel = true to prevent enabling.
    /// </summary>
    public event EventHandler<PluginLifecycleEventArgs>? PluginEnabling;
    
    /// <summary>
    /// Raised after a plugin has been enabled
    /// </summary>
    public event EventHandler<PluginLifecycleEventArgs>? PluginEnabled;
    
    /// <summary>
    /// Raised before a plugin is disabled. Handlers can set Cancel = true to prevent disabling.
    /// </summary>
    public event EventHandler<PluginLifecycleEventArgs>? PluginDisabling;
    
    /// <summary>
    /// Raised after a plugin has been disabled
    /// </summary>
    public event EventHandler<PluginLifecycleEventArgs>? PluginDisabled;
    
    /// <summary>
    /// Raised when any plugin state changes (plugins added, enabled, disabled, etc.)
    /// </summary>
    public event EventHandler? StateChanged;
    
    /// <summary>
    /// Global toggle for plugin system. When false, no plugins are considered enabled.
    /// </summary>
    public bool PluginsActive
    {
        get => _pluginsActive;
        set
        {
            if (_pluginsActive != value)
            {
                _pluginsActive = value;
                NotifyStateChanged();
            }
        }
    }
    
    /// <summary>
    /// All loaded plugins regardless of enabled state
    /// </summary>
    public IReadOnlyList<PluginInfo> Plugins => _plugins.AsReadOnly();
    
    /// <summary>
    /// Enabled plugins only (respects PluginsActive flag)
    /// </summary>
    public IEnumerable<PluginInfo> EnabledPlugins => 
        PluginsActive ? _plugins.Where(p => p.IsEnabled) : [];
    
    /// <summary>
    /// All menu components from enabled plugins (types only)
    /// </summary>
    public IEnumerable<Type> EnabledMenuComponents =>
        EnabledPlugins.SelectMany(p => p.MenuComponents);
    
    /// <summary>
    /// All context panel components from enabled plugins (types only)
    /// </summary>
    public IEnumerable<Type> EnabledContextPanelComponents =>
        EnabledPlugins.SelectMany(p => p.ContextPanelComponents);
    
    /// <summary>
    /// All menu component metadata from enabled plugins (with Icon, Title, Order)
    /// </summary>
    public IEnumerable<PluginComponentMetadata> EnabledMenuComponentsMetadata =>
        EnabledPlugins.SelectMany(p => p.MenuComponentsMetadata);
    
    /// <summary>
    /// All context panel component metadata from enabled plugins (with Icon, Title, Order)
    /// </summary>
    public IEnumerable<PluginComponentMetadata> EnabledContextPanelComponentsMetadata =>
        EnabledPlugins.SelectMany(p => p.ContextPanelComponentsMetadata);
    
    /// <summary>
    /// Register a newly loaded plugin
    /// </summary>
    public void RegisterPlugin(PluginInfo plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        
        if (_plugins.Any(p => p.Manifest.Id == plugin.Manifest.Id))
        {
            throw new InvalidOperationException($"Plugin with ID '{plugin.Manifest.Id}' is already registered");
        }
        
        _plugins.Add(plugin);
        NotifyStateChanged();
    }
    
    /// <summary>
    /// Enable a plugin by its ID
    /// </summary>
    /// <returns>True if the plugin was enabled, false if cancelled or not found</returns>
    public bool EnablePlugin(string pluginId)
    {
        var plugin = GetPlugin(pluginId);
        if (plugin is null || plugin.IsEnabled) return false;
        
        var args = new PluginLifecycleEventArgs { Plugin = plugin };
        PluginEnabling?.Invoke(this, args);
        
        if (args.Cancel) return false;
        
        plugin.IsEnabled = true;
        PluginEnabled?.Invoke(this, args);
        NotifyStateChanged();
        
        return true;
    }
    
    /// <summary>
    /// Disable a plugin by its ID
    /// </summary>
    /// <returns>True if the plugin was disabled, false if cancelled or not found</returns>
    public bool DisablePlugin(string pluginId)
    {
        var plugin = GetPlugin(pluginId);
        if (plugin is null || !plugin.IsEnabled) return false;
        
        var args = new PluginLifecycleEventArgs { Plugin = plugin };
        PluginDisabling?.Invoke(this, args);
        
        if (args.Cancel) return false;
        
        plugin.IsEnabled = false;
        PluginDisabled?.Invoke(this, args);
        NotifyStateChanged();
        
        return true;
    }
    
    /// <summary>
    /// Get a plugin by its ID
    /// </summary>
    public PluginInfo? GetPlugin(string pluginId)
    {
        return _plugins.FirstOrDefault(p => p.Manifest.Id == pluginId);
    }
    
    /// <summary>
    /// Notify listeners that state has changed
    /// </summary>
    public void NotifyStateChanged()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
    
    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        _plugins.Clear();
        GC.SuppressFinalize(this);
    }
}

