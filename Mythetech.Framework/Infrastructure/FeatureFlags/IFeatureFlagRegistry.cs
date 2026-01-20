namespace Mythetech.Framework.Infrastructure.FeatureFlags;

/// <summary>
/// Central registry that tracks all feature flags across all settings instances.
/// Provides the bridge between string-based flag lookups and property-based storage.
/// </summary>
public interface IFeatureFlagRegistry
{
    /// <summary>
    /// Registers a feature flags settings instance.
    /// Called automatically during startup for discovered settings.
    /// </summary>
    void RegisterFlagSettings(FeatureFlagsSettingsBase settings);

    /// <summary>
    /// Gets all registered flag infos.
    /// </summary>
    IReadOnlyList<FeatureFlagInfo> GetAllFlags();

    /// <summary>
    /// Gets a specific flag by key.
    /// </summary>
    FeatureFlagInfo? GetFlag(string key);

    /// <summary>
    /// Gets all registered settings instances.
    /// </summary>
    IReadOnlyList<FeatureFlagsSettingsBase> GetAllFlagSettings();

    /// <summary>
    /// Captures current state of all flags for change tracking.
    /// </summary>
    Dictionary<string, bool> CaptureState();
}
