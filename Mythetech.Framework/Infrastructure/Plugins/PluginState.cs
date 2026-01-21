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
    private readonly Lock _pluginsLock = new();
    private IPluginStateProvider? _stateProvider;
    private IMessageBus? _messageBus;
    private ILogger<PluginState>? _logger;
    private PluginLoader? _pluginLoader;
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
    public IReadOnlyList<PluginInfo> Plugins
    {
        get
        {
            lock (_pluginsLock)
            {
                return _plugins.ToList().AsReadOnly();
            }
        }
    }

    /// <summary>
    /// Enabled plugins only (respects PluginsActive flag)
    /// </summary>
    public IEnumerable<PluginInfo> EnabledPlugins
    {
        get
        {
            lock (_pluginsLock)
            {
                return PluginsActive ? _plugins.Where(p => p.IsEnabled).ToList() : [];
            }
        }
    }

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
    /// Register a newly loaded plugin.
    /// Version-aware: throws only on identical version, upgrades on newer version.
    /// </summary>
    public async Task RegisterPluginAsync(PluginInfo plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        lock (_pluginsLock)
        {
            var existing = GetPluginUnsafe(plugin.Manifest.Id);
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
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Cannot downgrade plugin '{plugin.Manifest.Id}' from version {existing.Manifest.Version} to {plugin.Manifest.Version}");
                }
            }
            else
            {
                _plugins.Add(plugin);
            }
        }

        NotifyStateChanged();
        await PublishPluginLoadedAsync(plugin);
    }

    /// <summary>
    /// Enable a plugin by its ID.
    /// </summary>
    /// <returns>True if the plugin was enabled, false if cancelled or not found</returns>
    public async Task<bool> EnablePluginAsync(string pluginId)
    {
        var plugin = GetPlugin(pluginId);
        if (plugin is null || plugin.IsEnabled) return false;

        var args = new PluginLifecycleEventArgs { Plugin = plugin };
        PluginEnabling?.Invoke(this, args);

        if (args.Cancel) return false;

        plugin.IsEnabled = true;
        PluginEnabled?.Invoke(this, args);
        NotifyStateChanged();

        await PublishPluginEnabledAsync(plugin);
        await PersistStateAsync();

        return true;
    }

    /// <summary>
    /// Disable a plugin by its ID.
    /// </summary>
    /// <returns>True if the plugin was disabled, false if cancelled or not found</returns>
    public async Task<bool> DisablePluginAsync(string pluginId)
    {
        var plugin = GetPlugin(pluginId);
        if (plugin is null || !plugin.IsEnabled) return false;

        var args = new PluginLifecycleEventArgs { Plugin = plugin };
        PluginDisabling?.Invoke(this, args);

        if (args.Cancel) return false;

        plugin.IsEnabled = false;
        PluginDisabled?.Invoke(this, args);
        NotifyStateChanged();

        await PublishPluginDisabledAsync(plugin);
        await PersistStateAsync();

        return true;
    }

    /// <summary>
    /// Get a plugin by its ID
    /// </summary>
    public PluginInfo? GetPlugin(string pluginId)
    {
        lock (_pluginsLock)
        {
            return GetPluginUnsafe(pluginId);
        }
    }

    /// <summary>
    /// Get a plugin by its ID and version
    /// </summary>
    public PluginInfo? GetPlugin(string pluginId, Version version)
    {
        lock (_pluginsLock)
        {
            return _plugins.FirstOrDefault(p => p.Manifest.Id == pluginId && p.IsSameVersion(version));
        }
    }

    /// <summary>
    /// Internal method to get plugin without locking - must be called within a lock
    /// </summary>
    private PluginInfo? GetPluginUnsafe(string pluginId)
    {
        return _plugins.FirstOrDefault(p => p.Manifest.Id == pluginId);
    }

    /// <summary>
    /// Check if a plugin can be registered (allows upgrades, disallows downgrades and identicals)
    /// </summary>
    /// <param name="plugin">Plugin to check</param>
    /// <returns>True if registration is allowed</returns>
    public bool CanRegisterPlugin(PluginInfo plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        lock (_pluginsLock)
        {
            var existing = GetPluginUnsafe(plugin.Manifest.Id);
            if (existing is null)
                return true;

            return plugin.IsNewerThan(existing.Manifest.Version);
        }
    }

    /// <summary>
    /// Register or upgrade a plugin. Replaces if newer version, ignores if same/older.
    /// </summary>
    /// <param name="plugin">Plugin to register</param>
    /// <returns>True if plugin was registered or upgraded, false if ignored</returns>
    public async Task<bool> RegisterOrUpgradePluginAsync(PluginInfo plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        bool registered = false;
        lock (_pluginsLock)
        {
            var existing = GetPluginUnsafe(plugin.Manifest.Id);
            if (existing is null)
            {
                _plugins.Add(plugin);
                registered = true;
            }
            else if (plugin.IsNewerThan(existing.Manifest.Version))
            {
                var wasEnabled = existing.IsEnabled;
                _plugins.Remove(existing);
                plugin.IsEnabled = wasEnabled;
                _plugins.Add(plugin);
                registered = true;
            }
        }

        if (registered)
        {
            NotifyStateChanged();
            await PublishPluginLoadedAsync(plugin);
        }

        return registered;
    }

    /// <summary>
    /// Remove a plugin by its ID.
    /// If the plugin has an associated LoadContext, the assembly will be unloaded.
    /// </summary>
    /// <returns>True if the plugin was removed, false if not found</returns>
    public async Task<bool> RemovePluginAsync(string pluginId)
    {
        PluginInfo? plugin;
        lock (_pluginsLock)
        {
            plugin = GetPluginUnsafe(pluginId);
            if (plugin is null) return false;
        }

        if (plugin.IsEnabled)
        {
            await DisablePluginAsync(pluginId);
        }

        lock (_pluginsLock)
        {
            _plugins.Remove(plugin);
        }

        if (plugin.LoadContext is not null)
        {
            try
            {
                plugin.LoadContext.Unload();
                _logger?.LogDebug("Unloaded assembly for plugin {PluginId}", pluginId);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to unload assembly for plugin {PluginId}", pluginId);
            }
        }

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
    /// Sets the plugin loader for discovering and loading plugins.
    /// </summary>
    /// <param name="loader">The plugin loader to use.</param>
    public void SetPluginLoader(PluginLoader? loader)
    {
        _pluginLoader = loader;
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
            lock (_pluginsLock)
            {
                foreach (var plugin in _plugins)
                {
                    if (disabledPlugins.Contains(plugin.Manifest.Id))
                    {
                        plugin.IsEnabled = false;
                    }
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
    /// Initializes plugins from a directory. Call from OnAfterRenderAsync after first render.
    /// </summary>
    /// <param name="pluginDirectory">Plugin directory path, or null to use default 'plugins' directory</param>
    public async Task InitializePluginsAsync(string? pluginDirectory = null)
    {
        if (PluginsLoaded)
        {
            _logger?.LogDebug("Plugins already initialized, skipping");
            return;
        }

        if (_pluginLoader is null)
        {
            throw new InvalidOperationException("PluginLoader not set. Call UsePluginFramework() before initializing plugins.");
        }

        var fullPath = Path.GetFullPath(pluginDirectory ?? GetDefaultPluginDirectory());
        SetPluginDirectory(fullPath);

        if (_messageBus != null)
        {
            await _messageBus.PublishAsync(new Events.PluginsLoadingStarted(fullPath));
        }

        var plugins = _pluginLoader.LoadPluginsFromDirectory(fullPath);

        foreach (var plugin in plugins)
        {
            await RegisterOrUpgradePluginAsync(plugin);
        }

        PluginsLoaded = true;

        if (_messageBus != null)
        {
            await _messageBus.PublishAsync(new Events.PluginsLoadingCompleted(fullPath, Plugins.Count));
        }
    }

    private static string GetDefaultPluginDirectory()
    {
        return Path.Combine(AppContext.BaseDirectory, "plugins");
    }

    /// <summary>
    /// Persists current plugin states to the state provider.
    /// </summary>
    private async Task PersistStateAsync()
    {
        if (_stateProvider == null) return;

        try
        {
            HashSet<string> disabledPlugins;
            lock (_pluginsLock)
            {
                disabledPlugins = _plugins
                    .Where(p => !p.IsEnabled)
                    .Select(p => p.Manifest.Id)
                    .ToHashSet();
            }

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
        await _messageBus.PublishAsync(new PluginLoaded(plugin));
    }

    private async Task PublishPluginEnabledAsync(PluginInfo plugin)
    {
        if (_messageBus == null) return;
        await _messageBus.PublishAsync(new Events.PluginEnabled(plugin));
    }

    private async Task PublishPluginDisabledAsync(PluginInfo plugin)
    {
        if (_messageBus == null) return;
        await _messageBus.PublishAsync(new Events.PluginDisabled(plugin));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        lock (_pluginsLock)
        {
            foreach (var plugin in _plugins)
            {
                if (plugin.LoadContext is not null)
                {
                    try
                    {
                        plugin.LoadContext.Unload();
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to unload assembly for plugin {PluginId}", plugin.Manifest.Id);
                    }
                }
            }
            _plugins.Clear();
        }
        GC.SuppressFinalize(this);
    }
}
