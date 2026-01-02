using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Mythetech.Framework.Infrastructure.Secrets;

/// <summary>
/// Extensions for registering secret manager services
/// </summary>
public static class SecretRegistrationExtensions
{
    /// <summary>
    /// Adds secret manager infrastructure services to the DI container.
    /// </summary>
    public static IServiceCollection AddSecretManagerFramework(this IServiceCollection services)
    {
        services.TryAddSingleton<SecretManagerState>();
        return services;
    }

    /// <summary>
    /// Register a secret manager implementation type.
    /// Multiple managers can be registered and will all be available.
    /// </summary>
    public static IServiceCollection AddSecretManager<T>(this IServiceCollection services)
        where T : class, ISecretManager
    {
        services.AddSecretManagerFramework();
        services.AddSingleton<ISecretManager, T>();
        return services;
    }

    /// <summary>
    /// Register a secret manager instance.
    /// Multiple managers can be registered and will all be available.
    /// </summary>
    public static IServiceCollection AddSecretManager(this IServiceCollection services, ISecretManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);

        services.AddSecretManagerFramework();
        services.AddSingleton<ISecretManager>(manager);
        return services;
    }

    /// <summary>
    /// Wire up all registered secret managers to the state (call after building the service provider).
    /// All registered ISecretManager implementations will be discovered and registered with SecretManagerState.
    /// The first manager registered becomes the active manager by default.
    /// </summary>
    public static IServiceProvider UseSecretManager(this IServiceProvider services)
    {
        var state = services.GetRequiredService<SecretManagerState>();
        var managers = services.GetServices<ISecretManager>();

        foreach (var manager in managers)
        {
            state.RegisterManager(manager);
        }

        return services;
    }
}

