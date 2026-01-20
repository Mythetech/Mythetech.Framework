using System.Reflection;

namespace Mythetech.Framework.Infrastructure.FeatureFlags;

/// <summary>
/// Metadata about a single feature flag, including its source property and settings instance.
/// </summary>
public class FeatureFlagInfo
{
    /// <summary>
    /// The unique key used for IFeatureFlagService lookups.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// Display label shown in the settings UI.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// Optional description/help text.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Group name for organizing flags in the UI.
    /// </summary>
    public required string Group { get; init; }

    /// <summary>
    /// Sort order within the group.
    /// </summary>
    public int Order { get; init; }

    /// <summary>
    /// The PropertyInfo for the flag property.
    /// </summary>
    public required PropertyInfo Property { get; init; }

    /// <summary>
    /// The settings instance containing this flag.
    /// </summary>
    public required FeatureFlagsSettingsBase SourceSettings { get; init; }
}
