using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Settings.Consumers;

namespace Mythetech.Framework.Infrastructure.Settings;

/// <summary>
/// Extension methods for registering settings framework services.
/// </summary>
public static class SettingsRegistrationExtensions
{
    /// <summary>
    /// Adds the core settings framework services.
    /// Call this before registering any settings models.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSettingsFramework(this IServiceCollection services)
    {
        services.AddSingleton<ISettingsProvider, SettingsProvider>();

        // Register consumers - they'll be discovered by AddMessageBus
        services.AddTransient<GenericSettingsModelConverter>();
        services.AddTransient<SettingsPersister>();

        // Also register consumers to the DI container
        var assembly = Assembly.GetExecutingAssembly();
        services.RegisterConsumers(assembly);

        return services;
    }

    /// <summary>
    /// Registers a custom settings storage implementation.
    /// </summary>
    /// <typeparam name="TStorage">The storage implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSettingsStorage<TStorage>(this IServiceCollection services)
        where TStorage : class, ISettingsStorage
    {
        services.AddSingleton<ISettingsStorage, TStorage>();
        return services;
    }

    /// <summary>
    /// Registers a settings storage instance.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="storage">The storage instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSettingsStorage(this IServiceCollection services, ISettingsStorage storage)
    {
        services.AddSingleton(storage);
        return services;
    }

    /// <summary>
    /// Registers a settings model with the provider.
    /// Call after building the service provider.
    /// </summary>
    /// <typeparam name="T">The settings type to register.</typeparam>
    /// <param name="serviceProvider">The built service provider.</param>
    /// <returns>The service provider for chaining.</returns>
    public static IServiceProvider RegisterSettings<T>(this IServiceProvider serviceProvider)
        where T : SettingsBase, new()
    {
        var provider = serviceProvider.GetRequiredService<ISettingsProvider>();
        provider.RegisterSettings(new T());
        return serviceProvider;
    }

    /// <summary>
    /// Registers a settings model instance with the provider.
    /// Call after building the service provider.
    /// </summary>
    /// <param name="serviceProvider">The built service provider.</param>
    /// <param name="settings">The settings instance to register.</param>
    /// <returns>The service provider for chaining.</returns>
    public static IServiceProvider RegisterSettings(this IServiceProvider serviceProvider, SettingsBase settings)
    {
        var provider = serviceProvider.GetRequiredService<ISettingsProvider>();
        provider.RegisterSettings(settings);
        return serviceProvider;
    }

    /// <summary>
    /// Loads persisted settings into registered models.
    /// Call after all settings are registered.
    /// </summary>
    /// <param name="serviceProvider">The built service provider.</param>
    /// <returns>The service provider for chaining.</returns>
    public static async Task<IServiceProvider> LoadPersistedSettingsAsync(this IServiceProvider serviceProvider)
    {
        var storage = serviceProvider.GetService<ISettingsStorage>();
        if (storage == null)
        {
            // No storage configured - skip loading
            return serviceProvider;
        }

        var provider = serviceProvider.GetRequiredService<ISettingsProvider>();
        var data = await storage.LoadAllSettingsAsync();
        await provider.ApplyPersistedSettingsAsync(data);

        return serviceProvider;
    }

    /// <summary>
    /// Activates the settings framework by registering consumers to the message bus.
    /// Call after UseMessageBus().
    /// </summary>
    /// <param name="serviceProvider">The built service provider.</param>
    /// <returns>The service provider for chaining.</returns>
    public static IServiceProvider UseSettingsFramework(this IServiceProvider serviceProvider)
    {
        var bus = serviceProvider.GetRequiredService<IMessageBus>();
        var assembly = typeof(SettingsRegistrationExtensions).Assembly;
        bus.RegisterConsumersToBus(assembly);
        return serviceProvider;
    }
}
