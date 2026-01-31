using Microsoft.Extensions.DependencyInjection;

namespace Mythetech.Framework.Infrastructure.Initialization;

/// <summary>
/// Extension methods for registering async initialization services.
/// </summary>
public static class InitializationRegistrationExtensions
{
    /// <summary>
    /// Adds the async initialization infrastructure to the service collection.
    /// Call this before registering any initialization hooks.
    /// </summary>
    public static IServiceCollection AddAsyncInitialization(this IServiceCollection services)
    {
        services.AddSingleton<IAsyncInitializationHost, AsyncInitializationHost>();
        return services;
    }

    /// <summary>
    /// Registers an initialization hook that will run during async initialization.
    /// Hooks are executed in order based on their Order property.
    /// </summary>
    /// <typeparam name="THook">The hook type to register</typeparam>
    public static IServiceCollection AddInitializationHook<THook>(this IServiceCollection services)
        where THook : class, IAsyncInitializationHook
    {
        services.AddSingleton<IAsyncInitializationHook, THook>();
        return services;
    }

    /// <summary>
    /// Registers an initialization hook instance that will run during async initialization.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="hook">The hook instance</param>
    public static IServiceCollection AddInitializationHook(this IServiceCollection services, IAsyncInitializationHook hook)
    {
        services.AddSingleton(hook);
        return services;
    }

    /// <summary>
    /// Registers an initialization hook using a factory function.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="factory">Factory function to create the hook</param>
    public static IServiceCollection AddInitializationHook(
        this IServiceCollection services,
        Func<IServiceProvider, IAsyncInitializationHook> factory)
    {
        services.AddSingleton(factory);
        return services;
    }

    /// <summary>
    /// Runs async initialization after the service provider is built.
    /// This executes all registered <see cref="IAsyncInitializationHook"/> instances.
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public static async Task<IServiceProvider> UseAsyncInitialization(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        var host = serviceProvider.GetRequiredService<IAsyncInitializationHost>();
        await host.InitializeAsync(cancellationToken);
        return serviceProvider;
    }
}
