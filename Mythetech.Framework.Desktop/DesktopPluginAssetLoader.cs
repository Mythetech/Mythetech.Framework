using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Mythetech.Framework.Infrastructure.Plugins;

namespace Mythetech.Framework.Desktop;

/// <summary>
/// Desktop-specific asset loader that reads CSS/JS files from plugin directories
/// and injects them inline, since dynamic plugins can't use _content/ URLs.
/// </summary>
public class DesktopPluginAssetLoader : IPluginAssetLoader
{
    private readonly IJSRuntime _jsRuntime;
    private readonly PluginState _pluginState;
    private readonly ILogger<DesktopPluginAssetLoader> _logger;
    private readonly ConcurrentDictionary<string, bool> _loadedAssets = new();

    public DesktopPluginAssetLoader(
        IJSRuntime jsRuntime, 
        PluginState pluginState,
        ILogger<DesktopPluginAssetLoader> logger)
    {
        _jsRuntime = jsRuntime;
        _pluginState = pluginState;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task LoadStylesheetAsync(string href, string? integrity = null, string? crossOrigin = null)
    {
        if (_loadedAssets.ContainsKey(href))
            return;

        // For external URLs, use standard link tag
        if (IsExternalUrl(href))
        {
            await LoadStylesheetViaLinkAsync(href, integrity, crossOrigin);
            return;
        }

        // For local paths, try to read and inject inline
        var content = await TryReadAssetContentAsync(href);
        if (content != null)
        {
            await InjectInlineStyleAsync(href, content);
        }
        else
        {
            _logger.LogWarning("Could not load stylesheet: {Path}", href);
        }
    }

    /// <inheritdoc />
    public async Task LoadScriptAsync(string src, string? integrity = null, string? crossOrigin = null)
    {
        if (_loadedAssets.ContainsKey(src))
            return;

        // For external URLs, use standard script tag
        if (IsExternalUrl(src))
        {
            await LoadScriptViaSrcAsync(src, integrity, crossOrigin);
            return;
        }

        // For local paths, try to read and inject inline
        var content = await TryReadAssetContentAsync(src);
        if (content != null)
        {
            await InjectInlineScriptAsync(src, content);
        }
        else
        {
            _logger.LogWarning("Could not load script: {Path}", src);
        }
    }

    /// <inheritdoc />
    public async Task LoadPluginAssetsAsync(PluginInfo pluginInfo)
    {
        foreach (var asset in pluginInfo.Manifest.Assets)
        {
            var resolvedPath = ResolveAssetToFilePath(pluginInfo, asset.Path);
            
            _logger.LogDebug("Loading plugin asset: {OriginalPath} -> {ResolvedPath}", 
                asset.Path, resolvedPath ?? "not found");
            
            switch (asset.Type)
            {
                case PluginAssetType.Css:
                    if (resolvedPath != null && File.Exists(resolvedPath))
                    {
                        var content = await File.ReadAllTextAsync(resolvedPath);
                        await InjectInlineStyleAsync(asset.Path, content);
                    }
                    else if (IsExternalUrl(asset.Path))
                    {
                        await LoadStylesheetViaLinkAsync(asset.Path, asset.Integrity, asset.CrossOrigin);
                    }
                    else
                    {
                        _logger.LogWarning("CSS asset not found: {Path}", asset.Path);
                    }
                    break;
                    
                case PluginAssetType.JavaScript:
                    if (resolvedPath != null && File.Exists(resolvedPath))
                    {
                        var content = await File.ReadAllTextAsync(resolvedPath);
                        await InjectInlineScriptAsync(asset.Path, content);
                    }
                    else if (IsExternalUrl(asset.Path))
                    {
                        await LoadScriptViaSrcAsync(asset.Path, asset.Integrity, asset.CrossOrigin);
                    }
                    else
                    {
                        _logger.LogWarning("JS asset not found: {Path}", asset.Path);
                    }
                    break;
            }
        }
    }

    /// <inheritdoc />
    public bool IsLoaded(string path) => _loadedAssets.ContainsKey(path);

    /// <inheritdoc />
    public async Task UnloadStylesheetAsync(string href)
    {
        var id = GetAssetId(href);
        var js = $"document.getElementById('{id}')?.remove();";
        await _jsRuntime.InvokeVoidAsync("eval", js);
        _loadedAssets.TryRemove(href, out _);
    }

    private async Task InjectInlineStyleAsync(string id, string content)
    {
        var safeId = GetAssetId(id);
        
        // Use Base64 encoding to safely transfer the CSS content.
        // This avoids all escaping issues with template literals.
        var base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(content));
        
        var js = $$"""
            (function() {
                if (document.getElementById('{{safeId}}')) return;
                try {
                    var binary = atob('{{base64Content}}');
                    var bytes = new Uint8Array(binary.length);
                    for (var i = 0; i < binary.length; i++) {
                        bytes[i] = binary.charCodeAt(i);
                    }
                    var decoded = new TextDecoder('utf-8').decode(bytes);
                    var style = document.createElement('style');
                    style.id = '{{safeId}}';
                    style.textContent = decoded;
                    document.head.appendChild(style);
                    console.log('Plugin CSS loaded: {{id}}');
                } catch (e) {
                    console.error('Failed to load plugin CSS {{id}}:', e);
                }
            })();
            """;

        await _jsRuntime.InvokeVoidAsync("eval", js);
        _loadedAssets[id] = true;
        _logger.LogDebug("Injected inline CSS: {Id}", id);
    }

