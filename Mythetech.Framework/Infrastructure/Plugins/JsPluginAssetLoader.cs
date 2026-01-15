using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.JSInterop;

namespace Mythetech.Framework.Infrastructure.Plugins;

/// <summary>
/// JavaScript-based asset loader that works in both Desktop (WebView) and WebAssembly
/// </summary>
public class JsPluginAssetLoader : IPluginAssetLoader
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ConcurrentDictionary<string, bool> _loadedAssets = new();

    /// <summary>
    /// Constructor
    /// </summary>
    public JsPluginAssetLoader(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <inheritdoc />
    public async Task LoadStylesheetAsync(string href, string? integrity = null, string? crossOrigin = null)
    {
        if (_loadedAssets.ContainsKey(href))
            return;

        // Use JSON serialization to safely escape values for JavaScript
        var safeHref = JsonSerializer.Serialize(href);
        var safeIntegrity = integrity != null ? JsonSerializer.Serialize(integrity) : "null";
        var safeCrossOrigin = crossOrigin != null ? JsonSerializer.Serialize(crossOrigin) : "null";

        var js = $$"""
            (function() {
                var href = {{safeHref}};
                if (document.querySelector('link[href="' + href + '"]')) return;
                var link = document.createElement('link');
                link.rel = 'stylesheet';
                link.href = href;
                var integrity = {{safeIntegrity}};
                var crossOrigin = {{safeCrossOrigin}};
                if (integrity) link.integrity = integrity;
                if (crossOrigin) link.crossOrigin = crossOrigin;
                document.head.appendChild(link);
            })();
            """;

        await _jsRuntime.InvokeVoidAsync("eval", js);
        _loadedAssets[href] = true;
    }

    /// <inheritdoc />
    public async Task LoadScriptAsync(string src, string? integrity = null, string? crossOrigin = null)
    {
        if (_loadedAssets.ContainsKey(src))
            return;

        // Use JSON serialization to safely escape values for JavaScript
        var safeSrc = JsonSerializer.Serialize(src);
        var safeIntegrity = integrity != null ? JsonSerializer.Serialize(integrity) : "null";
        var safeCrossOrigin = crossOrigin != null ? JsonSerializer.Serialize(crossOrigin) : "null";

        var js = $$"""
            new Promise((resolve, reject) => {
                var src = {{safeSrc}};
                if (document.querySelector('script[src="' + src + '"]')) { resolve(); return; }
                var script = document.createElement('script');
                script.src = src;
                script.charset = 'utf-8';
                var integrity = {{safeIntegrity}};
                var crossOrigin = {{safeCrossOrigin}};
                if (integrity) script.integrity = integrity;
                if (crossOrigin) script.crossOrigin = crossOrigin;
                script.onload = resolve;
                script.onerror = reject;
                document.head.appendChild(script);
            });
            """;

        await _jsRuntime.InvokeVoidAsync("eval", js);
        _loadedAssets[src] = true;
    }

    /// <inheritdoc />
    public async Task LoadPluginAssetsAsync(PluginInfo pluginInfo)
    {
        var assemblyName = pluginInfo.Assembly.GetName().Name;
        
        foreach (var asset in pluginInfo.Manifest.Assets)
        {
            var path = ResolveAssetPath(asset.Path, assemblyName);
            
            switch (asset.Type)
            {
                case PluginAssetType.Css:
                    await LoadStylesheetAsync(path, asset.Integrity, asset.CrossOrigin);
                    break;
                case PluginAssetType.JavaScript:
                    await LoadScriptAsync(path, asset.Integrity, asset.CrossOrigin);
                    break;
            }
        }
    }

    /// <inheritdoc />
    public bool IsLoaded(string path)
    {
        return _loadedAssets.ContainsKey(path);
    }

    /// <inheritdoc />
    public async Task UnloadStylesheetAsync(string href)
    {
        var safeHref = JsonSerializer.Serialize(href);
        var js = $$"""
            (function() {
                var href = {{safeHref}};
                var link = document.querySelector('link[href="' + href + '"]');
                if (link) link.remove();
            })();
            """;

        await _jsRuntime.InvokeVoidAsync("eval", js);
        _loadedAssets.TryRemove(href, out _);
    }

    private static string ResolveAssetPath(string path, string? assemblyName)
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
        return $"/_content/{assemblyName}/{path}";
    }
}

