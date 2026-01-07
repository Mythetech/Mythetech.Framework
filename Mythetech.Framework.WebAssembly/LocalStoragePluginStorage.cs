using System.Text.Json;
using Microsoft.JSInterop;
using Mythetech.Framework.Infrastructure.Plugins;

namespace Mythetech.Framework.WebAssembly;

/// <summary>
/// localStorage-based plugin storage for WebAssembly.
/// Each plugin's data is prefixed with its plugin ID.
/// </summary>
public class LocalStoragePluginStorage : IPluginStorage
{
    private readonly IJSRuntime _jsRuntime;
    private readonly string _pluginId;
    private readonly string _keyPrefix;

    /// <summary>
    /// Constructor
    /// </summary>
    public LocalStoragePluginStorage(IJSRuntime jsRuntime, string pluginId)
    {
        _jsRuntime = jsRuntime;
        _pluginId = pluginId;
        _keyPrefix = $"plugin:{pluginId}:";
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key)
    {
        var fullKey = _keyPrefix + key;
        var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", fullKey);
        
        if (string.IsNullOrEmpty(json))
            return default;
        
        return JsonSerializer.Deserialize<T>(json);
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value)
    {
        var fullKey = _keyPrefix + key;
        var json = JsonSerializer.Serialize(value);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", fullKey, json);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string key)
    {
        var fullKey = _keyPrefix + key;
        var exists = await ExistsAsync(key);
        
        if (exists)
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", fullKey);
        }
        
        return exists;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string key)
    {
        var fullKey = _keyPrefix + key;
        var value = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", fullKey);
        return value != null;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetKeysAsync(string? prefix = null)
    {
        var allKeys = await _jsRuntime.InvokeAsync<string[]>("eval", 
            "Object.keys(localStorage)");
        
        var pluginKeys = allKeys
            .Where(k => k.StartsWith(_keyPrefix))
            .Select(k => k[_keyPrefix.Length..]);
        
        if (prefix != null)
        {
            pluginKeys = pluginKeys.Where(k => k.StartsWith(prefix));
        }
        
        return pluginKeys.ToList();
    }

    /// <inheritdoc />
    public async Task ClearAsync()
    {
        var keys = await GetKeysAsync();
        foreach (var key in keys)
        {
            await DeleteAsync(key);
        }
    }
}

/// <summary>
/// Factory for creating LocalStorage-based plugin storage instances
/// </summary>
public class LocalStoragePluginStorageFactory : IPluginStorageFactory
{
    private readonly IJSRuntime _jsRuntime;

    /// <summary>
    /// Constructor
    /// </summary>
    public LocalStoragePluginStorageFactory(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <inheritdoc />
    public IPluginStorage? CreateForPlugin(string pluginId)
    {
        return new LocalStoragePluginStorage(_jsRuntime, pluginId);
    }

    /// <inheritdoc />
    public async Task<string> ExportPluginDataAsync(string pluginId)
    {
        var storage = CreateForPlugin(pluginId)!;
        var keys = await storage.GetKeysAsync();
        var data = new Dictionary<string, string>();
        
        foreach (var key in keys)
        {
            var json = await _jsRuntime.InvokeAsync<string?>(
                "localStorage.getItem", $"plugin:{pluginId}:{key}");
            if (json != null)
            {
                data[key] = json;
            }
        }
        
        return JsonSerializer.Serialize(data);
    }

    /// <inheritdoc />
    public async Task ImportPluginDataAsync(string pluginId, string jsonData)
    {
        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonData);
        if (data == null) return;
        
        foreach (var (key, value) in data)
        {
            await _jsRuntime.InvokeVoidAsync(
                "localStorage.setItem", $"plugin:{pluginId}:{key}", value);
        }
    }

    /// <inheritdoc />
    public async Task DeletePluginDataAsync(string pluginId)
    {
        var storage = CreateForPlugin(pluginId)!;
        await storage.ClearAsync();
    }
}

