namespace Mythetech.Framework.Infrastructure.FeatureFlags;

/// <summary>
/// Represents a feature flag.
/// </summary>
public class FeatureFlag
{
    /// <summary>
    /// Gets or sets the name of the feature.
    /// </summary>
    public required string Feature { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the feature is enabled.
    /// </summary>
    public required bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the attributes associated with the feature.
    /// </summary>
    public Dictionary<string, string> Attributes { get; set; } = [];
}
