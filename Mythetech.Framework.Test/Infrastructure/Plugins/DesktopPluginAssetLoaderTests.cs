using Shouldly;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Mythetech.Framework.Desktop;
using Mythetech.Framework.Infrastructure.Plugins;
using NSubstitute;

namespace Mythetech.Framework.Test.Infrastructure.Plugins;

public class DesktopPluginAssetLoaderTests : IDisposable
{
    private readonly string _pluginDirectory = Path.Combine(Path.GetTempPath(), $"plugin_assets_test_{Guid.NewGuid()}");
    private readonly IJSRuntime _jsRuntime = Substitute.For<IJSRuntime>();

    [Fact(DisplayName = "LoadPluginAssetsAsync injects a plugin's assets once across calls")]
    public async Task LoadPluginAssetsAsync_SecondCall_DoesNotInjectAgain()
    {
        var stylesheet = Path.Combine(_pluginDirectory, "wwwroot", "_content", "AssetTestPlugin", "style.css");
        Directory.CreateDirectory(Path.GetDirectoryName(stylesheet)!);
        File.WriteAllText(stylesheet, "body { color: red; }");
        var manifest = Substitute.For<IPluginManifest>();
        manifest.Assets.Returns([new PluginAsset { Path = "_content/AssetTestPlugin/style.css", Type = PluginAssetType.Css }]);
        var plugin = new PluginInfo
        {
            Manifest = manifest,
            Assembly = typeof(DesktopPluginAssetLoaderTests).Assembly,
            SourcePath = Path.Combine(_pluginDirectory, "AssetTestPlugin.dll")
        };
        var loader = new DesktopPluginAssetLoader(_jsRuntime, NullLogger<DesktopPluginAssetLoader>.Instance, new PluginState());

        await loader.LoadPluginAssetsAsync(plugin);
        await loader.LoadPluginAssetsAsync(plugin);

        await _jsRuntime.Received(1).InvokeAsync<IJSVoidResult>("eval", Arg.Any<object?[]?>());
    }

    private PluginInfo CreatePlugin(string id, string assetPath, string dllName)
    {
        var manifest = Substitute.For<IPluginManifest>();
        manifest.Id.Returns(id);
        manifest.Assets.Returns([new PluginAsset { Path = assetPath, Type = PluginAssetType.Css }]);
        return new PluginInfo
        {
            Manifest = manifest,
            Assembly = typeof(DesktopPluginAssetLoaderTests).Assembly,
            SourcePath = Path.Combine(_pluginDirectory, dllName)
        };
    }

    [Fact(DisplayName = "LoadPluginAssetsAsync re-injects a stylesheet for a new PluginInfo with the same ID")]
    public async Task LoadPluginAssetsAsync_NewPluginInfoForSameId_ReinjectsStylesheet()
    {
        var stylesheet = Path.Combine(_pluginDirectory, "wwwroot", "_content", "ReloadPlugin", "style.css");
        Directory.CreateDirectory(Path.GetDirectoryName(stylesheet)!);
        File.WriteAllText(stylesheet, "body { color: red; }");
        var loader = new DesktopPluginAssetLoader(_jsRuntime, NullLogger<DesktopPluginAssetLoader>.Instance);

        await loader.LoadPluginAssetsAsync(CreatePlugin("reload", "_content/ReloadPlugin/style.css", "ReloadPlugin.dll"));
        await loader.LoadPluginAssetsAsync(CreatePlugin("reload", "_content/ReloadPlugin/style.css", "ReloadPlugin.dll"));

        // inject, unload, inject
        await _jsRuntime.Received(3).InvokeAsync<IJSVoidResult>("eval", Arg.Any<object?[]?>());
        loader.IsLoaded("_content/ReloadPlugin/style.css").ShouldBeTrue();
    }

    [Fact(DisplayName = "Asset resolution uses a local build's static web assets manifest content roots")]
    public async Task ResolveAsset_LocalBuildManifest_UsesContentRoot()
    {
        var contentRoot = Path.Combine(_pluginDirectory, "project", "wwwroot") + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(contentRoot);
        File.WriteAllText(Path.Combine(contentRoot, "local.css"), "body { color: blue; }");
        var buildDir = Path.Combine(_pluginDirectory, "bin");
        Directory.CreateDirectory(buildDir);
        var manifestJson = """{"ContentRoots":["__ROOT__"],"Root":{"Children":{"local.css":{"Children":null,"Asset":{"ContentRootIndex":0,"SubPath":"local.css"},"Patterns":null}},"Asset":null,"Patterns":null}}""";
        File.WriteAllText(Path.Combine(buildDir, "LocalPlugin.staticwebassets.runtime.json"), manifestJson.Replace("__ROOT__", contentRoot.Replace("\\", "\\\\")));
        var plugin = CreatePlugin("local", "_content/LocalPlugin/local.css", Path.Combine("bin", "LocalPlugin.dll"));
        var loader = new DesktopPluginAssetLoader(_jsRuntime, NullLogger<DesktopPluginAssetLoader>.Instance);

        await loader.LoadPluginAssetsAsync(plugin);

        loader.IsLoaded("_content/LocalPlugin/local.css").ShouldBeTrue();
        await _jsRuntime.Received(1).InvokeAsync<IJSVoidResult>("eval", Arg.Any<object?[]?>());
    }

    [Fact(DisplayName = "LoadPluginAssetsAsync doesn't keep a loaded PluginInfo alive")]
    public async Task LoadPluginAssetsAsync_RemembersPluginWeakly()
    {
        var stylesheet = Path.Combine(_pluginDirectory, "wwwroot", "_content", "WeakPlugin", "style.css");
        Directory.CreateDirectory(Path.GetDirectoryName(stylesheet)!);
        File.WriteAllText(stylesheet, "body { color: red; }");
        var loader = new DesktopPluginAssetLoader(_jsRuntime, NullLogger<DesktopPluginAssetLoader>.Instance);

        var weak = await LoadAndForgetAsync(loader);
        await Task.Yield();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        weak.TryGetTarget(out _).ShouldBeFalse();
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private async Task<WeakReference<PluginInfo>> LoadAndForgetAsync(DesktopPluginAssetLoader loader)
    {
        var plugin = CreatePlugin("weak", "_content/WeakPlugin/style.css", "WeakPlugin.dll");
        await loader.LoadPluginAssetsAsync(plugin);
        return new WeakReference<PluginInfo>(plugin);
    }

    public void Dispose()
    {
        try { Directory.Delete(_pluginDirectory, recursive: true); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }
}
