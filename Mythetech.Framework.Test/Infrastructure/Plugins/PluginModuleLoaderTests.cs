using Bunit;
using Microsoft.Extensions.Logging.Abstractions;
using Mythetech.Framework.Desktop;
using Mythetech.Framework.Infrastructure.Plugins;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Plugins;

public class PluginModuleLoaderTests : BunitContext
{
    private const string InteropModulePath = "./_content/Mythetech.Framework/js/plugin-modules.js";
    private readonly string _pluginDirectory = Path.Combine(Path.GetTempPath(), $"plugin_modules_test_{Guid.NewGuid()}");

    private PluginInfo CreatePlugin(string dllName = "ModulePlugin.dll")
    {
        var manifest = Substitute.For<IPluginManifest>();
        manifest.Id.Returns("module-plugin");
        return new PluginInfo
        {
            Manifest = manifest,
            Assembly = typeof(PluginModuleLoaderTests).Assembly,
            SourcePath = Path.Combine(_pluginDirectory, dllName)
        };
    }

    private DesktopPluginModuleLoader CreateDesktopLoader() =>
        new(new PluginModuleInterop(JSInterop.JSRuntime), NullLogger<DesktopPluginModuleLoader>.Instance);

    private void WriteModule(string fileName, string text)
    {
        var path = Path.Combine(_pluginDirectory, "wwwroot", fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }

    [Fact(DisplayName = "DesktopImportAsync reads the module file and imports it from source")]
    public async Task DesktopImportAsync_ReadsModuleFileAndImportsFromSource()
    {
        WriteModule("counter.js", "export const answer = 42;");
        var interop = JSInterop.SetupModule(InteropModulePath);
        interop.SetupModule("importFromSource", args => (string?)args.Arguments[0] == "export const answer = 42;");

        var module = await CreateDesktopLoader().ImportAsync(CreatePlugin(), "counter.js");

        module.ShouldNotBeNull();
        interop.Invocations["importFromSource"].Select(i => i.Arguments[0]).ShouldBe(["export const answer = 42;"]);
    }

    [Fact(DisplayName = "DesktopImportAsync after a reload imports the new file text")]
    public async Task DesktopImportAsync_AfterReload_ImportsNewText()
    {
        WriteModule("counter.js", "export const v = 1;");
        var interop = JSInterop.SetupModule(InteropModulePath);
        interop.SetupModule("importFromSource", args => (string?)args.Arguments[0] == "export const v = 1;");
        interop.SetupModule("importFromSource", args => (string?)args.Arguments[0] == "export const v = 2;");
        var loader = CreateDesktopLoader();

        await loader.ImportAsync(CreatePlugin(), "counter.js");
        WriteModule("counter.js", "export const v = 2;");
        await loader.ImportAsync(CreatePlugin(), "counter.js");

        interop.Invocations["importFromSource"].Select(i => i.Arguments[0])
            .ShouldBe(["export const v = 1;", "export const v = 2;"]);
    }

    [Theory(DisplayName = "ImportAsync rejects external URLs")]
    [InlineData("http://example.com/a.js")]
    [InlineData("https://example.com/a.js")]
    [InlineData("//example.com/a.js")]
    public async Task ImportAsync_ExternalUrl_Throws(string url)
    {
        await Should.ThrowAsync<ArgumentException>(() => CreateDesktopLoader().ImportAsync(CreatePlugin(), url));
        await Should.ThrowAsync<ArgumentException>(() => new WebPluginModuleLoader(JSInterop.JSRuntime).ImportAsync(CreatePlugin(), url));
    }

    [Fact(DisplayName = "DesktopImportAsync throws FileNotFoundException when the path resolves to no file")]
    public async Task DesktopImportAsync_MissingFile_Throws()
    {
        await Should.ThrowAsync<FileNotFoundException>(() => CreateDesktopLoader().ImportAsync(CreatePlugin(), "missing.js"));
    }

    [Fact(DisplayName = "WebImportAsync imports the plugin's content URL")]
    public async Task WebImportAsync_ImportsContentUrl()
    {
        var assemblyName = typeof(PluginModuleLoaderTests).Assembly.GetName().Name;
        var module = JSInterop.SetupModule($"./_content/{assemblyName}/js/counter.js");
        var loader = new WebPluginModuleLoader(JSInterop.JSRuntime);

        var imported = await loader.ImportAsync(CreatePlugin(), "/js/counter.js");

        imported.ShouldNotBeNull();
        JSInterop.Invocations.ShouldContain(i => i.Identifier == "import" && (string?)i.Arguments[0] == $"./_content/{assemblyName}/js/counter.js");
    }

    [Fact(DisplayName = "WebImportAsync keeps a path that already starts with _content")]
    public async Task WebImportAsync_ContentPath_UsedAsIs()
    {
        JSInterop.SetupModule("./_content/Other.Package/js/x.js");

        await new WebPluginModuleLoader(JSInterop.JSRuntime).ImportAsync(CreatePlugin(), "/_content/Other.Package/js/x.js");

        JSInterop.Invocations.ShouldContain(i => i.Identifier == "import" && (string?)i.Arguments[0] == "./_content/Other.Package/js/x.js");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && Directory.Exists(_pluginDirectory))
            Directory.Delete(_pluginDirectory, true);
        base.Dispose(disposing);
    }
}
