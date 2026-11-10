namespace Mythetech.Framework.Infrastructure.Plugins.Installation;

/// <summary>
/// Installs and upgrades plugins without a restart. Each install loads from a new folder, and the folder it
/// replaces is deleted at the next startup, so no loaded file is ever deleted.
/// </summary>
public interface IPluginInstaller
{
    /// <summary>
    /// Downloads a plugin package or DLL and installs it.
    /// </summary>
    /// <param name="url">Where to download the package from</param>
    /// <param name="pluginKey">Key naming the plugin's folder, such as its registry name</param>
    /// <param name="cancellationToken">Cancels the download</param>
    Task<PluginInstallResult> InstallFromUrlAsync(string url, string pluginKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Installs a plugin package (a zip) or a bare plugin DLL.
    /// </summary>
    /// <param name="package">The package contents</param>
    /// <param name="fileName">The package's file name, used to save a bare DLL</param>
    /// <param name="pluginKey">Key naming the plugin's folder, such as its registry name</param>
    Task<PluginInstallResult> InstallFromStreamAsync(Stream package, string fileName, string pluginKey);

    /// <summary>
    /// Loads a plugin DLL where it is and registers it for this session only, replacing any plugin with its ID.
    /// </summary>
    /// <param name="dllPath">Path to the plugin DLL</param>
    Task<PluginInstallResult> ImportFromFileAsync(string dllPath);
}
