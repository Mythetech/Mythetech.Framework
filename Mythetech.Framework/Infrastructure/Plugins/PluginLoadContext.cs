using System.Reflection;
using System.Runtime.Loader;

namespace Mythetech.Framework.Infrastructure.Plugins;

/// <summary>
/// Custom AssemblyLoadContext for loading plugins with their dependencies.
/// Resolves dependencies from the plugin's directory.
/// </summary>
public class PluginLoadContext : AssemblyLoadContext
{
    private readonly string _pluginDirectory;

    /// <summary>
    /// Create a load context for a plugin
    /// </summary>
    /// <param name="pluginPath">Full path to the plugin DLL</param>
    public PluginLoadContext(string pluginPath) : base(isCollectible: true)
    {
        _pluginDirectory = Path.GetDirectoryName(pluginPath) ?? string.Empty;
    }

    /// <inheritdoc />
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Try to find the DLL in the plugin's directory
        var dllPath = Path.Combine(_pluginDirectory, $"{assemblyName.Name}.dll");
        if (File.Exists(dllPath))
        {
            return LoadFromAssemblyPath(dllPath);
        }

        // Fall back to default context (shared assemblies like MudBlazor, Framework, etc.)
        return null;
    }
}
