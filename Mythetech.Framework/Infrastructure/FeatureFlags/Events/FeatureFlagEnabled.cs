namespace Mythetech.Framework.Infrastructure.FeatureFlags.Events;

/// <summary>
/// Published when a feature flag is enabled.
/// </summary>
/// <param name="FlagKey">The unique key of the flag that was enabled.</param>
/// <param name="SourceSettingsId">The SettingsId of the settings class containing this flag.</param>
public record FeatureFlagEnabled(string FlagKey, string SourceSettingsId);
