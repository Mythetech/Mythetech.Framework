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
    /// Register a secret manager implementation type
    /// </summary>
    public static IServiceCollection AddSecretManager<T>(this IServiceCollection services) 
        where T : class, ISecretManager
    {
        services.AddSecretManagerFramework();
        services.AddSingleton<ISecretManager, T>();
        return services;
    }
    
    /// <summary>
    /// Register a secret manager instance
    /// </summary>
    public static IServiceCollection AddSecretManager(this IServiceCollection services, ISecretManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);
        
        services.AddSecretManagerFramework();
        services.AddSingleton(manager);
        return services;
    }
    
    /// <summary>
    /// Wire up the registered secret manager to the state (call after building the service provider)
    /// </summary>
    public static IServiceProvider UseSecretManager(this IServiceProvider services)
    {
        var state = services.GetRequiredService<SecretManagerState>();
        var manager = services.GetService<ISecretManager>();
        if (manager != null)
        {
            state.RegisterManager(manager);
        }
        return services;
    }
}

