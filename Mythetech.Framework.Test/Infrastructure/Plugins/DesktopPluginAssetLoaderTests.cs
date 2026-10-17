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
        var loader = new DesktopPluginAssetLoader(_jsRuntime, new PluginState(), NullLogger<DesktopPluginAssetLoader>.Instance);

        await loader.LoadPluginAssetsAsync(plugin);
        await loader.LoadPluginAssetsAsync(plugin);

        await _jsRuntime.Received(1).InvokeAsync<IJSVoidResult>("eval", Arg.Any<object?[]?>());
    }

    public void Dispose()
    {
        try { Directory.Delete(_pluginDirectory, recursive: true); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }
}
