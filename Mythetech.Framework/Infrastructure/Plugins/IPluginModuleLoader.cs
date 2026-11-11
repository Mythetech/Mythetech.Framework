using Microsoft.JSInterop;

namespace Mythetech.Framework.Infrastructure.Plugins;

/// <summary>
/// Imports a plugin's JavaScript files as ES modules.
/// </summary>
public interface IPluginModuleLoader
{
    /// <summary>
    /// Imports an ES module from a plugin.
    /// </summary>
    /// <param name="plugin">The plugin that owns the module</param>
    /// <param name="modulePath">
    /// Path inside the plugin: relative to its wwwroot, or <c>_content/{Package}/...</c>, with or without a leading slash.
    /// External URLs are rejected.
    /// </param>
    /// <returns>The module reference. The caller owns it and must dispose it.</returns>
    /// <exception cref="ArgumentException">The path is an external URL.</exception>
    /// <exception cref="JSDisconnectedException">The JavaScript runtime is gone, so there is no module to return.</exception>
    Task<IJSObjectReference> ImportAsync(PluginInfo plugin, string modulePath);
}
