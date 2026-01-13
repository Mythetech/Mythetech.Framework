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
    private const string KeyPrefix = "settings:";

    /// <summary>
    /// Creates a new localStorage settings storage instance.
    /// </summary>
    /// <param name="jsRuntime">JS runtime for interop</param>
    public LocalStorageSettingsStorage(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
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
            // Get all localStorage keys
            var allKeys = await _jsRuntime.InvokeAsync<string[]>("eval", "Object.keys(localStorage)");

            // Filter to settings keys and load values
            foreach (var key in allKeys.Where(k => k.StartsWith(KeyPrefix)))
            {
                var settingsId = key[KeyPrefix.Length..];
                var value = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
                if (value != null)
                {
                    result[settingsId] = value;
                }
            }
        }
        catch
        {
            // JS interop may fail in certain contexts
        }

        return result;
    }
}
