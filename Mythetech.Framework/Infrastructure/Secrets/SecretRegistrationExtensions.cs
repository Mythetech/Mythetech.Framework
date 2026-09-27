using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mythetech.Framework.Infrastructure.Settings;

namespace Mythetech.Framework.Infrastructure.Secrets;

/// <summary>
/// Extensions for registering secret manager services
/// </summary>
public static class SecretRegistrationExtensions
{
    /// <summary>
    /// Adds secret manager infrastructure services to the DI container.
    /// When the settings framework is also added, the active manager choice is saved and restored across launches.
    /// </summary>
    public static IServiceCollection AddSecretManagerFramework(this IServiceCollection services)
    {
        services.TryAddSingleton<SecretManagerState>();
        services.TryAddSingleton<SecretManagerSettings>();

        // Lets SettingsProvider load and save the choice. Without the settings framework nothing reads this
        // option, and the active manager lasts for the session.
        services.Configure<SettingsRegistrationOptions>(options =>
        {
            if (!options.DiscoveredSettingsTypes.Contains(typeof(SecretManagerSettings)))
            {
                options.DiscoveredSettingsTypes.Add(typeof(SecretManagerSettings));
            }
        });

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
    /// The first manager registered becomes the active manager by default. With the settings framework, the
    /// manager chosen in an earlier launch becomes active instead once persisted settings are loaded, whether
    /// they load before or after this call.
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

