using MudBlazor;
using Mythetech.Framework.Infrastructure.Settings;

namespace SampleHost.Shared.Settings;

/// <summary>
/// Sample application settings to demonstrate the settings framework.
/// Controls various UI behaviors in the sample host apps.
/// </summary>
public class SampleAppSettings : SettingsBase
{
    /// <inheritdoc />
    public override string SettingsId => "SampleApp";

    /// <inheritdoc />
    public override string DisplayName => "Application";

    /// <inheritdoc />
    public override string Icon => Icons.Material.Filled.Apps;

    /// <inheritdoc />
    public override int Order => 10;

    /// <summary>
    /// Whether to show the context panel sidebar on the right.
    /// </summary>
    [Setting(Label = "Show Context Panel", Description = "Display the plugin context panel sidebar", Group = "Layout")]
    public bool ShowContextPanel { get; set; } = true;

    /// <summary>
    /// Width of the context panel in pixels.
    /// </summary>
    [Setting(Label = "Context Panel Width", Description = "Width of the context panel in pixels", Group = "Layout", Min = 200, Max = 600, Step = 20)]
    public int ContextPanelWidth { get; set; } = 320;

    /// <summary>
    /// Whether the navigation drawer starts expanded.
    /// </summary>
    [Setting(Label = "Expand Drawer on Start", Description = "Start with the navigation drawer expanded", Group = "Layout")]
    public bool DrawerExpandedOnStart { get; set; } = true;

    /// <summary>
    /// Application title shown in the app bar.
    /// </summary>
    [Setting(Label = "App Title", Description = "Title displayed in the application bar", Group = "Branding")]
    public string AppTitle { get; set; } = "Plugin Host Sample";
}
