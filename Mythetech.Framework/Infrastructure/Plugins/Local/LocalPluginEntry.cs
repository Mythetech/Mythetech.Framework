namespace Mythetech.Framework.Infrastructure.Plugins.Local;

/// <summary>
/// A local plugin tracked by <see cref="ILocalPluginService"/>.
/// </summary>
/// <param name="PluginId">The plugin's manifest ID</param>
/// <param name="DllPath">Full path to the plugin DLL in its build output folder</param>
public sealed record LocalPluginEntry(string PluginId, string DllPath);
