using System.Text.Json;
using Microsoft.JSInterop;
using Mythetech.Framework.Infrastructure.Plugins;

namespace Mythetech.Framework.WebAssembly.Storage.LocalStorage;

public class LocalStoragePluginStorage : IPluginStorage
{
    private readonly IJSRuntime _jsRuntime;
    private readonly string _pluginId;
    private readonly string _keyPrefix;

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
