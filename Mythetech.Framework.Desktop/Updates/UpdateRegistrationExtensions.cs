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
    /// Adds Velopack update management services with platform-aware configuration.
    /// The callback receives pre-computed platform identifier (e.g., "windows", "macos", "linux")
    /// and channel identifier (e.g., "win", "osx", "linux").
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration callback receiving (options, platform, channel).</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddUpdateService(this IServiceCollection services, Action<UpdateServiceOptions, string, string> configure)
    {
        var (platform, channel) = GetPlatformInfo();
        return services.AddUpdateService(options => configure(options, platform, channel));
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
    /// Activates the update system. Call after UseMessageBus() and UseSettingsFramework().
    /// To check for updates on startup, call <see cref="IUpdateService.CheckForUpdatesAsync"/>
    /// from your layout's OnAfterRenderAsync method.
    /// </summary>
    public static IServiceProvider UseUpdateService(this IServiceProvider serviceProvider)
    {
        // Currently a no-op, but provides a consistent activation pattern
        // and a place to add future initialization logic
        return serviceProvider;
    }

    private static (string Platform, string Channel) GetPlatformInfo()
    {
        if (OperatingSystem.IsWindows())
            return ("windows", "win");
        if (OperatingSystem.IsMacOS())
            return ("macos", "osx");
        return ("linux", "linux");
    }
}