    private async Task InjectInlineScriptAsync(string id, string content)
    {
        var safeId = GetAssetId(id);
        
        // Use Base64 encoding to safely transfer the script content.
        // This avoids all escaping issues with template literals and complex JS.
        var base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(content));
                
        // The script decodes the Base64 and creates a script element with the decoded content.
        // Using a script element (not eval) ensures proper global scope execution.
        var js = $$"""
            (function() {
                if (document.getElementById('{{safeId}}')) return;
                try {
                    var binary = atob('{{base64Content}}');
                    var bytes = new Uint8Array(binary.length);
                    for (var i = 0; i < binary.length; i++) {
                        bytes[i] = binary.charCodeAt(i);
                    }
                    var decoded = new TextDecoder('utf-8').decode(bytes);
                    var script = document.createElement('script');
                    script.id = '{{safeId}}';
                    script.textContent = decoded;
                    document.head.appendChild(script);
                    console.log('Plugin asset loaded: {{id}}');
                } catch (e) {
                    console.error('Failed to load plugin asset {{id}}:', e);
                }
            })();
            """;
        
        try
        {
            await _jsRuntime.InvokeVoidAsync("eval", js);
            _loadedAssets[id] = true;
            _logger.LogInformation("Injected inline JS: {Id} ({Length} bytes)", id, content.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to inject inline JS: {Id}", id);
            throw;
        }
    }

    private async Task LoadStylesheetViaLinkAsync(string href, string? integrity, string? crossOrigin)
    {
        var js = $$"""
            (function() {
                if (document.querySelector('link[href="{{href}}"]')) return;
                var link = document.createElement('link');
                link.rel = 'stylesheet';
                link.href = '{{href}}';
                {{(integrity != null ? $"link.integrity = '{integrity}';" : "")}}
                {{(crossOrigin != null ? $"link.crossOrigin = '{crossOrigin}';" : "")}}
                document.head.appendChild(link);
            })();
            """;

        await _jsRuntime.InvokeVoidAsync("eval", js);
        _loadedAssets[href] = true;
    }

    private async Task LoadScriptViaSrcAsync(string src, string? integrity, string? crossOrigin)
    {
        var js = $$"""
            new Promise((resolve, reject) => {
                if (document.querySelector('script[src="{{src}}"]')) { resolve(); return; }
                var script = document.createElement('script');
                script.src = '{{src}}';
                script.charset = 'utf-8';
                {{(integrity != null ? $"script.integrity = '{integrity}';" : "")}}
                {{(crossOrigin != null ? $"script.crossOrigin = '{crossOrigin}';" : "")}}
                script.onload = resolve;
                script.onerror = reject;
                document.head.appendChild(script);
            });
            """;

        await _jsRuntime.InvokeVoidAsync("eval", js);
        _loadedAssets[src] = true;
    }

    private string? ResolveAssetToFilePath(PluginInfo pluginInfo, string assetPath)
    {
        // External URLs can't be resolved to files
        if (IsExternalUrl(assetPath))
            return null;

        var pluginDir = Path.GetDirectoryName(pluginInfo.SourcePath);
        if (pluginDir == null) return null;

        // Case 1: _content/{PackageName}/path - this is how RCL assets are referenced
        if (assetPath.StartsWith("/_content/") || assetPath.StartsWith("_content/"))
        {
            var contentPath = assetPath.TrimStart('/');
            
            // Try: {pluginDir}/wwwroot/_content/{PackageName}/{path}
            var wwwrootContentPath = Path.Combine(pluginDir, "wwwroot", contentPath);
            if (File.Exists(wwwrootContentPath))
                return wwwrootContentPath;
            
            // Try: {pluginDir}/_content/{PackageName}/{path}
            var directContentPath = Path.Combine(pluginDir, contentPath);
            if (File.Exists(directContentPath))
                return directContentPath;
            
            // Parse the path to get package name and relative path
            var parts = contentPath.Split('/', 3);
            if (parts.Length >= 3)
            {
                var relativePath = parts[2];
                
                // Try: {pluginDir}/wwwroot/{relativePath}
                var wwwrootOnly = Path.Combine(pluginDir, "wwwroot", relativePath);
                if (File.Exists(wwwrootOnly))
                    return wwwrootOnly;
                
                // Try: {pluginDir}/{relativePath}
                var directPath = Path.Combine(pluginDir, relativePath);
                if (File.Exists(directPath))
                    return directPath;
            }
        }
        
        // Case 2: Relative path - look in plugin's wwwroot
        var wwwrootPath = Path.Combine(pluginDir, "wwwroot", assetPath);
        if (File.Exists(wwwrootPath))
            return wwwrootPath;
        
        // Case 3: Try direct path from plugin directory
        var directFromPlugin = Path.Combine(pluginDir, assetPath);
        if (File.Exists(directFromPlugin))
            return directFromPlugin;

        _logger.LogWarning("Asset not found: {AssetPath} (plugin dir: {PluginDir})", assetPath, pluginDir);
        return null;
    }

    private async Task<string?> TryReadAssetContentAsync(string path)
    {
        // Try to find which plugin this asset belongs to
        foreach (var plugin in _pluginState.Plugins)
        {
            var resolved = ResolveAssetToFilePath(plugin, path);
            if (resolved != null && File.Exists(resolved))
            {
                return await File.ReadAllTextAsync(resolved);
            }
        }
        return null;
    }

    private static bool IsExternalUrl(string path) =>
        path.StartsWith("http://") || path.StartsWith("https://") || path.StartsWith("//");

    private static string GetAssetId(string path) =>
        "plugin-asset-" + path.GetHashCode().ToString("x");
}

