using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using Mythetech.Framework.Infrastructure.Plugins;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Plugins;

public class SharedAssembliesTests
{
    // A single-file host lists none of its bundled assemblies as trusted.
    private static readonly HashSet<string> NoTrustedNames = new(StringComparer.OrdinalIgnoreCase);

    [Fact(DisplayName = "TryGetHostCopy finds a host assembly not yet loaded when no trusted names are listed")]
    public void TryGetHostCopy_NoTrustedNames_HostAssemblyNotYetLoaded_ReturnsDefaultCopy()
    {
        var name = UnloadedRuntimeAssemblyName();

        var hostCopy = SharedAssemblies.TryGetHostCopy(new AssemblyName(name), NoTrustedNames, NewCache());

        hostCopy.ShouldNotBeNull().GetName().Name.ShouldBe(name);
        AssemblyLoadContext.GetLoadContext(hostCopy).ShouldBeSameAs(AssemblyLoadContext.Default);
    }

    [Fact(DisplayName = "TryGetHostCopy finds a host assembly already loaded when no trusted names are listed")]
    public void TryGetHostCopy_NoTrustedNames_HostAssemblyLoaded_ReturnsDefaultCopy()
    {
        var framework = typeof(IPluginManifest).Assembly;

        var hostCopy = SharedAssemblies.TryGetHostCopy(framework.GetName(), NoTrustedNames, NewCache());

        hostCopy.ShouldBeSameAs(framework);
    }

    [Fact(DisplayName = "TryGetHostCopy returns null for a name no host has, and caches the miss")]
    public void TryGetHostCopy_NoTrustedNames_UnknownName_ReturnsNullAndCachesMiss()
    {
        var name = $"NoSuchHostAssembly_{Guid.NewGuid():N}";
        var cache = NewCache();

        var hostCopy = SharedAssemblies.TryGetHostCopy(new AssemblyName(name), NoTrustedNames, cache);

        hostCopy.ShouldBeNull();
        cache.ShouldContainKey(name);
        SharedAssemblies.TryGetHostCopy(new AssemblyName(name), NoTrustedNames, cache).ShouldBeNull();
    }

    private static ConcurrentDictionary<string, Assembly?> NewCache() => new(StringComparer.OrdinalIgnoreCase);

    // Any shared framework assembly the default context hasn't loaded, so the lookup has to go past the loaded set.
    private static string UnloadedRuntimeAssemblyName()
    {
        var loaded = AssemblyLoadContext.Default.Assemblies
            .Select(assembly => assembly.GetName().Name)
            .OfType<string>()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var runtimeDirectory = RuntimeEnvironment.GetRuntimeDirectory();

        return (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Where(path => path.StartsWith(runtimeDirectory, StringComparison.Ordinal))
            .Select(Path.GetFileNameWithoutExtension)
            .OfType<string>()
            .Order(StringComparer.Ordinal)
            .First(name => !loaded.Contains(name));
    }
}
