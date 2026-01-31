using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.FeatureFlags;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.FeatureFlags;

public class FeatureFlagRegistryTests
{
    private readonly FeatureFlagRegistry _registry;
    private readonly ILogger<FeatureFlagRegistry> _logger;

    public FeatureFlagRegistryTests()
    {
        _logger = Substitute.For<ILogger<FeatureFlagRegistry>>();
        _registry = new FeatureFlagRegistry(_logger);
    }

    [Fact(DisplayName = "RegisterFlagSettings registers all flags from settings")]
    public void RegisterFlagSettings_RegistersAllFlags_FromSettings()
    {
        // Arrange
        var testFlags = new TestFeatureFlags();

        // Act
        _registry.RegisterFlagSettings(testFlags);

        // Assert
        var allFlags = _registry.GetAllFlags();
        allFlags.Count.ShouldBe(3);
        allFlags.ShouldContain(f => f.Key == "Beta");
        allFlags.ShouldContain(f => f.Key == "TestFeature");
        allFlags.ShouldContain(f => f.Key == "Experimental");
    }

    [Fact(DisplayName = "GetFlag returns correct flag info")]
    public void GetFlag_ReturnsCorrectFlagInfo()
    {
        // Arrange
        var testFlags = new TestFeatureFlags();
        _registry.RegisterFlagSettings(testFlags);

        // Act
        var flag = _registry.GetFlag("Beta");

        // Assert
        flag.ShouldNotBeNull();
        flag.Key.ShouldBe("Beta");
        flag.Label.ShouldBe("Beta");
        flag.Description.ShouldBe("Enable beta features");
        flag.SourceSettings.ShouldBe(testFlags);
    }

    [Fact(DisplayName = "GetFlag returns null for unknown flag")]
    public void GetFlag_ReturnsNull_ForUnknownFlag()
    {
        // Arrange
        var testFlags = new TestFeatureFlags();
        _registry.RegisterFlagSettings(testFlags);

        // Act
        var flag = _registry.GetFlag("NonExistentFlag");

        // Assert
        flag.ShouldBeNull();
    }

    [Fact(DisplayName = "CaptureState returns current flag values")]
    public void CaptureState_ReturnsCurrentFlagValues()
    {
        // Arrange
        var testFlags = new TestFeatureFlags();
        _registry.RegisterFlagSettings(testFlags);

        // Act
        var state = _registry.CaptureState();

        // Assert
        state.Count.ShouldBe(3);
        state["Beta"].ShouldBeTrue();
        state["TestFeature"].ShouldBeFalse();
        state["Experimental"].ShouldBeFalse();
    }

    [Fact(DisplayName = "CaptureState reflects changes to flag values")]
    public void CaptureState_ReflectsChanges_ToFlagValues()
    {
        // Arrange
        var testFlags = new TestFeatureFlags();
        _registry.RegisterFlagSettings(testFlags);

        // Act - modify flags through the settings
        testFlags.Beta = false;
        testFlags.TestFeature = true;
        var state = _registry.CaptureState();

        // Assert
        state["Beta"].ShouldBeFalse();
        state["TestFeature"].ShouldBeTrue();
    }

    [Fact(DisplayName = "GetAllFlagSettings returns all registered settings")]
    public void GetAllFlagSettings_ReturnsAllRegisteredSettings()
    {
        // Arrange
        var testFlags = new TestFeatureFlags();
        _registry.RegisterFlagSettings(testFlags);

        // Act
        var settings = _registry.GetAllFlagSettings();

        // Assert
        settings.Count.ShouldBe(1);
        settings[0].ShouldBe(testFlags);
    }

    [Fact(DisplayName = "RegisterFlagSettings skips duplicate keys and logs warnings")]
    public void RegisterFlagSettings_SkipsDuplicateKeys_AndLogsWarnings()
    {
        // Arrange
        var testFlags1 = new TestFeatureFlags();
        var testFlags2 = new TestFeatureFlags();

        // Act
        _registry.RegisterFlagSettings(testFlags1);
        _registry.RegisterFlagSettings(testFlags2);

        // Assert - should still only have 3 flags (duplicates skipped)
        var allFlags = _registry.GetAllFlags();
        allFlags.Count.ShouldBe(3);

        // Verify both settings instances were registered (even though flags are shared)
        var allSettings = _registry.GetAllFlagSettings();
        allSettings.Count.ShouldBe(2);

        // Verify warning logs were received for duplicates (check log level)
        _logger.Received(3).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}
