using MudBlazor;
using Mythetech.Framework.Infrastructure.Settings;

namespace Mythetech.Framework.Infrastructure.Plugins;

/// <summary>
/// Framework-level settings for the plugin system.
/// Controls global plugin behavior and persistence options.
/// </summary>
public class PluginSettings : SettingsBase
{
    /// <inheritdoc />
    public override string SettingsId => "Plugins";

    /// <inheritdoc />
    public override string DisplayName => "Plugins";

    /// <inheritdoc />
    public override string Icon => Icons.Material.Filled.Extension;

    /// <inheritdoc />
    public override int Order => 20;

    /// <summary>
    /// Global toggle for the plugin system.
    /// When disabled, no plugins will be active regardless of individual enabled states.
    /// </summary>
    [Setting(
        Label = "Plugins Active",
        Description = "Global toggle to enable or disable all plugins",
        Group = "General",
        Order = 10)]
    public bool PluginsActive { get; set; } = true;

    /// <summary>
    /// Whether to automatically enable newly loaded plugins.
    /// </summary>
    [Setting(
        Label = "Auto-Enable Plugins",
        Description = "Automatically enable plugins when they are loaded",
        Group = "Behavior",
        Order = 20)]
    public bool AutoEnablePlugins { get; set; } = true;
}
