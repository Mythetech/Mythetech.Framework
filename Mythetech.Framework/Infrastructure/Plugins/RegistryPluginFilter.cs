namespace Mythetech.Framework.Infrastructure.Plugins;

/// <summary>
/// Decides which registry entries the plugin browser lists.
/// </summary>
public static class RegistryPluginFilter
{
    /// <summary>
    /// Returns true when the entry should be offered: not deprecated, visible in this environment,
    /// and either not installed or newer than the installed version.
    /// </summary>
    public static bool ShouldList(RegistryPluginEntry entry, PluginInfo? installed, bool isProduction, bool showPreview)
    {
        if (entry.IsDeprecated)
            return false;

        if (entry.IsDevPlugin && isProduction)
            return false;

        if (entry.IsPreview && !showPreview)
            return false;

        if (installed is null)
            return true;

        var registryVersion = PluginInfo.ParseVersion(entry.Version);
        if (registryVersion is null)
            return false;

        return installed.IsOlderThan(registryVersion);
    }
}
