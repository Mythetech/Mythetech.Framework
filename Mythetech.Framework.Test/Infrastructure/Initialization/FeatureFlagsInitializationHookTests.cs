using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mythetech.Framework.Components.FeatureFlags;
using Mythetech.Framework.Infrastructure.FeatureFlags;
using Mythetech.Framework.Infrastructure.FeatureFlags.Events;
using Mythetech.Framework.Infrastructure.Initialization;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Settings;
using Mythetech.Framework.Test.Infrastructure.FeatureFlags;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Initialization;

public class FeatureFlagsInitializationHookTests
{
    [Fact(DisplayName = "FeatureFlagsInitializationHook registers the app's feature flags and announces them")]
    public async Task InitializeAsync_RegistersFlags_AndPublishesInitialized()
    {
        var bus = Substitute.For<IMessageBus>();
        var settingsProvider = Substitute.For<ISettingsProvider>();
        settingsProvider.GetAllSettings().Returns([new TestFeatureFlags()]);
        var services = new ServiceCollection()
            .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
            .AddSingleton(bus)
            .AddSingleton(settingsProvider)
            .AddFeatureFlags()
            .BuildServiceProvider();
        var hook = new FeatureFlagsInitializationHook(services, NullLogger<FeatureFlagsInitializationHook>.Instance);

        await hook.InitializeAsync();

        services.GetRequiredService<IFeatureFlagRegistry>().GetFlag("Beta").ShouldNotBeNull();
        await bus.Received(1).PublishAsync(Arg.Is<FeatureFlagsInitialized>(e => e.FlagKeys.Contains("Beta")));
    }

    [Fact(DisplayName = "FeatureFlagsInitializationHook refreshes feature flag UI that rendered before it ran")]
    public async Task InitializeAsync_NotifiesFeatureFlagStateChanged()
    {
        var settingsProvider = Substitute.For<ISettingsProvider>();
        settingsProvider.GetAllSettings().Returns([new TestFeatureFlags()]);
        var services = new ServiceCollection()
            .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
            .AddSingleton(Substitute.For<IMessageBus>())
            .AddSingleton(settingsProvider)
            .AddFeatureFlags()
            .BuildServiceProvider();
        var notified = false;
        services.GetRequiredService<FeatureFlagStateProvider>().FeatureFlagStateChanged += _ => notified = true;
        var hook = new FeatureFlagsInitializationHook(services, NullLogger<FeatureFlagsInitializationHook>.Instance);

        await hook.InitializeAsync();

        notified.ShouldBeTrue();
    }

    [Fact(DisplayName = "FeatureFlagsInitializationHook runs after settings load")]
    public void Order_IsAfterSettings()
    {
        var hook = new FeatureFlagsInitializationHook(new ServiceCollection().BuildServiceProvider(), NullLogger<FeatureFlagsInitializationHook>.Instance);

        hook.Order.ShouldBeGreaterThan(new SettingsInitializationHook(new ServiceCollection().BuildServiceProvider(), NullLogger<SettingsInitializationHook>.Instance).Order);
    }
}
