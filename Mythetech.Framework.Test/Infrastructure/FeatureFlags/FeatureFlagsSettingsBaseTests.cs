using Mythetech.Framework.Infrastructure.FeatureFlags;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.FeatureFlags;

public class FeatureFlagsSettingsBaseTests
{
    [Fact(DisplayName = "Two instances of the same flags type report their own values")]
    public void GetFlagValue_SameTypeDifferentInstances_ReportsEachInstancesValue()
    {
        var first = new TestFeatureFlags { Beta = true };
        var second = new TestFeatureFlags { Beta = false };

        first.GetFlagValue("Beta").ShouldBeTrue();
        second.GetFlagValue("Beta").ShouldBeFalse();
    }

    [Fact(DisplayName = "SetFlagValue sets the flag with that key")]
    public void SetFlagValue_SetsTheFlag()
    {
        var flags = new TestFeatureFlags { Experimental = false };

        flags.SetFlagValue("Experimental", true);

        flags.Experimental.ShouldBeTrue();
    }

    [Fact(DisplayName = "GetFlagValue returns false for a key that is not a flag")]
    public void GetFlagValue_UnknownKey_ReturnsFalse()
    {
        new TestFeatureFlags().GetFlagValue("Missing").ShouldBeFalse();
    }

    [Fact(DisplayName = "A flag without a label is labelled with its property name split into words")]
    public void GetFeatureFlags_WithoutLabel_SplitsPropertyName()
    {
        var flag = new UnlabeledFeatureFlags().GetFeatureFlags().ShouldHaveSingleItem();

        flag.Key.ShouldBe("DarkModeToggle");
        flag.Label.ShouldBe("Dark Mode Toggle");
    }

    [Fact(DisplayName = "Each flag reports the settings instance it came from")]
    public void GetFeatureFlags_ReportsSourceInstance()
    {
        var flags = new TestFeatureFlags();

        flags.GetFeatureFlags().ShouldAllBe(f => f.SourceSettings == flags);
    }

    private sealed class UnlabeledFeatureFlags : FeatureFlagsSettingsBase
    {
        public override string SettingsId => "UnlabeledFeatureFlags";
        public override string DisplayName => "Unlabeled Feature Flags";

        [FeatureFlag]
        public bool DarkModeToggle { get; set; }
    }
}
