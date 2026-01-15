using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mythetech.Framework.Infrastructure.Files;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Plugins.Consumers;
using Mythetech.Framework.Infrastructure.Plugins.Events;
using Mythetech.Framework.Infrastructure.Settings;
using Mythetech.Framework.Infrastructure.Settings.Events;

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
        return services.AddPluginFramework(_ => { });
    }
    
    /// <summary>
    /// Adds plugin framework infrastructure services to the DI container.
    /// Automatically adds the DisabledPluginConsumerFilter to block consumers from disabled plugins.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configure">Optional configuration action for plugin registry options</param>
    public static IServiceCollection AddPluginFramework(this IServiceCollection services, Action<PluginRegistryOptions> configure)
    {
        services.Configure(configure);
        
        services.AddSingleton<PluginState>();
        services.AddSingleton<PluginLoader>();
        services.AddSingleton<PluginStateStore>();
        
        services.TryAddScoped<IPluginAssetLoader, JsPluginAssetLoader>();
        services.TryAddScoped<IPluginRegistryService, PluginRegistryService>();
        
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

        // Settings consumer
        services.AddTransient<PluginSettingsConsumer>();

        return services;
    }
    
    /// <summary>
    /// Initialize plugin framework services including message bus registrations.
    /// Call this after UseMessageBus() to register plugin settings consumer.
    /// </summary>
    public static IServiceProvider UsePluginFramework(this IServiceProvider services)
    {
        var messageBus = services.GetRequiredService<IMessageBus>();

        // Register settings consumer
        messageBus.RegisterConsumerType<SettingsModelChanged<PluginSettings>, PluginSettingsConsumer>();

        // Set state provider and message bus if available
        var pluginState = services.GetRequiredService<PluginState>();
        var stateProvider = services.GetService<IPluginStateProvider>();
        pluginState.SetStateProvider(stateProvider);
        pluginState.SetMessageBus(messageBus);

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

        var fullPath = Path.GetFullPath(pluginDirectory);
        state.SetPluginDirectory(fullPath);

        var plugins = loader.LoadPluginsFromDirectory(fullPath);

        foreach (var plugin in plugins)
        {
            try
            {
                state.RegisterPlugin(plugin);
            }
            catch (InvalidOperationException)
            {
                state.RegisterOrUpgradePlugin(plugin);
            }
        }

        state.PluginsLoaded = true;
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
            try
            {
                state.RegisterPlugin(plugin);
            }
            catch (InvalidOperationException)
            {
                state.RegisterOrUpgradePlugin(plugin);
            }
        }

        return services;
    }

    /// <summary>
    /// Load plugins using the CustomPluginDirectory setting if set,
    /// otherwise falls back to the default directory.
    /// Publishes lifecycle events via MessageBus.
    /// Call this after settings are loaded (e.g., in OnAfterRenderAsync).
    /// </summary>
    /// <param name="services">Service provider</param>
    /// <returns>The service provider for chaining</returns>
    public static async Task<IServiceProvider> UsePluginsFromSettingsAsync(this IServiceProvider services)
    {
        var state = services.GetRequiredService<PluginState>();

        if (state.PluginsLoaded)
        {
            Console.WriteLine("[PluginLoader] Plugins already loaded, skipping");
            return services;
        }

        var messageBus = services.GetService<IMessageBus>();
        var settingsProvider = services.GetService<ISettingsProvider>();
        var pluginSettings = settingsProvider?.GetSettings<PluginSettings>();

        Console.WriteLine($"[PluginLoader] CustomPluginDirectory setting: '{pluginSettings?.CustomPluginDirectory ?? "(null)"}'");

        string pluginDirectory;
        if (!string.IsNullOrWhiteSpace(pluginSettings?.CustomPluginDirectory)
            && Directory.Exists(pluginSettings.CustomPluginDirectory))
        {
            pluginDirectory = pluginSettings.CustomPluginDirectory;
            Console.WriteLine($"[PluginLoader] Using custom directory: {pluginDirectory}");
        }
        else
        {
            pluginDirectory = Path.Combine(AppContext.BaseDirectory, DefaultPluginDirectory);
            Console.WriteLine($"[PluginLoader] Using default directory: {pluginDirectory}");
        }

        if (messageBus != null)
        {
            await messageBus.PublishAsync(new PluginsLoadingStarted(pluginDirectory));
        }

        services.UsePlugins(pluginDirectory);

        Console.WriteLine($"[PluginLoader] Loaded {state.Plugins.Count} plugins from {pluginDirectory}");
        foreach (var plugin in state.Plugins)
        {
            Console.WriteLine($"[PluginLoader]   - {plugin.Manifest.Name} v{plugin.Manifest.Version} from {plugin.SourcePath}");
        }

        if (messageBus != null)
        {
            await messageBus.PublishAsync(new PluginsLoadingCompleted(pluginDirectory, state.Plugins.Count));
        }

        return services;
    }
}
