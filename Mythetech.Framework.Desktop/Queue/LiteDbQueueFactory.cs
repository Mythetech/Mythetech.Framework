using System.Collections.Concurrent;
using LiteDB;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Queue;

namespace Mythetech.Framework.Desktop.Queue;

/// <summary>
/// Factory for creating LiteDB-backed queue instances.
/// Queues are stored in a single database file with separate collections per queue name.
/// </summary>
public class LiteDbQueueFactory : IQueueFactory, IDisposable
{
    private readonly Lazy<ILiteDatabase?> _database;
    private readonly ConcurrentDictionary<string, object> _queues = new();
    private readonly ILoggerFactory? _loggerFactory;
    private readonly ILogger<LiteDbQueueFactory>? _logger;

    /// <summary>
    /// Creates a new LiteDB queue factory.
    /// Uses lazy initialization to defer database creation until first use.
    /// </summary>
    /// <param name="databasePath">Path to the LiteDB file.</param>
    /// <param name="loggerFactory">Optional logger factory for creating queue loggers.</param>
    public LiteDbQueueFactory(string databasePath, ILoggerFactory? loggerFactory = null)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory?.CreateLogger<LiteDbQueueFactory>();

        _database = new Lazy<ILiteDatabase?>(() =>
        {
            try
            {
                return new LiteDatabase(databasePath);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize queue storage at {DatabasePath}. Queue persistence will be unavailable.", databasePath);
                return null;
            }
        });
    }

    /// <summary>
    /// Creates a new LiteDB queue factory with an existing database.
    /// </summary>
    /// <param name="database">An existing LiteDB database instance.</param>
    /// <param name="loggerFactory">Optional logger factory for creating queue loggers.</param>
    public LiteDbQueueFactory(ILiteDatabase database, ILoggerFactory? loggerFactory = null)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory?.CreateLogger<LiteDbQueueFactory>();
        _database = new Lazy<ILiteDatabase?>(() => database);
    }

    /// <inheritdoc />
    public IQueue<T>? GetQueue<T>(string queueName) where T : class
    {
        if (string.IsNullOrWhiteSpace(queueName))
        {
            throw new ArgumentException("Queue name cannot be empty", nameof(queueName));
        }

        var db = _database.Value;
        if (db == null)
        {
            _logger?.LogDebug("Queue storage unavailable, returning null for queue {QueueName}", queueName);
            return null;
        }

        // Normalize queue name for collection name (replace dots with underscores)
        var collectionName = $"queue_{queueName.Replace(".", "_")}";
        var cacheKey = $"{collectionName}_{typeof(T).FullName}";

        var queue = _queues.GetOrAdd(cacheKey, _ =>
        {
            var logger = _loggerFactory?.CreateLogger<LiteDbQueue<T>>();
            return new LiteDbQueue<T>(db, collectionName, logger);
        });

        return (IQueue<T>)queue;
    }

    /// <inheritdoc />
    public IEnumerable<string> GetQueueNames()
    {
        var db = _database.Value;
        if (db == null)
        {
            return Enumerable.Empty<string>();
        }

        try
        {
            return db.GetCollectionNames()
                .Where(name => name.StartsWith("queue_"))
                .Select(name => name.Substring("queue_".Length).Replace("_", "."))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get queue names");
            return Enumerable.Empty<string>();
        }
    }

    /// <inheritdoc />
    public Task<bool> DeleteQueueAsync(string queueName)
    {
        if (string.IsNullOrWhiteSpace(queueName))
        {
            return Task.FromResult(false);
        }

        var db = _database.Value;
        if (db == null)
        {
            return Task.FromResult(false);
        }

        try
        {
            var collectionName = $"queue_{queueName.Replace(".", "_")}";


            var keysToRemove = _queues.Keys.Where(k => k.StartsWith(collectionName + "_")).ToList();
            foreach (var key in keysToRemove)
            {
                _queues.TryRemove(key, out _);
            }


            var result = db.DropCollection(collectionName);
            _logger?.LogDebug("Deleted queue {QueueName}: {Result}", queueName, result);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to delete queue {QueueName}", queueName);
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _queues.Clear();

        if (_database.IsValueCreated && _database.Value != null)
        {
            _database.Value.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}
