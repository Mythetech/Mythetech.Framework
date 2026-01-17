using Microsoft.Extensions.DependencyInjection;
using Mythetech.Framework.Infrastructure.Settings;

namespace Mythetech.Framework.Desktop.Updates;

/// <summary>
/// Extension methods for registering update services.
/// </summary>
public static class UpdateRegistrationExtensions
{
    /// <summary>
    /// Adds Velopack update management services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="updateUrl">URL to the update feed.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddUpdateService(this IServiceCollection services, string updateUrl)
    {
        return services.AddUpdateService(options => options.UpdateUrl = updateUrl);
    }

    /// <summary>
    /// Adds Velopack update management services with full configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration callback.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddUpdateService(this IServiceCollection services, Action<UpdateServiceOptions> configure)
    {
        services.Configure(configure);
        services.AddSingleton<IUpdateService, VelopackUpdateService>();

        return services;
    }

    /// <summary>
    /// Activates the update system by checking for updates on startup if enabled.
    /// Call after UseMessageBus() and UseSettingsFramework().
    /// </summary>
    public static async Task<IServiceProvider> UseUpdateServiceAsync(this IServiceProvider serviceProvider)
    {
        var settingsProvider = serviceProvider.GetService<ISettingsProvider>();
        var settings = settingsProvider?.GetSettings<UpdateSettings>();

        if (settings?.AutoCheckOnStartup == true)
        {
            var updateService = serviceProvider.GetRequiredService<IUpdateService>();
            if (updateService.IsInstalled)
            {
                try
                {
                    await updateService.CheckForUpdatesAsync();
                }
                catch
                {
                    // Silently ignore startup check failures
                }
            }
        }

        return serviceProvider;
    }
}
