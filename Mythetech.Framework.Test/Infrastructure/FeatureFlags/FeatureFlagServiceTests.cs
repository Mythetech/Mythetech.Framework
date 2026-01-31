using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.FeatureFlags;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Settings;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.FeatureFlags;

public class FeatureFlagServiceTests
{
    private readonly IFeatureFlagRegistry _registry;
    private readonly ISettingsProvider _settingsProvider;
    private readonly ILogger<FeatureFlagService> _logger;
    private readonly FeatureFlagService _service;
    private readonly TestFeatureFlags _testFlags;

    public FeatureFlagServiceTests()
    {
        _logger = Substitute.For<ILogger<FeatureFlagService>>();
        _settingsProvider = Substitute.For<ISettingsProvider>();

        // Create real registry with test flags
        var registryLogger = Substitute.For<ILogger<FeatureFlagRegistry>>();
        _registry = new FeatureFlagRegistry(registryLogger);

        // Register test feature flags
        _testFlags = new TestFeatureFlags();
        _registry.RegisterFlagSettings(_testFlags);

        _service = new FeatureFlagService(_registry, _settingsProvider, _logger);
    }

    [Fact(DisplayName = "IsFeatureFlagEnabled returns true when flag is enabled")]
    public async Task IsFeatureFlagEnabled_ReturnsTrue_WhenFlagIsEnabled()
    {
        // Arrange - Beta is enabled by default in TestFeatureFlags

        // Act
        var result = await _service.IsFeatureFlagEnabled("Beta");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact(DisplayName = "IsFeatureFlagEnabled returns false when flag is disabled")]
    public async Task IsFeatureFlagEnabled_ReturnsFalse_WhenFlagIsDisabled()
    {
        // Arrange - TestFeature is disabled by default

        // Act
        var result = await _service.IsFeatureFlagEnabled("TestFeature");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact(DisplayName = "IsFeatureFlagEnabled returns false when feature does not exist")]
    public async Task IsFeatureFlagEnabled_ReturnsFalse_WhenFeatureDoesNotExist()
    {
        // Arrange
        string feature = "NonExistentFeature";

        // Act
        var result = await _service.IsFeatureFlagEnabled(feature);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact(DisplayName = "DisableFeatureFlag disables an enabled feature")]
    public async Task DisableFeatureFlag_DisablesFeature_WhenFeatureExists()
    {
        // Arrange - Beta is enabled by default
        var initialValue = await _service.IsFeatureFlagEnabled("Beta");
        initialValue.ShouldBeTrue();

        // Act
        await _service.DisableFeatureFlag("Beta");

        // Assert
        var result = await _service.IsFeatureFlagEnabled("Beta");
        result.ShouldBeFalse();

        // Verify settings change was notified
        await _settingsProvider.Received(1).NotifySettingsChangedAsync(_testFlags);
    }

    [Fact(DisplayName = "EnableFeatureFlag enables a disabled feature")]
    public async Task EnableFeatureFlag_EnablesFeature_WhenFeatureExists()
    {
        // Arrange - TestFeature is disabled by default
        var initialValue = await _service.IsFeatureFlagEnabled("TestFeature");
        initialValue.ShouldBeFalse();

        // Act
        await _service.EnableFeatureFlag("TestFeature");

        // Assert
        var result = await _service.IsFeatureFlagEnabled("TestFeature");
        result.ShouldBeTrue();

        // Verify settings change was notified
        await _settingsProvider.Received(1).NotifySettingsChangedAsync(_testFlags);
    }

    [Fact(DisplayName = "EnableFeatureFlag logs warning when feature does not exist")]
    public async Task EnableFeatureFlag_LogsWarning_WhenFeatureDoesNotExist()
    {
        // Arrange
        string feature = "NonExistentFeature";

        // Act
        await _service.EnableFeatureFlag(feature);

        // Assert - verify warning was logged
        _logger.ReceivedWithAnyArgs(1).LogWarning("");
    }

    [Fact(DisplayName = "DisableFeatureFlag logs warning when feature does not exist")]
    public async Task DisableFeatureFlag_LogsWarning_WhenFeatureDoesNotExist()
    {
        // Arrange
        string feature = "NonExistentFeature";

        // Act
        await _service.DisableFeatureFlag(feature);

        // Assert - verify warning was logged
        _logger.ReceivedWithAnyArgs(1).LogWarning("");
    }

    [Fact(DisplayName = "GetFeatureFlagsAsync returns only enabled features by default")]
    public async Task GetFeatureFlagsAsync_ReturnsOnlyEnabledFeatures_WhenEnabledOnlyIsTrue()
    {
        // Arrange - Beta is enabled, TestFeature and Experimental are disabled

        // Act
        var result = await _service.GetFeatureFlagsAsync(enabledOnly: true);

        // Assert
        result.Count.ShouldBe(1);
        result[0].Feature.ShouldBe("Beta");
        result[0].Enabled.ShouldBeTrue();
    }

    [Fact(DisplayName = "GetFeatureFlagsAsync returns all features when enabledOnly is false")]
    public async Task GetFeatureFlagsAsync_ReturnsAllFeatures_WhenEnabledOnlyIsFalse()
    {
        // Arrange

        // Act
        var result = await _service.GetFeatureFlagsAsync(enabledOnly: false);

        // Assert
        result.Count.ShouldBe(3);
        result.ShouldContain(f => f.Feature == "Beta" && f.Enabled);
        result.ShouldContain(f => f.Feature == "TestFeature" && !f.Enabled);
        result.ShouldContain(f => f.Feature == "Experimental" && !f.Enabled);
    }

    [Fact(DisplayName = "GetFeatureFlagsAsync reflects state changes after enable/disable")]
    public async Task GetFeatureFlagsAsync_ReflectsStateChanges_AfterEnableDisable()
    {
        // Arrange - initially Beta=true, TestFeature=false

        // Act - disable Beta, enable TestFeature
        await _service.DisableFeatureFlag("Beta");
        await _service.EnableFeatureFlag("TestFeature");

        var result = await _service.GetFeatureFlagsAsync(enabledOnly: true);

        // Assert - now only TestFeature should be enabled
        result.Count.ShouldBe(1);
        result[0].Feature.ShouldBe("TestFeature");
    }
}
