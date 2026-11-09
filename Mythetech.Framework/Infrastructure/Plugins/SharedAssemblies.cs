using System.Reflection;
using System.Runtime.Loader;

namespace Mythetech.Framework.Infrastructure.Plugins;

/// <summary>
/// Identifies assemblies the host provides, which every plugin must share instead of loading its own copy.
/// </summary>
internal static class SharedAssemblies
{
    private static readonly Lazy<HashSet<string>> TrustedPlatformAssemblyNames = new(ReadTrustedPlatformAssemblyNames);

    /// <summary>
    /// True when the host ships an assembly with this simple name or has already loaded one into the default context.
    /// </summary>
    public static bool IsShared(AssemblyName name)
    {
        return name.Name is not null
            && (TrustedPlatformAssemblyNames.Value.Contains(name.Name) || FindInDefault(name) is not null);
    }

    /// <summary>
    /// The assembly with this simple name already loaded into the default context, if any.
    /// </summary>
    public static Assembly? FindInDefault(AssemblyName name)
    {
        return AssemblyLoadContext.Default.Assemblies
            .FirstOrDefault(assembly => string.Equals(assembly.GetName().Name, name.Name, StringComparison.OrdinalIgnoreCase));
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
