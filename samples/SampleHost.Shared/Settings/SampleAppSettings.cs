using MudBlazor;
using Mythetech.Framework;
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
    public override string Icon => MythetechFrameworkIcons.Apps;

    /// <inheritdoc />
    public override int Order => 10;

    /// <summary>
    /// Whether plugin contributed panels appear in the context drawer.
    /// </summary>
    [Setting(Label = "Show Plugin Panels", Description = "Display plugin context panels in the drawer", Group = "Layout")]
    public bool ShowContextPanel { get; set; } = true;

    /// <summary>
    /// Largest width the context panel can be dragged to, in pixels.
    /// </summary>
    [Setting(Label = "Max Panel Width", Description = "Largest width the context panel can be resized to", Group = "Layout", Min = 200, Max = 600, Step = 20)]
    public int ContextPanelWidth { get; set; } = 600;

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
