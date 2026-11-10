using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Plugins;

namespace Mythetech.Framework.Desktop;

/// <summary>
/// Resolves a plugin asset path to a file in the plugin's folder or its static web assets manifest.
/// </summary>
internal static class PluginAssetPathResolver
{
    internal static string? Resolve(PluginInfo pluginInfo, string assetPath, ILogger logger)
    {
        // External URLs can't be resolved to files
        if (PluginUrls.IsExternalUrl(assetPath))
            return null;

        var pluginDir = Path.GetDirectoryName(pluginInfo.SourcePath);
        if (pluginDir == null) return null;

        var assemblyName = Path.GetFileNameWithoutExtension(pluginInfo.SourcePath);
        var manifest = StaticWebAssetsManifest.TryLoad(Path.Combine(pluginDir, $"{assemblyName}.staticwebassets.runtime.json"));
        if (manifest != null)
        {
            var trimmed = assetPath.TrimStart('/');
            var fromManifest = manifest.Resolve(trimmed);
            if (fromManifest == null && trimmed.StartsWith("_content/"))
            {
                var parts = trimmed.Split('/', 3);
                if (parts.Length == 3)
                    fromManifest = manifest.Resolve(parts[2]);
            }
            if (fromManifest != null)
                return fromManifest;
        }

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

        logger.LogWarning("Asset not found: {AssetPath} (plugin dir: {PluginDir})", assetPath, pluginDir);
        return null;
    }
}
