using System.Text.Json;
using Microsoft.JSInterop;
using Mythetech.Framework.Infrastructure.Plugins;

namespace Mythetech.Framework.WebAssembly.Storage.LocalStorage;

public class LocalStoragePluginStorageFactory : IPluginStorageFactory
{
    private readonly IJSRuntime _jsRuntime;

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
