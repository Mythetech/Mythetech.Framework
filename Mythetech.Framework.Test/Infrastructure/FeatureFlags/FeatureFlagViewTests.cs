using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Components.FeatureFlags;
using Mythetech.Framework.Infrastructure.FeatureFlags;
using Mythetech.Framework.Infrastructure.Settings;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.FeatureFlags;

public class FeatureFlagViewTests : TestContext
{
    private readonly TestFeatureFlags _testFlags;
    private readonly IFeatureFlagRegistry _registry;
    private readonly FeatureFlagService _flagService;
    private readonly DefaultFeatureFlagStateProvider _stateProvider;

    public FeatureFlagViewTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        // Set up the feature flag infrastructure
        var registryLogger = Substitute.For<ILogger<FeatureFlagRegistry>>();
        _registry = new FeatureFlagRegistry(registryLogger);

        _testFlags = new TestFeatureFlags();
        _registry.RegisterFlagSettings(_testFlags);

        var settingsProvider = Substitute.For<ISettingsProvider>();
        var serviceLogger = Substitute.For<ILogger<FeatureFlagService>>();
        _flagService = new FeatureFlagService(_registry, settingsProvider, serviceLogger);

        _stateProvider = new DefaultFeatureFlagStateProvider(_flagService);

        // Register services
        Services.AddSingleton<IFeatureFlagService>(_flagService);
        Services.AddSingleton<FeatureFlagStateProvider>(_stateProvider);
    }

    [Fact(DisplayName = "FeatureFlagView renders ChildContent when feature is enabled")]
    public void FeatureFlagView_RendersChildContent_WhenFeatureEnabled()
    {
        // Arrange - Beta is enabled by default
        var featureFlagState = Task.FromResult(new FeatureFlagState(["Beta"]));

        // Act - Use wrapper component approach
        var cut = RenderComponent<CascadingValue<Task<FeatureFlagState>>>(parameters => parameters
            .Add(p => p.Value, featureFlagState)
            .AddChildContent<FeatureFlagView>(flagParams => flagParams
                .Add(p => p.Feature, "Beta")
                .Add(p => p.ChildContent, (RenderFragment<FeatureFlagState>)(state => builder =>
                {
                    builder.AddContent(0, "Enabled Beta Feature");
                }))
            )
        );

        // Assert
        cut.Markup.ShouldContain("Enabled Beta Feature");
    }

    [Fact(DisplayName = "FeatureFlagView renders empty when feature is disabled")]
    public void FeatureFlagView_RendersEmpty_WhenFeatureDisabled()
    {
        // Arrange - empty state means no features enabled
        var featureFlagState = Task.FromResult(new FeatureFlagState([]));

        // Act
        var cut = RenderComponent<CascadingValue<Task<FeatureFlagState>>>(parameters => parameters
            .Add(p => p.Value, featureFlagState)
            .AddChildContent<FeatureFlagView>(flagParams => flagParams
                .Add(p => p.Feature, "TestFeature")
                .Add(p => p.ChildContent, (RenderFragment<FeatureFlagState>)(state => builder =>
                {
                    builder.AddContent(0, "Enabled Feature");
                }))
            )
        );

        // Assert - should be empty since feature is disabled and no Inactive provided
        cut.Markup.ShouldNotContain("Enabled Feature");
    }

    [Fact(DisplayName = "FeatureFlagView renders Active content when feature is enabled")]
    public void FeatureFlagView_RendersActiveContent_WhenFeatureEnabled()
    {
        // Arrange
        var featureFlagState = Task.FromResult(new FeatureFlagState(["Beta"]));

        // Act
        var cut = RenderComponent<CascadingValue<Task<FeatureFlagState>>>(parameters => parameters
            .Add(p => p.Value, featureFlagState)
            .AddChildContent<FeatureFlagView>(flagParams => flagParams
                .Add(p => p.Feature, "Beta")
                .Add(p => p.Active, (RenderFragment<FeatureFlagState>)(state => builder =>
                {
                    builder.AddContent(0, "This is shown when the feature flag is enabled.");
                }))
                .Add(p => p.Inactive, (RenderFragment<FeatureFlagState>)(state => builder =>
                {
                    builder.AddContent(0, "This is shown when the feature flag is disabled.");
                }))
            )
        );

        // Assert
        cut.Markup.ShouldContain("This is shown when the feature flag is enabled.");
        cut.Markup.ShouldNotContain("This is shown when the feature flag is disabled.");
    }

    [Fact(DisplayName = "FeatureFlagView renders Inactive content when feature is disabled")]
    public void FeatureFlagView_RendersInactiveContent_WhenFeatureDisabled()
    {
        // Arrange
        var featureFlagState = Task.FromResult(new FeatureFlagState([]));

        // Act
        var cut = RenderComponent<CascadingValue<Task<FeatureFlagState>>>(parameters => parameters
            .Add(p => p.Value, featureFlagState)
            .AddChildContent<FeatureFlagView>(flagParams => flagParams
                .Add(p => p.Feature, "TestFeature")
                .Add(p => p.Active, (RenderFragment<FeatureFlagState>)(state => builder =>
                {
                    builder.AddContent(0, "This is shown when the feature flag is enabled.");
                }))
                .Add(p => p.Inactive, (RenderFragment<FeatureFlagState>)(state => builder =>
                {
                    builder.AddContent(0, "This is shown when the feature flag is disabled.");
                }))
            )
        );

        // Assert
        cut.Markup.ShouldContain("This is shown when the feature flag is disabled.");
        cut.Markup.ShouldNotContain("This is shown when the feature flag is enabled.");
    }

    [Fact(DisplayName = "FeatureFlagView with non-existent feature renders Inactive")]
    public void FeatureFlagView_RendersInactive_WhenFeatureDoesNotExist()
    {
        // Arrange
        var featureFlagState = Task.FromResult(new FeatureFlagState([]));

        // Act
        var cut = RenderComponent<CascadingValue<Task<FeatureFlagState>>>(parameters => parameters
            .Add(p => p.Value, featureFlagState)
            .AddChildContent<FeatureFlagView>(flagParams => flagParams
                .Add(p => p.Feature, "NonExistentFeature")
                .Add(p => p.Active, (RenderFragment<FeatureFlagState>)(state => builder =>
                {
                    builder.AddContent(0, "Feature is active");
                }))
                .Add(p => p.Inactive, (RenderFragment<FeatureFlagState>)(state => builder =>
                {
                    builder.AddContent(0, "Feature is inactive");
                }))
            )
        );

        // Assert
        cut.Markup.ShouldContain("Feature is inactive");
        cut.Markup.ShouldNotContain("Feature is active");
    }
}
