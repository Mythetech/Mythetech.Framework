using Mythetech.Framework.Infrastructure.Plugins.Installation;

namespace Mythetech.Framework.Infrastructure.Plugins.Local;

/// <summary>
/// Loads plugins from local build output folders and reloads them when a rebuild changes them.
/// A development feature, behind <see cref="PluginSettings.LocalPluginsEnabled"/>.
/// </summary>
public interface ILocalPluginService
{
    /// <summary>
    /// Whether this app can load local plugins.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Whether local plugins are turned on in the plugin settings.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// The local plugins currently tracked.
    /// </summary>
    IReadOnlyList<LocalPluginEntry> Entries { get; }

    /// <summary>
    /// Loads the saved local plugins and starts watching them. Does nothing unless available and enabled.
    /// </summary>
    Task LoadSavedAsync();

    /// <summary>
    /// Applies the local plugins setting. Turning it on loads the saved local plugins once plugins have loaded;
    /// turning it off stops watching, and loaded local plugins stay until restart.
    /// </summary>
    Task SetEnabledAsync(bool enabled);

    /// <summary>
    /// Loads the plugin in a build output DLL, registers it in place of any plugin with the same ID,
    /// saves its path and watches its folder.
    /// </summary>
    /// <param name="dllPath">Path to the plugin DLL in its build output folder</param>
    Task<PluginInstallResult> AddAsync(string dllPath);

    /// <summary>
    /// Stops tracking a local plugin and unregisters it, loading back any install it replaced.
    /// Its build output and stored data are kept.
    /// </summary>
    /// <returns>True when a local plugin with that ID was tracked</returns>
    Task<bool> RemoveAsync(string pluginId);
}
