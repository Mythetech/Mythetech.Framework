using System.Collections.Concurrent;

namespace Mythetech.Framework.Infrastructure.Plugins;

/// <summary>
/// Provides shared state storage for plugins.
/// State is isolated per-plugin using the plugin's ID as a namespace.
/// </summary>
public class PluginStateStore
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, object>> _pluginStates = new();
    private readonly PluginState _pluginState;

    /// <summary>
    /// Raised when any plugin state changes
    /// </summary>
    public event EventHandler<PluginStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Constructor
    /// </summary>
    public PluginStateStore(PluginState pluginState)
    {
        _pluginState = pluginState;
    }

    /// <summary>
    /// Get state for a plugin by explicit plugin ID
    /// </summary>
    public T? Get<T>(string pluginId, string key = "default")
    {
        if (_pluginStates.TryGetValue(pluginId, out var pluginState) &&
            pluginState.TryGetValue(key, out var value) &&
            value is T typed)
        {
            return typed;
        }
        return default;
    }

    /// <summary>
    /// Set state for a plugin by explicit plugin ID
    /// </summary>
    public void Set<T>(string pluginId, string key, T value)
    {
        var pluginState = _pluginStates.GetOrAdd(pluginId, _ => new ConcurrentDictionary<string, object>());
        pluginState[key] = value!;
        StateChanged?.Invoke(this, new PluginStateChangedEventArgs(pluginId, key));
    }

    /// <summary>
    /// Get state using assembly to infer plugin ID.
    /// Call this from plugin code - the assembly of the caller's type is used to find the plugin.
    /// </summary>
    public T? GetForPlugin<T>(Type callerType, string key = "default")
    {
        var pluginId = GetPluginIdForType(callerType);
        return pluginId != null ? Get<T>(pluginId, key) : default;
    }

    /// <summary>
    /// Set state using assembly to infer plugin ID.
    /// Call this from plugin code - the assembly of the caller's type is used to find the plugin.
    /// </summary>
    public void SetForPlugin<T>(Type callerType, string key, T value)
    {
        var pluginId = GetPluginIdForType(callerType);
        if (pluginId != null)
        {
            Set(pluginId, key, value);
        }
    }

    /// <summary>
    /// Clear all state for a specific plugin
    /// </summary>
    public void ClearPlugin(string pluginId)
    {
        _pluginStates.TryRemove(pluginId, out _);
        StateChanged?.Invoke(this, new PluginStateChangedEventArgs(pluginId, "*"));
    }

    private string? GetPluginIdForType(Type type)
    {
        var assembly = type.Assembly;
        var plugin = _pluginState.Plugins.FirstOrDefault(p => p.Assembly == assembly);
        return plugin?.Manifest.Id;
    }
}

/// <summary>
/// Event args for plugin state changes
/// </summary>
public class PluginStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// The plugin whose state changed
    /// </summary>
    public string PluginId { get; }
    
    /// <summary>
    /// The key that changed (or "*" for all keys)
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Constructor
    /// </summary>
    public PluginStateChangedEventArgs(string pluginId, string key)
    {
        PluginId = pluginId;
        Key = key;
    }
}

