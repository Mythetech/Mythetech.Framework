namespace Mythetech.Framework.Infrastructure.FeatureFlags;

/// <summary>
/// Marks a boolean property as a feature flag.
/// The property will appear in the Feature Flags settings section
/// and can be toggled at runtime.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class FeatureFlagAttribute : Attribute
{
    /// <summary>
    /// Display label shown in the settings UI.
    /// If not set, defaults to the property name with spaces inserted between words.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Description/help text shown below the toggle in settings UI.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Group name for organizing flags in the UI.
    /// Defaults to "General" if not specified.
    /// </summary>
    public string Group { get; set; } = "General";

    /// <summary>
    /// Sort order within the group (lower values appear first).
    /// </summary>
    public int Order { get; set; } = 100;

    /// <summary>
    /// The string key used for IFeatureFlagService lookups.
    /// If not specified, defaults to the property name.
    /// </summary>
    public string? Key { get; set; }
}
