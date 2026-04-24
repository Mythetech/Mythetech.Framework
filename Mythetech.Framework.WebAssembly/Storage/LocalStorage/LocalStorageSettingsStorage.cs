using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Mythetech.Framework.Infrastructure.Settings;

namespace Mythetech.Framework.WebAssembly.Storage.LocalStorage;

public class LocalStorageSettingsStorage : ISettingsStorage
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<LocalStorageSettingsStorage>? _logger;
    private const string KeyPrefix = "settings:";

    public LocalStorageSettingsStorage(IJSRuntime jsRuntime, ILogger<LocalStorageSettingsStorage>? logger = null)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SaveSettingsAsync(string settingsId, string jsonData)
    {
        var key = KeyPrefix + settingsId;
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, jsonData);
    }

    /// <inheritdoc />
    public async Task<string?> LoadSettingsAsync(string settingsId)
    {
        var key = KeyPrefix + settingsId;
        return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, string>> LoadAllSettingsAsync()
    {
        var result = new Dictionary<string, string>();

        try
        {
            for (var i = 0; ; i++)
            {
                var key = await _jsRuntime.InvokeAsync<string?>("localStorage.key", i);
                if (key == null)
                {
                    break;
                }

                if (key.StartsWith(KeyPrefix))
                {
                    var settingsId = key[KeyPrefix.Length..];
                    var value = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
                    if (value != null)
                    {
                        result[settingsId] = value;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Failed to load settings from localStorage");
        }

        return result;
    }
}
