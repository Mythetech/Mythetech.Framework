using MudBlazor;
using Mythetech.Framework.Desktop.Updates.Components;
using Mythetech.Framework.Infrastructure.Settings;

namespace Mythetech.Framework.Desktop.Updates;

/// <summary>
/// User preferences for application updates.
/// </summary>
public class UpdateSettings : SettingsBase
{
    /// <inheritdoc />
    public override string SettingsId => "Updates";

    /// <inheritdoc />
    public override string DisplayName => "Updates";

    /// <inheritdoc />
    public override string Icon => Icons.Material.Filled.SystemUpdate;

    /// <inheritdoc />
    public override int Order => 100;

    /// <inheritdoc />
    public override Type? EndingContent => typeof(UpdateSettingsEndingContent);

    /// <summary>
    /// Whether to automatically check for updates when the application starts.
    /// </summary>
    [Setting(
        Label = "Check for Updates on Startup",
        Description = "Automatically check for new versions when the application launches",
        Group = "Automatic Updates",
        Order = 10)]
    public bool AutoCheckOnStartup { get; set; } = true;

    /// <summary>
    /// Whether to automatically download updates when found.
    /// </summary>
    [Setting(
        Label = "Auto-Download Updates",
        Description = "Automatically download updates in the background when available",
        Group = "Automatic Updates",
        Order = 20)]
    public bool AutoDownload { get; set; }

    /// <summary>
    /// How often to check for updates (in hours). 0 = only on startup.
    /// </summary>
    [Setting(
        Label = "Check Interval (hours)",
        Description = "How often to check for updates. Set to 0 for startup only.",
        Group = "Automatic Updates",
        Order = 30,
        Min = 0,
        Max = 168,
        Step = 1)]
    public int CheckIntervalHours { get; set; } = 24;
}
