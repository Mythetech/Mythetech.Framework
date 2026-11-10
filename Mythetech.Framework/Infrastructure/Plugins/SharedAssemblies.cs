using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Loader;

namespace Mythetech.Framework.Infrastructure.Plugins;

/// <summary>
/// Finds the host's copies of assemblies, which every plugin must share instead of loading its own copy.
/// </summary>
internal static class SharedAssemblies
{
    private static readonly Lazy<HashSet<string>> TrustedPlatformAssemblyNames = new(ReadTrustedPlatformAssemblyNames);
    private static readonly ConcurrentDictionary<string, Assembly?> HostCopies = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The host's copy of the assembly with this simple name, or null when the host has none and the plugin uses its own.
    /// </summary>
    public static Assembly? TryGetHostCopy(AssemblyName name) =>
        TryGetHostCopy(name, TrustedPlatformAssemblyNames.Value, HostCopies);

    internal static Assembly? TryGetHostCopy(
        AssemblyName name, IReadOnlySet<string> trustedNames, ConcurrentDictionary<string, Assembly?> cache)
    {
        var simpleName = name.Name;
        if (string.IsNullOrEmpty(simpleName))
        {
            return null;
        }

        // A cached miss is rechecked against what's loaded, since the host can still load that name itself.
        if (cache.TryGetValue(simpleName, out var cached))
        {
            return cached ?? FindInDefault(simpleName);
        }

        return cache.GetOrAdd(simpleName, FindInDefault(simpleName) ?? LoadFromDefault(simpleName, trustedNames));
    }

    private static Assembly? FindInDefault(string simpleName)
    {
        return AssemblyLoadContext.Default.Assemblies
            .FirstOrDefault(assembly => string.Equals(assembly.GetName().Name, simpleName, StringComparison.OrdinalIgnoreCase));
    }

    private static Assembly? LoadFromDefault(string simpleName, IReadOnlySet<string> trustedNames)
    {
        // By simple name, so a copy at a different version than the host's still binds to the host's.
        var name = new AssemblyName(simpleName);
        if (trustedNames.Contains(simpleName))
        {
            return AssemblyLoadContext.Default.LoadFromAssemblyName(name);
        }

        // Single-file hosts list few or none of their bundled assemblies as trusted, so ask the default context.
        try
        {
            return AssemblyLoadContext.Default.LoadFromAssemblyName(name);
        }
        catch (Exception ex) when (ex is FileNotFoundException or FileLoadException or BadImageFormatException)
        {
            return null;
        }
    }

    private static HashSet<string> ReadTrustedPlatformAssemblyNames()
    {
        var paths = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? string.Empty;
        return paths
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(Path.GetFileNameWithoutExtension)
            .OfType<string>()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
