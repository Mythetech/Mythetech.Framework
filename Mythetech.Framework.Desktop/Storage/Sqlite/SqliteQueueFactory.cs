using System.Collections.Concurrent;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Queue;

namespace Mythetech.Framework.Desktop.Storage.Sqlite;

public class SqliteQueueFactory : IQueueFactory, IDisposable
{
    private readonly Lazy<string?> _connectionString;
    private readonly ConcurrentDictionary<string, object> _queues = new();
    private readonly ILoggerFactory? _loggerFactory;
    private readonly ILogger<SqliteQueueFactory>? _logger;

    public SqliteQueueFactory(string databasePath, ILoggerFactory? loggerFactory = null)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory?.CreateLogger<SqliteQueueFactory>();

        _connectionString = new Lazy<string?>(() =>
        {
            try
            {
                var connStr = new SqliteConnectionStringBuilder
                {
                    DataSource = databasePath,
                    Mode = SqliteOpenMode.ReadWriteCreate
                }.ToString();

                using var connection = new SqliteConnection(connStr);
                connection.Open();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "PRAGMA journal_mode=WAL";
                cmd.ExecuteNonQuery();

                return connStr;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize queue storage at {DatabasePath}. Queue persistence will be unavailable.", databasePath);
                return null;
            }
        });
    }

    /// <inheritdoc />
    public IQueue<T>? GetQueue<T>(string queueName) where T : class
    {
        if (string.IsNullOrWhiteSpace(queueName))
        {
            throw new ArgumentException("Queue name cannot be empty", nameof(queueName));
        }

        var connStr = _connectionString.Value;
        if (connStr == null)
        {
            _logger?.LogDebug("Queue storage unavailable, returning null for queue {QueueName}", queueName);
            return null;
        }

        var tableName = $"queue_{queueName.Replace(".", "_")}";
        var cacheKey = $"{tableName}_{typeof(T).FullName}";

        var queue = _queues.GetOrAdd(cacheKey, _ =>
        {
            var logger = _loggerFactory?.CreateLogger<SqliteQueue<T>>();
            return new SqliteQueue<T>(connStr, tableName, logger);
        });

        return (IQueue<T>)queue;
    }

    /// <inheritdoc />
    public IEnumerable<string> GetQueueNames()
    {
        var connStr = _connectionString.Value;
        if (connStr == null)
        {
            return Enumerable.Empty<string>();
        }

        try
        {
            using var connection = new SqliteConnection(connStr);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name LIKE 'queue_%'";

            var names = new List<string>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var tableName = reader.GetString(0);
                var queueName = tableName.Substring("queue_".Length).Replace("_", ".");
                names.Add(queueName);
            }

            return names;
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

        var connStr = _connectionString.Value;
        if (connStr == null)
        {
            return Task.FromResult(false);
        }

        try
        {
            var tableName = $"queue_{queueName.Replace(".", "_")}";

            var keysToRemove = _queues.Keys.Where(k => k.StartsWith(tableName + "_")).ToList();
            foreach (var key in keysToRemove)
            {
                _queues.TryRemove(key, out _);
            }

            using var connection = new SqliteConnection(connStr);
            connection.Open();

            using var checkCmd = connection.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(1) FROM sqlite_master WHERE type='table' AND name=@name";
            checkCmd.Parameters.AddWithValue("@name", tableName);
            var exists = (long)checkCmd.ExecuteScalar()! > 0;

            if (!exists)
            {
                return Task.FromResult(false);
            }

            using var dropCmd = connection.CreateCommand();
            dropCmd.CommandText = $"DROP TABLE [{tableName}]";
            dropCmd.ExecuteNonQuery();

            _logger?.LogDebug("Deleted queue {QueueName}", queueName);
            return Task.FromResult(true);
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
        GC.SuppressFinalize(this);
    }
}
