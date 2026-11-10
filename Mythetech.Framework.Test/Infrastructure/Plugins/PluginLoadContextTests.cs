using System.Runtime.InteropServices;
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

    [Fact(DisplayName = "LoadPluginsFromFolder loads a plugin built against a newer Mythetech.Framework against the host's copy")]
    public void LoadPluginsFromFolder_PluginReferencesNewerFrameworkVersion_LoadsAgainstHostCopy()
    {
        var frameworkName = typeof(IPluginManifest).Assembly.GetName().Name!;
        var newerFrameworkPath = TestPluginBuilder.BuildLibrary(NewFolder(), frameworkName, """
            [assembly: System.Reflection.AssemblyVersion("99.0.0.0")]

            namespace Mythetech.Framework.Infrastructure.Plugins;

            public interface IPluginManifest
            {
                string Id { get; }
                string Name { get; }
                System.Version Version { get; }
                string Developer { get; }
                string Description { get; }
            }
            """);
        var folder = NewFolder();
        TestPluginBuilder.Build(folder, UniqueName("NewerFramework"), "mythetech.test.newer-framework", new Version(1, 0, 0),
            extraReferences: [newerFrameworkPath]);

        var plugin = _loader.LoadPluginsFromFolder(folder).ShouldHaveSingleItem();

        plugin.Assembly.GetReferencedAssemblies().Single(reference => reference.Name == frameworkName)
            .Version.ShouldBe(new Version(99, 0, 0, 0));
        plugin.Manifest.GetType().GetInterface(nameof(IPluginManifest)).ShouldNotBeNull()
            .Assembly.ShouldBeSameAs(typeof(IPluginManifest).Assembly);
    }

    [Fact(DisplayName = "LoadPluginsFromFolder records the Mythetech.Framework version the plugin references")]
    public void LoadPluginsFromFolder_RecordsReferencedFrameworkVersion()
    {
        var folder = NewFolder();
        TestPluginBuilder.Build(folder, UniqueName("RecordsVersion"), "mythetech.test.records-version", new Version(1, 0, 0));

        var plugin = _loader.LoadPluginsFromFolder(folder).ShouldHaveSingleItem();

        plugin.FrameworkVersion.ShouldBe(typeof(IPluginManifest).Assembly.GetName().Version);
    }

    [Theory(DisplayName = "IsBuiltAgainstNewerFramework compares the plugin's version with the host's")]
    [InlineData("0.23.0.0", "0.22.1.0", true)]
    [InlineData("0.22.1.0", "0.22.1.0", false)]
    [InlineData("0.22.0.0", "0.22.1.0", false)]
    [InlineData(null, "0.22.1.0", false)]
    public void IsBuiltAgainstNewerFramework_Cases(string? plugin, string host, bool expected)
    {
        PluginLoader.IsBuiltAgainstNewerFramework(plugin is null ? null : Version.Parse(plugin), Version.Parse(host))
            .ShouldBe(expected);
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

    [Fact(DisplayName = "LoadPluginsFromFolder resolves a dependency that the plugin's deps.json places under a runtime folder")]
    public void LoadPluginsFromFolder_DependencyUnderRuntimesFolder_ResolvesThroughDepsJson()
    {
        var folder = NewFolder();
        var rid = RuntimeInformation.RuntimeIdentifier;
        var depName = UniqueName("RidDep");
        var depRelativePath = $"runtimes/{rid}/lib/net11.0/{depName}.dll";
        var depPath = TestPluginBuilder.BuildLibrary(
            Path.GetDirectoryName(Path.Combine(folder, depRelativePath))!, depName, """
                public static class RidDep
                {
                    public static string Value => "rid";
                }
                """);
        var pluginName = UniqueName("UsesRidDep");
        TestPluginBuilder.Build(folder, pluginName, "mythetech.test.uses-rid-dep", new Version(1, 0, 0),
            extraSource: $$"""
                namespace {{TestPluginBuilder.ManifestNamespace}};

                public partial class Manifest
                {
                    static partial void OverrideDescription(ref string description) => description = RidDep.Value;
                }
                """,
            extraReferences: [depPath]);
        File.WriteAllText(Path.Combine(folder, $"{pluginName}.deps.json"), $$"""
            {
              "runtimeTarget": { "name": ".NETCoreApp,Version=v11.0" },
              "targets": {
                ".NETCoreApp,Version=v11.0": {
                  "{{pluginName}}/1.0.0": {
                    "dependencies": { "{{depName}}": "1.0.0" },
                    "runtime": { "{{pluginName}}.dll": {} }
                  },
                  "{{depName}}/1.0.0": {
                    "runtimeTargets": {
                      "{{depRelativePath}}": { "rid": "{{rid}}", "assetType": "runtime" }
                    }
                  }
                }
              },
              "libraries": {
                "{{pluginName}}/1.0.0": { "type": "project", "serviceable": false, "sha512": "" },
                "{{depName}}/1.0.0": { "type": "package", "serviceable": false, "sha512": "", "path": "{{depName.ToLowerInvariant()}}/1.0.0" }
              }
            }
            """);

        var plugin = _loader.LoadPluginsFromFolder(folder).ShouldHaveSingleItem();

        plugin.Manifest.Description.ShouldBe("rid");
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
