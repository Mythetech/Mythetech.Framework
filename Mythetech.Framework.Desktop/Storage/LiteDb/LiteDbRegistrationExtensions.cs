using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Framework.Infrastructure.Queue;
using Mythetech.Framework.Infrastructure.Settings;

namespace Mythetech.Framework.Desktop.Storage.LiteDb;

public static class LiteDbRegistrationExtensions
{
    public const string DefaultDatabaseName = "plugins.db";
    public const string DefaultSettingsDatabaseName = "settings.db";
    public const string DefaultPluginStateDatabaseName = "plugin_state.db";
    public const string DefaultQueueDatabaseName = "queue.db";

    public static IServiceCollection AddPluginStorage(this IServiceCollection services)
        => services.AddPluginStorage("Mythetech");

    public static IServiceCollection AddPluginStorage(this IServiceCollection services, string appName)
    {
        var pluginDbPath = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            appName,
            DefaultDatabaseName);

        try { Directory.CreateDirectory(Path.GetDirectoryName(pluginDbPath)!); } catch { /* Let Lazy handle failures */ }

        return services.AddPluginStorageWithPath(pluginDbPath);
    }

    public static IServiceCollection AddPluginStorageWithPath(this IServiceCollection services, string databasePath)
    {
        services.AddSingleton<IPluginStorageFactory>(sp =>
        {
            var logger = sp.GetService<ILogger<LiteDbPluginStorageFactory>>();
            return new LiteDbPluginStorageFactory(databasePath, logger);
        });

        return services;
    }

    public static IServiceCollection AddDesktopSettingsStorage(this IServiceCollection services)
        => services.AddDesktopSettingsStorage("Mythetech");

    public static IServiceCollection AddDesktopSettingsStorage(this IServiceCollection services, string appName)
    {
        var settingsDbPath = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            appName,
            DefaultSettingsDatabaseName);

        try { Directory.CreateDirectory(Path.GetDirectoryName(settingsDbPath)!); } catch { /* Let Lazy handle failures */ }

        return services.AddDesktopSettingsStorageWithPath(settingsDbPath);
    }

    public static IServiceCollection AddDesktopSettingsStorageWithPath(this IServiceCollection services, string databasePath)
    {
        services.AddSingleton<ISettingsStorage>(sp =>
        {
            var logger = sp.GetService<ILogger<LiteDbSettingsStorage>>();
            return new LiteDbSettingsStorage(databasePath, logger);
        });

        return services;
    }

    public static IServiceCollection AddPluginStateProvider(this IServiceCollection services)
        => services.AddPluginStateProvider("Mythetech");

    public static IServiceCollection AddPluginStateProvider(this IServiceCollection services, string appName)
    {
        var stateDbPath = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            appName,
            DefaultPluginStateDatabaseName);

        try { Directory.CreateDirectory(Path.GetDirectoryName(stateDbPath)!); } catch { /* Let Lazy handle failures */ }

        return services.AddPluginStateProviderWithPath(stateDbPath);
    }

    public static IServiceCollection AddPluginStateProviderWithPath(this IServiceCollection services, string databasePath)
    {
        services.AddSingleton<IPluginStateProvider>(sp =>
        {
            var logger = sp.GetService<ILogger<LiteDbPluginStateProvider>>();
            return new LiteDbPluginStateProvider(databasePath, logger);
        });

        return services;
    }

    public static IServiceCollection AddLiteDbQueue(this IServiceCollection services)
        => services.AddLiteDbQueue("Mythetech");

    public static IServiceCollection AddLiteDbQueue(this IServiceCollection services, string appName)
    {
        var queueDbPath = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            appName,
            DefaultQueueDatabaseName);

        try { Directory.CreateDirectory(Path.GetDirectoryName(queueDbPath)!); } catch { /* Let Lazy handle failures */ }

        return services.AddLiteDbQueueWithPath(queueDbPath);
    }

    public static IServiceCollection AddLiteDbQueueWithPath(this IServiceCollection services, string databasePath)
    {
        services.AddSingleton<IQueueFactory>(sp =>
        {
            var loggerFactory = sp.GetService<ILoggerFactory>();
            return new LiteDbQueueFactory(databasePath, loggerFactory);
        });

        return services;
    }
}
