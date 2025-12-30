using Microsoft.AspNetCore.Components;
using Mythetech.Framework.Infrastructure.MessageBus;

namespace Mythetech.Framework.Infrastructure.Plugins.Components;

/// <summary>
/// Base class for all plugin Blazor components.
/// Provides access to the plugin context, message bus, shared state, and persistent storage.
/// </summary>
public abstract class PluginComponentBase : ComponentBase, IDisposable
{
    private IPluginStorage? _storage;
    private string? _pluginId;
    
    /// <summary>
    /// The plugin context providing access to host services
    /// </summary>
    [Inject]
    protected PluginContext Context { get; set; } = default!;
    
    /// <summary>
    /// Shortcut to the message bus for publishing messages
    /// </summary>
    protected IMessageBus MessageBus => Context.MessageBus;
    
    /// <summary>
    /// Shortcut to the state store
    /// </summary>
    protected PluginStateStore StateStore => Context.StateStore;
    
    /// <summary>
    /// Persistent storage for this plugin.
    /// Returns null if the host doesn't support storage.
    /// </summary>
    protected IPluginStorage? Storage
    {
        get
        {
            if (_storage != null) return _storage;
            
            var pluginId = GetPluginId();
            if (pluginId == null || Context.StorageFactory == null) return null;
            
            _storage = Context.StorageFactory.CreateForPlugin(pluginId);
            return _storage;
        }
    }
    
    /// <summary>
    /// Whether persistent storage is available
    /// </summary>
    protected bool HasStorage => Context.StorageFactory != null;
    
    /// <summary>
    /// Asset loader for loading CSS/JS at runtime
    /// </summary>
    protected IPluginAssetLoader? AssetLoader => Context.AssetLoader;
    
    /// <summary>
    /// Load a CSS stylesheet dynamically.
    /// Path is relative to the plugin's wwwroot, or an absolute URL.
    /// </summary>
    /// <param name="path">Path to stylesheet (relative to wwwroot or absolute URL)</param>
    protected async Task LoadStylesheetAsync(string path)
    {
        if (AssetLoader == null) return;
        
        var resolvedPath = ResolvePath(path);
        await AssetLoader.LoadStylesheetAsync(resolvedPath);
    }
    
    /// <summary>
    /// Load a JavaScript file dynamically.
    /// Path is relative to the plugin's wwwroot, or an absolute URL.
    /// </summary>
    /// <param name="path">Path to script (relative to wwwroot or absolute URL)</param>
    protected async Task LoadScriptAsync(string path)
    {
        if (AssetLoader == null) return;
        
        var resolvedPath = ResolvePath(path);
        await AssetLoader.LoadScriptAsync(resolvedPath);
    }
    
    private string ResolvePath(string path)
    {
        // Absolute URLs - pass through
        if (path.StartsWith("http://") || path.StartsWith("https://") || path.StartsWith("//"))
            return path;
        
        // Already a _content path (e.g., from a NuGet package) - pass through
        if (path.StartsWith("/_content/") || path.StartsWith("_content/"))
            return path.StartsWith("/") ? path : "/" + path;
        
        // Absolute path starting with / - pass through
        if (path.StartsWith("/"))
            return path;
        
        // Relative path - prefix with plugin's content path
        var assemblyName = GetType().Assembly.GetName().Name;
        return $"/_content/{assemblyName}/{path}";
    }
    
    /// <summary>
    /// Publish a message with default plugin timeout (30s)
    /// </summary>
    protected Task PublishAsync<TMessage>(TMessage message) where TMessage : class
        => Context.PublishAsync(message);

    /// <summary>
    /// Get shared state for this plugin (in-memory, not persistent).
    /// State is automatically namespaced by the plugin's assembly.
    /// </summary>
    /// <typeparam name="T">The state type</typeparam>
    /// <param name="key">Optional key for multiple state values (default: "default")</param>
    protected T? GetState<T>(string key = "default")
        => StateStore.GetForPlugin<T>(GetType(), key);

    /// <summary>
    /// Set shared state for this plugin (in-memory, not persistent).
    /// State is automatically namespaced by the plugin's assembly.
    /// Triggers StateChanged event so other components can react.
    /// </summary>
    /// <typeparam name="T">The state type</typeparam>
    /// <param name="value">The value to store</param>
    /// <param name="key">Optional key for multiple state values (default: "default")</param>
    protected void SetState<T>(T value, string key = "default")
        => StateStore.SetForPlugin(GetType(), key, value);

    /// <summary>
    /// Get the plugin ID for this component's plugin
    /// </summary>
    protected string? GetPluginId()
    {
        if (_pluginId != null) return _pluginId;
        
        var assembly = GetType().Assembly;
        var plugin = Context.StateStore.GetForPlugin<object>(GetType(), "__lookup__");
        
        // Look up plugin by assembly
        var pluginState = Context.Services.GetService(typeof(PluginState)) as PluginState;
        var pluginInfo = pluginState?.Plugins.FirstOrDefault(p => p.Assembly == assembly);
        
        _pluginId = pluginInfo?.Manifest.Id;
        return _pluginId;
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        StateStore.StateChanged += OnPluginStateChanged;
    }
    
    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        
        // Auto-load plugin assets on first render
        await LoadPluginAssetsAsync();
    }
    
    /// <summary>
    /// Loads all assets declared in this plugin's manifest.
    /// Called automatically on first render, but can be called manually if needed.
    /// Assets are deduplicated so multiple calls are safe.
    /// </summary>
    protected async Task LoadPluginAssetsAsync()
    {
        if (AssetLoader == null) return;
        
        var pluginState = Context.Services.GetService(typeof(PluginState)) as PluginState;
        var assembly = GetType().Assembly;
        var pluginInfo = pluginState?.Plugins.FirstOrDefault(p => p.Assembly == assembly);
        
        if (pluginInfo != null)
        {
            await AssetLoader.LoadPluginAssetsAsync(pluginInfo);
        }
    }

    private void OnPluginStateChanged(object? sender, PluginStateChangedEventArgs e)
    {
        InvokeAsync(StateHasChanged);
    }

    /// <inheritdoc />
    public virtual void Dispose()
    {
        StateStore.StateChanged -= OnPluginStateChanged;
        GC.SuppressFinalize(this);
    }
}
