using Microsoft.Extensions.DependencyInjection;
using Mythetech.Framework.Desktop.Photino;
using Mythetech.Framework.Desktop.Services;
using Mythetech.Framework.Infrastructure;
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
    /// Database is stored in the application's base directory.
    /// </summary>
    public static IServiceCollection AddPluginStorage(this IServiceCollection services)
    {
        return services.AddPluginStorage(Path.Combine(AppContext.BaseDirectory, DefaultDatabaseName));
    }
    
    /// <summary>
    /// Registers the plugin storage factory for Desktop using LiteDB with a custom database path.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="databasePath">Full path to the LiteDB database file</param>
    public static IServiceCollection AddPluginStorage(this IServiceCollection services, string databasePath)
    {
        services.AddSingleton<IPluginStorageFactory>(new LiteDbPluginStorageFactory(databasePath));
        
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
}