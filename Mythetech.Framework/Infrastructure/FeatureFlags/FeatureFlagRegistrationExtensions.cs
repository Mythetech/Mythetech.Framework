using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Mythetech.Framework.Components.FeatureFlags;
using Mythetech.Framework.Infrastructure.FeatureFlags.Consumers;
using Mythetech.Framework.Infrastructure.FeatureFlags.Events;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Settings;

namespace Mythetech.Framework.Infrastructure.FeatureFlags;

/// <summary>
/// Extension methods for registering feature flag services.
/// </summary>
public static class FeatureFlagRegistrationExtensions
{
    /// <summary>
    /// Adds feature flag infrastructure to the service collection.
    /// Call this after AddSettingsFramework() and before RegisterSettingsFromAssembly().
    /// </summary>
    public static IServiceCollection AddFeatureFlags(this IServiceCollection services)
    {
        services.AddSingleton<IFeatureFlagRegistry, FeatureFlagRegistry>();
        services.AddSingleton<DefaultFeatureFlagStateProvider>();
        services.AddSingleton<FeatureFlagStateProvider>(sp =>
            sp.GetRequiredService<DefaultFeatureFlagStateProvider>());
        services.AddScoped<IFeatureFlagService, FeatureFlagService>();
        services.AddSingleton<FeatureFlagChangeTracker>();

        return services;
    }

    /// <summary>
    /// Initializes feature flags after the service provider is built.
    /// Registers all discovered FeatureFlagsSettingsBase instances with the registry.
    /// Call this after LoadPersistedSettingsAsync().
    /// </summary>
    public static async Task<IServiceProvider> UseFeatureFlags(this IServiceProvider serviceProvider)
    {
        var registry = serviceProvider.GetRequiredService<IFeatureFlagRegistry>();
        var settingsProvider = serviceProvider.GetRequiredService<ISettingsProvider>();
        var bus = serviceProvider.GetRequiredService<IMessageBus>();

        // Register all feature flag settings
        foreach (var settings in settingsProvider.GetAllSettings())
        {
            if (settings is FeatureFlagsSettingsBase flagSettings)
            {
                registry.RegisterFlagSettings(flagSettings);
            }
        }

        // Eagerly resolve and subscribe the change tracker so it captures initial state NOW
        // (must be done before any settings changes can occur)
        var changeTracker = serviceProvider.GetRequiredService<FeatureFlagChangeTracker>();
        bus.Subscribe<Settings.Events.SettingsModelChanged>(changeTracker);

        // Emit initialization event
        var allFlags = registry.GetAllFlags().Select(f => f.Key).ToList();
        await bus.PublishAsync(new FeatureFlagsInitialized(allFlags));

        return serviceProvider;
    }
}
