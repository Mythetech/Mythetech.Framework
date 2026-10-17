using Mythetech.Framework.Infrastructure.Initialization;
using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Framework.Infrastructure.Settings;

namespace SampleHost.Desktop;

/// <summary>
/// Loads plugins once settings have loaded, so a custom plugin directory takes effect, and before
/// ApplicationReady, so the smoke run's plugin check waits for them.
/// </summary>
public sealed class SamplePluginsHook(PluginState pluginState, ISettingsProvider settingsProvider) : IAsyncInitializationHook
{
    public int Order => 300;

    public string Name => "Plugins";

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var customDirectory = settingsProvider.GetSettings<PluginSettings>()?.CustomPluginDirectory;
        var pluginDirectory = !string.IsNullOrWhiteSpace(customDirectory) && Directory.Exists(customDirectory)
            ? customDirectory
            : null;

        await pluginState.InitializePluginsAsync(pluginDirectory);
        await pluginState.LoadStateAsync();
    }
}
