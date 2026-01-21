namespace Mythetech.Framework.Infrastructure.FeatureFlags.Events;

/// <summary>
/// Published when a feature flag is disabled.
/// </summary>
/// <param name="FlagKey">The unique key of the flag that was disabled.</param>
/// <param name="SourceSettingsId">The SettingsId of the settings class containing this flag.</param>
public record FeatureFlagDisabled(string FlagKey, string SourceSettingsId);
