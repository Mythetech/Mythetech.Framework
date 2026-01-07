using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Desktop.Environment;
using Mythetech.Framework.Desktop.Photino;
using Mythetech.Framework.Desktop.Services;
using Mythetech.Framework.Infrastructure;
using Mythetech.Framework.Infrastructure.Environment;
using Mythetech.Framework.Infrastructure.Files;
using Mythetech.Framework.Infrastructure.Plugins;

namespace Mythetech.Framework.Desktop;

/// <summary>
/// Generic desktop service registration extensions
/// </summary>
public static class DesktopRegistrationExtensions
{
    /// <summary>
    /// Default database filename for plugin storage
    /// </summary>
    public const string DefaultDatabaseName = "plugins.db";
    
    /// <summary>
    /// Adds required desktop services for a given host
    /// </summary>
    public static IServiceCollection AddDesktopServices(this IServiceCollection services, DesktopHost host = DesktopHost.Photino)
    {
        switch (host)
        {
            case DesktopHost.Photino:
                services.AddPhotinoServices();
                break;
            default:
                throw new ArgumentException("Invalid host type", nameof(host));
        }

        services.AddLinkOpenService();
        services.AddPluginStorage();
        services.AddDesktopAssetLoader();
        services.AddShowFileService();

        return services;
    }
    
    /// <summary>
    /// Registers the desktop-specific plugin asset loader that reads files from disk
    /// and injects them inline (since dynamic plugins can't use _content/ URLs).
    /// This overrides the default JsPluginAssetLoader.
    /// </summary>
    public static IServiceCollection AddDesktopAssetLoader(this IServiceCollection services)
    {
        // Remove any existing registration
        var existing = services.FirstOrDefault(d => d.ServiceType == typeof(IPluginAssetLoader));
        if (existing != null)
        {
            services.Remove(existing);
        }
        
        services.AddScoped<IPluginAssetLoader, DesktopPluginAssetLoader>();
        
        return services;
    }

    /// <summary>
    /// Registers the link opening service for Desktop
    /// </summary>
    public static IServiceCollection AddLinkOpenService(this IServiceCollection services)
    {
        services.AddTransient<ILinkOpenService, LinkOpenService>();

        return services;
    }
    
    /// <summary>
    /// Registers the plugin storage factory for Desktop using LiteDB.
    /// Database is stored in the user's local application data directory under "Mythetech".
    /// </summary>
    public static IServiceCollection AddPluginStorage(this IServiceCollection services)
        => services.AddPluginStorage("Mythetech");

    /// <summary>
    /// Registers the plugin storage factory for Desktop using LiteDB with a custom application name.
    /// Database is stored in the user's local application data directory under the specified app name.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="appName">Application name for the storage folder</param>
    public static IServiceCollection AddPluginStorage(this IServiceCollection services, string appName)
    {
        var pluginDbPath = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            appName,
            DefaultDatabaseName);

        try { Directory.CreateDirectory(Path.GetDirectoryName(pluginDbPath)!); } catch { /* Let Lazy handle failures */ }

        return services.AddPluginStorageWithPath(pluginDbPath);
    }

    /// <summary>
    /// Registers the plugin storage factory for Desktop using LiteDB with a custom database path.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="databasePath">Full path to the LiteDB database file</param>
    public static IServiceCollection AddPluginStorageWithPath(this IServiceCollection services, string databasePath)
    {
        services.AddSingleton<IPluginStorageFactory>(sp =>
        {
            var logger = sp.GetService<ILogger<LiteDbPluginStorageFactory>>();
            return new LiteDbPluginStorageFactory(databasePath, logger);
        });

        return services;
    }

    /// <summary>
    /// Registers the show file service for Desktop (cross-platform file/folder reveal)
    /// </summary>
    public static IServiceCollection AddShowFileService(this IServiceCollection services)
    {
        services.AddTransient<IShowFileService, ShowFileService>();

        return services;
    }

    /// <summary>
    /// Registers the runtime environment for Desktop with default Development configuration.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="development">Is development</param>
    /// <param name="version">Application version (defaults to entry assembly version)</param>
    /// <param name="baseAddress">Base address (defaults to "app://")</param>
    public static IServiceCollection AddRuntimeEnvironment(this IServiceCollection services, bool? development = false, Version? version = null, string baseAddress = "app://")
    {
        return services.AddRuntimeEnvironment(development is true ? DesktopRuntimeEnvironment.Development(version, baseAddress) : DesktopRuntimeEnvironment.Production(version, baseAddress));
    }

    /// <summary>
    /// Registers a custom runtime environment for Desktop.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="environment">The runtime environment instance</param>
    public static IServiceCollection AddRuntimeEnvironment(this IServiceCollection services, DesktopRuntimeEnvironment environment)
    {
        services.AddSingleton<IRuntimeEnvironment>(environment);
        return services;
    }
}