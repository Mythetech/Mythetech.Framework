using MudBlazor;
using Mythetech.Framework.Infrastructure.Settings;

namespace Mythetech.Framework.Infrastructure.Privacy;

/// <summary>
/// Privacy settings for managing user consent to anonymous data collection.
/// Appears as its own section in the settings panel.
/// </summary>
public class PrivacySettings : SettingsBase
{
    /// <inheritdoc />
    public override string SettingsId => "privacy";

    /// <inheritdoc />
    public override string DisplayName => "Privacy";

    /// <inheritdoc />
    public override string Icon => Icons.Material.Filled.Shield;

    /// <inheritdoc />
    public override int Order => int.MaxValue;

    /// <summary>
    /// Whether the user has opted in to sending anonymous crash reports.
    /// </summary>
    [Setting(Label = "Crash Reporting",
             Description = "Send anonymous crash reports to help improve stability",
             Group = "Diagnostics")]
    public bool CrashReportingEnabled { get; set; } = false;

    /// <summary>
    /// Whether the user has opted in to sending anonymous error reports.
    /// </summary>
    [Setting(Label = "Error Reporting",
             Description = "Send anonymous error and critical log entries to help identify issues",
             Group = "Diagnostics")]
    public bool ErrorReportingEnabled { get; set; } = false;

    /// <summary>
    /// Whether the user has been shown the privacy consent dialog.
    /// Not rendered in the settings panel.
    /// </summary>
    public bool HasSeenPrivacyDialog { get; set; } = false;
}
