using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Framework.Infrastructure.Queue;
using Mythetech.Framework.Infrastructure.Settings;

namespace Mythetech.Framework.Desktop.Storage.Sqlite;

public static class SqliteRegistrationExtensions
{
    public const string DefaultDatabaseName = "plugins.sqlite";
    public const string DefaultSettingsDatabaseName = "settings.sqlite";
    public const string DefaultPluginStateDatabaseName = "plugin_state.sqlite";
    public const string DefaultQueueDatabaseName = "queue.sqlite";

    public static IServiceCollection AddSqlitePluginStorage(this IServiceCollection services)
        => services.AddSqlitePluginStorage("Mythetech");

    public static IServiceCollection AddSqlitePluginStorage(this IServiceCollection services, string appName)
    {
        var dbPath = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            appName,
            DefaultDatabaseName);

        try { Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!); } catch { /* Let Lazy handle failures */ }

        return services.AddSqlitePluginStorageWithPath(dbPath);
    }

    public static IServiceCollection AddSqlitePluginStorageWithPath(this IServiceCollection services, string databasePath)
    {
        services.AddSingleton<IPluginStorageFactory>(sp =>
        {
            var logger = sp.GetService<ILogger<SqlitePluginStorageFactory>>();
            return new SqlitePluginStorageFactory(databasePath, logger);
        });

        return services;
    }

    public static IServiceCollection AddSqliteSettingsStorage(this IServiceCollection services)
        => services.AddSqliteSettingsStorage("Mythetech");

    public static IServiceCollection AddSqliteSettingsStorage(this IServiceCollection services, string appName)
    {
        var dbPath = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            appName,
            DefaultSettingsDatabaseName);

        try { Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!); } catch { /* Let Lazy handle failures */ }

        return services.AddSqliteSettingsStorageWithPath(dbPath);
    }

    public static IServiceCollection AddSqliteSettingsStorageWithPath(this IServiceCollection services, string databasePath)
    {
        services.AddSingleton<ISettingsStorage>(sp =>
        {
            var logger = sp.GetService<ILogger<SqliteSettingsStorage>>();
            return new SqliteSettingsStorage(databasePath, logger);
        });

        return services;
    }

    public static IServiceCollection AddSqlitePluginStateProvider(this IServiceCollection services)
        => services.AddSqlitePluginStateProvider("Mythetech");

    public static IServiceCollection AddSqlitePluginStateProvider(this IServiceCollection services, string appName)
    {
        var dbPath = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            appName,
            DefaultPluginStateDatabaseName);

        try { Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!); } catch { /* Let Lazy handle failures */ }

        return services.AddSqlitePluginStateProviderWithPath(dbPath);
    }

    public static IServiceCollection AddSqlitePluginStateProviderWithPath(this IServiceCollection services, string databasePath)
    {
        services.AddSingleton<IPluginStateProvider>(sp =>
        {
            var logger = sp.GetService<ILogger<SqlitePluginStateProvider>>();
            return new SqlitePluginStateProvider(databasePath, logger);
        });

        return services;
    }

    public static IServiceCollection AddSqliteQueue(this IServiceCollection services)
        => services.AddSqliteQueue("Mythetech");

    public static IServiceCollection AddSqliteQueue(this IServiceCollection services, string appName)
    {
        var dbPath = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            appName,
            DefaultQueueDatabaseName);

        try { Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!); } catch { /* Let Lazy handle failures */ }

        return services.AddSqliteQueueWithPath(dbPath);
    }

    public static IServiceCollection AddSqliteQueueWithPath(this IServiceCollection services, string databasePath)
    {
        services.AddSingleton<IQueueFactory>(sp =>
        {
            var loggerFactory = sp.GetService<ILoggerFactory>();
            return new SqliteQueueFactory(databasePath, loggerFactory);
        });

        return services;
    }
}
