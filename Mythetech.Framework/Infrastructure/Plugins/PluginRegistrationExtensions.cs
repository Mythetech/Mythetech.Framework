using Microsoft.Extensions.DependencyInjection;
using Mythetech.Framework.Infrastructure.MessageBus;

namespace Mythetech.Framework.Infrastructure.Plugins;

/// <summary>
/// Extensions for registering plugin framework services
/// </summary>
public static class PluginRegistrationExtensions
{
    /// <summary>
    /// Default plugin directory name
    /// </summary>
    public const string DefaultPluginDirectory = "plugins";
    
    /// <summary>
    /// Adds plugin framework infrastructure services to the DI container.
    /// Automatically adds the DisabledPluginConsumerFilter to block consumers from disabled plugins.
    /// </summary>
    public static IServiceCollection AddPluginFramework(this IServiceCollection services)
    {
        services.AddSingleton<PluginState>();
        services.AddSingleton<PluginLoader>();
        services.AddSingleton<PluginStateStore>();
        services.AddSingleton<IPluginAssetLoader, JsPluginAssetLoader>();
        services.AddScoped<PluginContext>(sp =>
        {
            var messageBus = sp.GetRequiredService<IMessageBus>();
            var stateStore = sp.GetRequiredService<PluginStateStore>();
            var storageFactory = sp.GetService<IPluginStorageFactory>();
            var assetLoader = sp.GetService<IPluginAssetLoader>();
            var linkOpenService = sp.GetService<ILinkOpenService>();
            var fileSaveService = sp.GetService<IFileSaveService>();
            
            return new PluginContext
            {
                MessageBus = messageBus,
                Services = sp,
                StateStore = stateStore,
                StorageFactory = storageFactory,
                AssetLoader = assetLoader,
                LinkOpenService = linkOpenService,
                FileSaveService = fileSaveService
            };
        });
        
        services.AddPluginConsumerFilter();
        
        return services;
    }
    
    /// <summary>
    /// Load plugins from the default 'plugins' directory relative to the app base
    /// </summary>
    public static IServiceProvider UsePlugins(this IServiceProvider services)
    {
        var baseDir = AppContext.BaseDirectory;
        var pluginDir = Path.Combine(baseDir, DefaultPluginDirectory);
        
        return services.UsePlugins(pluginDir);
    }
    
    /// <summary>
    /// Load plugins from a specific directory
    /// </summary>
    /// <param name="services">Service provider</param>
    /// <param name="pluginDirectory">Path to the plugins directory</param>
    public static IServiceProvider UsePlugins(this IServiceProvider services, string pluginDirectory)
    {
        var loader = services.GetRequiredService<PluginLoader>();
        var state = services.GetRequiredService<PluginState>();
        
        var plugins = loader.LoadPluginsFromDirectory(pluginDirectory);
        
        foreach (var plugin in plugins)
        {
            state.RegisterPlugin(plugin);
        }
        
        return services;
    }
    
    /// <summary>
    /// Register a plugin from an assembly (useful for testing or embedded plugins)
    /// </summary>
    public static IServiceProvider UsePlugin(this IServiceProvider services, System.Reflection.Assembly assembly)
    {
        var loader = services.GetRequiredService<PluginLoader>();
        var state = services.GetRequiredService<PluginState>();
        
        var plugin = loader.LoadPlugin(assembly);
        if (plugin is not null)
        {
            state.RegisterPlugin(plugin);
        }
        
        return services;
    }
}
