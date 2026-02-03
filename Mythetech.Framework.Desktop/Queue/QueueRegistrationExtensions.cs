using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Queue;

namespace Mythetech.Framework.Desktop.Queue;

/// <summary>
/// Extension methods for registering LiteDB-based queue services.
/// </summary>
public static class QueueRegistrationExtensions
{
    /// <summary>
    /// Default database filename for queue storage.
    /// </summary>
    public const string DefaultQueueDatabaseName = "queue.db";

    /// <summary>
    /// Registers the LiteDB-based queue factory for Desktop.
    /// Database is stored in the user's local application data directory under "Mythetech".
    /// </summary>
    public static IServiceCollection AddLiteDbQueue(this IServiceCollection services)
        => services.AddLiteDbQueue("Mythetech");

    /// <summary>
    /// Registers the LiteDB-based queue factory for Desktop with a custom application name.
    /// Database is stored in the user's local application data directory under the specified app name.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="appName">Application name for the storage folder.</param>
    public static IServiceCollection AddLiteDbQueue(this IServiceCollection services, string appName)
    {
        var queueDbPath = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            appName,
            DefaultQueueDatabaseName);

        try { Directory.CreateDirectory(Path.GetDirectoryName(queueDbPath)!); } catch { /* Let Lazy handle failures */ }

        return services.AddLiteDbQueueWithPath(queueDbPath);
    }

    /// <summary>
    /// Registers the LiteDB-based queue factory for Desktop with a custom database path.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="databasePath">Full path to the LiteDB database file.</param>
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
