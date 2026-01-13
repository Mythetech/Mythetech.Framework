using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Mythetech.Framework.Infrastructure.Plugins;

namespace Mythetech.Framework.WebAssembly.Plugins;

/// <summary>
/// localStorage-based plugin state provider for WebAssembly applications.
/// Persists which plugins are disabled across browser sessions.
/// </summary>
public class LocalStoragePluginStateProvider : IPluginStateProvider
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<LocalStoragePluginStateProvider>? _logger;
    private const string StorageKey = "plugin:disabled";

    /// <summary>
    /// Creates a new localStorage plugin state provider.
    /// </summary>
    /// <param name="jsRuntime">JS runtime for interop</param>
    /// <param name="logger">Optional logger for error reporting</param>
    public LocalStoragePluginStateProvider(IJSRuntime jsRuntime, ILogger<LocalStoragePluginStateProvider>? logger = null)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlySet<string>> LoadDisabledPluginsAsync()
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            if (string.IsNullOrEmpty(json))
            {
                return new HashSet<string>();
            }

            var plugins = JsonSerializer.Deserialize<HashSet<string>>(json);
            _logger?.LogDebug("Loaded {Count} disabled plugins from localStorage", plugins?.Count ?? 0);
            return plugins ?? new HashSet<string>();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load disabled plugins from localStorage");
            return new HashSet<string>();
        }
    }

    /// <inheritdoc />
    public async Task SaveDisabledPluginsAsync(IReadOnlySet<string> disabledPlugins)
    {
        try
        {
            var json = JsonSerializer.Serialize(disabledPlugins);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
            _logger?.LogDebug("Saved {Count} disabled plugins to localStorage", disabledPlugins.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save disabled plugins to localStorage");
        }
    }
}
