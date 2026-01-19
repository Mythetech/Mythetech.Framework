using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Mythetech.Framework.Infrastructure.Settings;

namespace Mythetech.Framework.WebAssembly.Settings;

/// <summary>
/// localStorage-based settings storage for WebAssembly applications.
/// Uses a different key prefix from plugin storage to keep app settings
/// isolated from plugin data.
/// </summary>
public class LocalStorageSettingsStorage : ISettingsStorage
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<LocalStorageSettingsStorage>? _logger;
    private const string KeyPrefix = "settings:";

    /// <summary>
    /// Creates a new localStorage settings storage instance.
    /// </summary>
    /// <param name="jsRuntime">JS runtime for interop</param>
    /// <param name="logger">Optional logger for diagnostics</param>
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
            // Iterate through localStorage keys without using eval
            // localStorage.key(i) returns null when index is out of bounds
            for (var i = 0; ; i++)
            {
                var key = await _jsRuntime.InvokeAsync<string?>("localStorage.key", i);
                if (key == null)
                {
                    break; // No more keys
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
            // JS interop may fail in certain contexts (e.g., SSR, prerendering)
            _logger?.LogDebug(ex, "Failed to load settings from localStorage");
        }

        return result;
    }
}
