namespace Mythetech.Framework.Infrastructure.FeatureFlags;

/// <summary>
/// Defines a service for managing feature flags.
/// </summary>
public interface IFeatureFlagService
{
    /// <summary>
    /// Gets a list of feature flags.
    /// </summary>
    /// <param name="enabledOnly">If true, only enabled feature flags are returned; otherwise, all feature flags are returned.</param>
    /// <returns>A list of feature flags.</returns>
    Task<List<FeatureFlag>> GetFeatureFlagsAsync(bool enabledOnly = true);

    /// <summary>
    /// Checks if a feature flag is enabled.
    /// </summary>
    /// <param name="feature">The name of the feature.</param>
    /// <returns>True if the feature flag is enabled; otherwise, false.</returns>
    Task<bool> IsFeatureFlagEnabled(string feature);

    /// <summary>
    /// Enables a feature flag.
    /// </summary>
    /// <param name="feature">The name of the feature.</param>
    Task EnableFeatureFlag(string feature);

    /// <summary>
    /// Disables a feature flag.
    /// </summary>
    /// <param name="feature">The name of the feature.</param>
    Task DisableFeatureFlag(string feature);
}
