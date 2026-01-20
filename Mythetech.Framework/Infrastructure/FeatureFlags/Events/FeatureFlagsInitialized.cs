namespace Mythetech.Framework.Infrastructure.FeatureFlags.Events;

/// <summary>
/// Published after all feature flags have been loaded from persistence.
/// Consumers can use this to know when flags are ready to use.
/// </summary>
/// <param name="FlagKeys">All registered flag keys.</param>
public record FeatureFlagsInitialized(IReadOnlyList<string> FlagKeys);
