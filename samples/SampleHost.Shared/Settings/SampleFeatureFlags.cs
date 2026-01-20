using Mythetech.Framework.Infrastructure.FeatureFlags;

namespace SampleHost.Shared.Settings;

/// <summary>
/// Sample feature flags to demonstrate the feature flag framework.
/// </summary>
public class SampleFeatureFlags : FeatureFlagsSettingsBase
{
    /// <inheritdoc />
    public override string SettingsId => "SampleFeatureFlags";

    /// <inheritdoc />
    public override string DisplayName => "Feature Flags";

    /// <summary>
    /// Enable dark mode theme across the application.
    /// </summary>
    [FeatureFlag(Label = "Dark Mode", Description = "Enable dark mode theme", Group = "UI")]
    public bool DarkMode { get; set; } = false;

    /// <summary>
    /// Show the new experimental dashboard.
    /// </summary>
    [FeatureFlag(Label = "New Dashboard", Description = "Show the redesigned dashboard (experimental)", Group = "Experimental")]
    public bool NewDashboard { get; set; } = false;

    /// <summary>
    /// Enable advanced plugin management features.
    /// </summary>
    [FeatureFlag(Label = "Advanced Plugin Management", Description = "Enable advanced plugin configuration options", Group = "Experimental")]
    public bool AdvancedPluginManagement { get; set; } = false;

    /// <summary>
    /// Show developer tools in the UI.
    /// </summary>
    [FeatureFlag(Label = "Developer Tools", Description = "Show developer debugging tools", Group = "Developer")]
    public bool DeveloperTools { get; set; } = false;
}
