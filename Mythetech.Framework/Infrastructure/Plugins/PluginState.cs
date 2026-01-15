using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Plugins.Events;

namespace Mythetech.Framework.Infrastructure.Plugins;

/// <summary>
/// Central state for managing plugins. UI components should depend on this.
/// Registered as a Singleton in DI.
/// </summary>
public class PluginState : IDisposable
{
    private readonly List<PluginInfo> _plugins = [];
    private IPluginStateProvider? _stateProvider;
    private IMessageBus? _messageBus;
    private ILogger<PluginState>? _logger;
    private bool _disposed;
    private bool _pluginsActive = true;
    private bool _pluginsLoaded;
    
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
    /// The directory path from which plugins were loaded.
    /// Set when plugins are loaded via UsePlugins().
    /// </summary>
    public string? PluginDirectory { get; private set; }

    /// <summary>
    /// Whether plugins have been loaded. Used to prevent double-loading
    /// when using deferred plugin loading.
    /// </summary>
    public bool PluginsLoaded
    {
        get => _pluginsLoaded;
        internal set => _pluginsLoaded = value;
    }
    
    /// <summary>
    /// Register a newly loaded plugin
    /// Version-aware: throws only on identical version, upgrades on newer version
    /// </summary>
    public void RegisterPlugin(PluginInfo plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        var existing = GetPlugin(plugin.Manifest.Id);
        if (existing is not null)
        {
            if (plugin.IsSameVersion(existing.Manifest.Version))
            {
                throw new InvalidOperationException(
                    $"Plugin with ID '{plugin.Manifest.Id}' version {plugin.Manifest.Version} is already registered");
            }

            if (plugin.IsNewerThan(existing.Manifest.Version))
            {
                var wasEnabled = existing.IsEnabled;
                _plugins.Remove(existing);
                plugin.IsEnabled = wasEnabled;
                _plugins.Add(plugin);
                NotifyStateChanged();
                _ = PublishPluginLoadedAsync(plugin);
                return;
            }

            throw new InvalidOperationException(
                $"Cannot downgrade plugin '{plugin.Manifest.Id}' from version {existing.Manifest.Version} to {plugin.Manifest.Version}");
        }

        _plugins.Add(plugin);
        NotifyStateChanged();
        _ = PublishPluginLoadedAsync(plugin);
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

        // Publish MessageBus event
        _ = PublishPluginEnabledAsync(plugin);

        // Persist state asynchronously (fire and forget)
        _ = PersistStateAsync();

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

        // Publish MessageBus event
        _ = PublishPluginDisabledAsync(plugin);

        // Persist state asynchronously (fire and forget)
        _ = PersistStateAsync();

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
    /// Get a plugin by its ID and version
    /// </summary>
    public PluginInfo? GetPlugin(string pluginId, Version version)
    {
        return _plugins.FirstOrDefault(p => p.Manifest.Id == pluginId && p.IsSameVersion(version));
    }
    
    /// <summary>
    /// Check if a plugin can be registered (allows upgrades, disallows downgrades and identicals)
    /// </summary>
    /// <param name="plugin">Plugin to check</param>
    /// <returns>True if registration is allowed</returns>
    public bool CanRegisterPlugin(PluginInfo plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        
        var existing = GetPlugin(plugin.Manifest.Id);
        if (existing is null)
            return true;
        
        return plugin.IsNewerThan(existing.Manifest.Version);
    }
    
    /// <summary>
    /// Register or upgrade a plugin. Replaces if newer version, ignores if same/older.
    /// </summary>
    /// <param name="plugin">Plugin to register</param>
    /// <returns>True if plugin was registered or upgraded, false if ignored</returns>
    public bool RegisterOrUpgradePlugin(PluginInfo plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        var existing = GetPlugin(plugin.Manifest.Id);
        if (existing is null)
        {
            _plugins.Add(plugin);
            NotifyStateChanged();
            _ = PublishPluginLoadedAsync(plugin);
            return true;
        }

        if (plugin.IsNewerThan(existing.Manifest.Version))
        {
            var wasEnabled = existing.IsEnabled;
            _plugins.Remove(existing);
            plugin.IsEnabled = wasEnabled;
            _plugins.Add(plugin);
            NotifyStateChanged();
            _ = PublishPluginLoadedAsync(plugin);
            return true;
        }

        return false;
    }
    
    /// <summary>
    /// Remove a plugin by its ID
    /// </summary>
    /// <returns>True if the plugin was removed, false if not found</returns>
    public bool RemovePlugin(string pluginId)
    {
        var plugin = GetPlugin(pluginId);
        if (plugin is null) return false;
        
        if (plugin.IsEnabled)
        {
            DisablePlugin(pluginId);
        }
        
        _plugins.Remove(plugin);
        NotifyStateChanged();
        
        return true;
    }
    
    /// <summary>
    /// Sets the plugin directory path. Called when plugins are loaded.
    /// </summary>
    internal void SetPluginDirectory(string pluginDirectory)
    {
        PluginDirectory = pluginDirectory;
    }

    /// <summary>
    /// Sets the state provider for persisting plugin enabled/disabled states.
    /// </summary>
    /// <param name="provider">The state provider to use.</param>
    public void SetStateProvider(IPluginStateProvider? provider)
    {
        _stateProvider = provider;
    }

    /// <summary>
    /// Sets the message bus for publishing plugin lifecycle events.
    /// </summary>
    /// <param name="messageBus">The message bus to use.</param>
    public void SetMessageBus(IMessageBus? messageBus)
    {
        _messageBus = messageBus;
    }

    /// <summary>
    /// Sets the logger for diagnostic output.
    /// </summary>
    /// <param name="logger">The logger to use.</param>
    public void SetLogger(ILogger<PluginState>? logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Loads persisted plugin states from the state provider.
    /// Should be called after plugins are loaded.
    /// </summary>
    public async Task LoadStateAsync()
    {
        if (_stateProvider == null) return;

        try
        {
            var disabledPlugins = await _stateProvider.LoadDisabledPluginsAsync();
            foreach (var plugin in _plugins)
            {
                if (disabledPlugins.Contains(plugin.Manifest.Id))
                {
                    plugin.IsEnabled = false;
                }
            }
            NotifyStateChanged();
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Failed to load plugin state - state persistence is optional");
        }
    }

    /// <summary>
    /// Persists current plugin states to the state provider.
    /// </summary>
    private async Task PersistStateAsync()
    {
        if (_stateProvider == null) return;

        try
        {
            var disabledPlugins = _plugins
                .Where(p => !p.IsEnabled)
                .Select(p => p.Manifest.Id)
                .ToHashSet();

            await _stateProvider.SaveDisabledPluginsAsync(disabledPlugins);
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Failed to persist plugin state - state persistence is optional");
        }
    }

    /// <summary>
    /// Notify listeners that state has changed
    /// </summary>
    public void NotifyStateChanged()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task PublishPluginLoadedAsync(PluginInfo plugin)
    {
        if (_messageBus == null) return;
        try
        {
            await _messageBus.PublishAsync(new PluginLoaded(plugin));
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Failed to publish PluginLoaded event for {PluginId}", plugin.Manifest.Id);
        }
    }

    private async Task PublishPluginEnabledAsync(PluginInfo plugin)
    {
        if (_messageBus == null) return;
        try
        {
            await _messageBus.PublishAsync(new Events.PluginEnabled(plugin));
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Failed to publish PluginEnabled event for {PluginId}", plugin.Manifest.Id);
        }
    }

    private async Task PublishPluginDisabledAsync(PluginInfo plugin)
    {
        if (_messageBus == null) return;
        try
        {
            await _messageBus.PublishAsync(new Events.PluginDisabled(plugin));
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Failed to publish PluginDisabled event for {PluginId}", plugin.Manifest.Id);
        }
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

