using System.Reflection;
using System.Runtime.Loader;

namespace Mythetech.Framework.Infrastructure.Plugins;

/// <summary>
/// Collectible AssemblyLoadContext for one plugin folder.
/// Assemblies the host provides resolve from the host; everything else resolves from the plugin's folder.
/// </summary>
public class PluginLoadContext : AssemblyLoadContext
{
    private readonly string _pluginDirectory;
    private readonly AssemblyDependencyResolver? _resolver;

    /// <summary>
    /// Create a load context for a plugin folder
    /// </summary>
    /// <param name="pluginDirectory">Folder containing the plugin and its dependencies</param>
    /// <param name="mainAssemblyPath">Full path to the plugin DLL, whose .deps.json, when present, resolves dependencies</param>
    public PluginLoadContext(string pluginDirectory, string? mainAssemblyPath = null) : base(isCollectible: true)
    {
        _pluginDirectory = pluginDirectory;

        // The browser has no AssemblyDependencyResolver.
        if (!OperatingSystem.IsBrowser()
            && mainAssemblyPath is not null
            && File.Exists(Path.ChangeExtension(mainAssemblyPath, ".deps.json")))
        {
            _resolver = new AssemblyDependencyResolver(mainAssemblyPath);
        }
    }

    /// <inheritdoc />
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // A second copy of a host assembly would give the plugin different types than the host's.
        if (SharedAssemblies.IsShared(assemblyName))
        {
            return null;
        }

        var path = ResolveAssemblyPath(assemblyName);
        return path is null ? null : LoadFromAssemblyPath(path);
    }

    /// <inheritdoc />
    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        if (_resolver is null || OperatingSystem.IsBrowser())
        {
            return IntPtr.Zero;
        }

        var path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return path is null ? IntPtr.Zero : LoadUnmanagedDllFromPath(path);
    }

    private string? ResolveAssemblyPath(AssemblyName assemblyName)
    {
        if (_resolver is not null && !OperatingSystem.IsBrowser())
        {
            var resolved = _resolver.ResolveAssemblyToPath(assemblyName);
            if (resolved is not null)
            {
                return resolved;
            }
        }

        var path = Path.Combine(_pluginDirectory, $"{assemblyName.Name}.dll");
        return File.Exists(path) ? path : null;
    }
}
