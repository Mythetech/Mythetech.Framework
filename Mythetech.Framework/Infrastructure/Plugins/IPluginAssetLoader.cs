namespace Mythetech.Framework.Infrastructure.Plugins;

/// <summary>
/// Service for dynamically loading plugin assets (CSS/JS) at runtime
/// </summary>
public interface IPluginAssetLoader
{
    /// <summary>
    /// Load a CSS stylesheet
    /// </summary>
    /// <param name="href">URL or path to the stylesheet</param>
    /// <param name="integrity">Optional SRI hash</param>
    /// <param name="crossOrigin">Optional crossorigin attribute</param>
    Task LoadStylesheetAsync(string href, string? integrity = null, string? crossOrigin = null);
    
    /// <summary>
    /// Load a JavaScript file
    /// </summary>
    /// <param name="src">URL or path to the script</param>
    /// <param name="integrity">Optional SRI hash</param>
    /// <param name="crossOrigin">Optional crossorigin attribute</param>
    Task LoadScriptAsync(string src, string? integrity = null, string? crossOrigin = null);
    
    /// <summary>
    /// Load all assets declared by a plugin
    /// </summary>
    /// <param name="pluginInfo">The plugin whose assets to load</param>
    Task LoadPluginAssetsAsync(PluginInfo pluginInfo);
    
    /// <summary>
    /// Check if an asset has already been loaded
    /// </summary>
    /// <param name="path">The asset path</param>
    bool IsLoaded(string path);
    
    /// <summary>
    /// Unload a CSS stylesheet (remove from DOM)
    /// </summary>
    /// <param name="href">URL or path to the stylesheet</param>
    Task UnloadStylesheetAsync(string href);
}

