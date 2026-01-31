using Mythetech.Framework.Infrastructure.FeatureFlags;

namespace Mythetech.Framework.Test.Infrastructure.FeatureFlags;

/// <summary>
/// Test feature flags settings for use in unit tests.
/// </summary>
public class TestFeatureFlags : FeatureFlagsSettingsBase
{
    public override string SettingsId => "TestFeatureFlags";
    public override string DisplayName => "Test Feature Flags";

    [FeatureFlag(Label = "Beta", Description = "Enable beta features")]
    public bool Beta { get; set; } = true;

    [FeatureFlag(Label = "Test Feature", Description = "A test feature")]
    public bool TestFeature { get; set; } = false;

    [FeatureFlag(Label = "Experimental", Description = "Enable experimental features", Group = "Experimental")]
    public bool Experimental { get; set; } = false;
}
