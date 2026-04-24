using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Framework.Infrastructure.Queue;
using Mythetech.Framework.Infrastructure.Settings;

namespace Mythetech.Framework.WebAssembly.Storage.Sqlite;

public static class SqliteRegistrationExtensions
{
    public const string DefaultPluginsDatabaseName = "plugins.db";
    public const string DefaultSettingsDatabaseName = "settings.db";
    public const string DefaultPluginStateDatabaseName = "plugin_state.db";
    public const string DefaultQueueDatabaseName = "queue.db";

    public static IServiceCollection AddSqlitePluginStorage(this IServiceCollection services)
        => services.AddSqlitePluginStorage(DefaultPluginsDatabaseName);

    public static IServiceCollection AddSqlitePluginStorage(this IServiceCollection services, string databaseName)
    {
        services.AddSingleton<IPluginStorageFactory>(sp =>
        {
            var logger = sp.GetService<ILogger<SqlitePluginStorageFactory>>();
            return new SqlitePluginStorageFactory(databaseName, logger);
        });

        return services;
    }

    public static IServiceCollection AddSqliteSettingsStorage(this IServiceCollection services)
        => services.AddSqliteSettingsStorage(DefaultSettingsDatabaseName);

    public static IServiceCollection AddSqliteSettingsStorage(this IServiceCollection services, string databaseName)
    {
        services.AddSingleton<ISettingsStorage>(sp =>
        {
            var logger = sp.GetService<ILogger<SqliteSettingsStorage>>();
            return new SqliteSettingsStorage(databaseName, logger);
        });

        return services;
    }

    public static IServiceCollection AddSqlitePluginStateProvider(this IServiceCollection services)
        => services.AddSqlitePluginStateProvider(DefaultPluginStateDatabaseName);

    public static IServiceCollection AddSqlitePluginStateProvider(this IServiceCollection services, string databaseName)
    {
        services.AddSingleton<IPluginStateProvider>(sp =>
        {
            var logger = sp.GetService<ILogger<SqlitePluginStateProvider>>();
            return new SqlitePluginStateProvider(databaseName, logger);
        });

        return services;
    }

    public static IServiceCollection AddSqliteQueue(this IServiceCollection services)
        => services.AddSqliteQueue(DefaultQueueDatabaseName);

    public static IServiceCollection AddSqliteQueue(this IServiceCollection services, string databaseName)
    {
        services.AddSingleton<IQueueFactory>(sp =>
        {
            var loggerFactory = sp.GetService<ILoggerFactory>();
            return new SqliteQueueFactory(databaseName, loggerFactory);
        });

        return services;
    }
}
