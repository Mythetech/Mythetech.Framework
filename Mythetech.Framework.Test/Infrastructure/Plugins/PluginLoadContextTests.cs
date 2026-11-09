using System.Runtime.Loader;
using Microsoft.Extensions.Logging.Abstractions;
using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Framework.Test.Infrastructure.Plugins.TestPlugins;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Plugins;

public class PluginLoadContextTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"plugin_load_context_test_{Guid.NewGuid():N}");
    private readonly PluginLoader _loader = new(NullLogger<PluginLoader>.Instance, new PluginState());

    [Fact(DisplayName = "LoadPluginsFromFolder loads a plugin into its own PluginLoadContext")]
    public void LoadPluginsFromFolder_LoadsPluginIntoItsOwnContext()
    {
        var folder = NewFolder();
        TestPluginBuilder.Build(folder, UniqueName("OwnContext"), "mythetech.test.own-context", new Version(1, 0, 0));

        var plugin = _loader.LoadPluginsFromFolder(folder).ShouldHaveSingleItem();

        plugin.LoadContext.ShouldBeOfType<PluginLoadContext>();
        AssemblyLoadContext.GetLoadContext(plugin.Assembly).ShouldBeSameAs(plugin.LoadContext);
    }

    [Fact(DisplayName = "LoadPluginsFromFolder loads both copies of an assembly name found in two folders")]
    public void LoadPluginsFromFolder_SameAssemblyNameInTwoFolders_LoadsBoth()
    {
        var assemblyName = UniqueName("TwoFolders");
        var folderA = NewFolder();
        var folderB = NewFolder();
        TestPluginBuilder.Build(folderA, assemblyName, "mythetech.test.two-folders", new Version(1, 0, 0));
        TestPluginBuilder.Build(folderB, assemblyName, "mythetech.test.two-folders", new Version(2, 0, 0));

        var a = _loader.LoadPluginsFromFolder(folderA).ShouldHaveSingleItem();
        var b = _loader.LoadPluginsFromFolder(folderB).ShouldHaveSingleItem();

        b.Manifest.Version.ShouldBe(new Version(2, 0, 0));
        b.Assembly.ShouldNotBeSameAs(a.Assembly);
    }

    [Fact(DisplayName = "LoadPluginsFromFolder uses the host's Mythetech.Framework when the folder carries its own copy")]
    public void LoadPluginsFromFolder_FolderContainsHostFrameworkCopy_UsesHostCopy()
    {
        var folder = NewFolder();
        TestPluginBuilder.Build(folder, UniqueName("FrameworkCopy"), "mythetech.test.framework-copy", new Version(1, 0, 0));
        var frameworkPath = typeof(IPluginManifest).Assembly.Location;
        File.Copy(frameworkPath, Path.Combine(folder, Path.GetFileName(frameworkPath)));

        var plugins = _loader.LoadPluginsFromFolder(folder);

        var plugin = plugins.ShouldHaveSingleItem();
        AssemblyLoadContext.GetLoadContext(typeof(IPluginManifest).Assembly).ShouldBeSameAs(AssemblyLoadContext.Default);
        plugin.LoadContext.ShouldNotBeNull().Assemblies
            .ShouldNotContain(assembly => assembly.GetName().Name == typeof(IPluginManifest).Assembly.GetName().Name);
    }

    [Fact(DisplayName = "LoadPluginsFromFolder resolves a plugin's dependency from its folder")]
    public void LoadPluginsFromFolder_DependencyInFolder_Resolves()
    {
        var folder = NewFolder();
        var depPath = TestPluginBuilder.BuildLibrary(folder, UniqueName("Dep"), """
            public static class Dep
            {
                public static string Value => "dep";
            }
            """);
        TestPluginBuilder.Build(folder, UniqueName("UsesDep"), "mythetech.test.uses-dep", new Version(1, 0, 0),
            extraSource: $$"""
                namespace {{TestPluginBuilder.ManifestNamespace}};

                public partial class Manifest
                {
                    static partial void OverrideDescription(ref string description) => description = Dep.Value;
                }
                """,
            extraReferences: [depPath]);

        var plugin = _loader.LoadPluginsFromFolder(folder).ShouldHaveSingleItem();

        plugin.Manifest.Description.ShouldBe("dep");
    }

    [Fact(DisplayName = "LoadPluginsFromFolder skips a plugin whose dependency is missing and other plugins still load")]
    public void LoadPluginsFromFolder_MissingDependency_SkipsWithoutThrowing()
    {
        var brokenFolder = NewFolder();
        var depPath = TestPluginBuilder.BuildLibrary(brokenFolder, UniqueName("MissingDep"), """
            namespace Dep;

            public class BaseThing
            {
            }
            """);
        TestPluginBuilder.Build(brokenFolder, UniqueName("NeedsDep"), "mythetech.test.needs-dep", new Version(1, 0, 0),
            extraSource: """
                public class Holder : Dep.BaseThing
                {
                }
                """,
            extraReferences: [depPath]);
        File.Delete(depPath);
        var healthyFolder = NewFolder();
        TestPluginBuilder.Build(healthyFolder, UniqueName("Healthy"), "mythetech.test.healthy", new Version(1, 0, 0));

        var broken = Should.NotThrow(() => _loader.LoadPluginsFromFolder(brokenFolder));
        var healthy = _loader.LoadPluginsFromFolder(healthyFolder);

        broken.ShouldBeEmpty();
        healthy.ShouldHaveSingleItem().Manifest.Id.ShouldBe("mythetech.test.healthy");
    }

    private string NewFolder()
    {
        var folder = Path.Combine(_root, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        return folder;
    }

    private static string UniqueName(string prefix) => $"{prefix}_{Guid.NewGuid():N}";

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }
}
