using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Queue;
using SqliteWasmBlazor;

namespace Mythetech.Framework.WebAssembly.Storage.Sqlite;

public class SqliteQueueFactory : IQueueFactory
{
    private readonly string _connectionString;
    private readonly ConcurrentDictionary<string, object> _queues = new();
    private readonly ILoggerFactory? _loggerFactory;
    private readonly ILogger<SqliteQueueFactory>? _logger;
    private bool _initialized;

    public SqliteQueueFactory(string databaseName, ILoggerFactory? loggerFactory = null)
    {
        _connectionString = $"Data Source={databaseName}";
        _loggerFactory = loggerFactory;
        _logger = loggerFactory?.CreateLogger<SqliteQueueFactory>();
    }

    private async Task EnsureInitializedAsync()
    {
        if (_initialized) return;

        try
        {
            await using var connection = new SqliteWasmConnection(_connectionString);
            await connection.OpenAsync();
            _initialized = true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize queue storage. Queue persistence will be unavailable.");
        }
    }

    /// <inheritdoc />
    public IQueue<T>? GetQueue<T>(string queueName) where T : class
    {
        if (string.IsNullOrWhiteSpace(queueName))
        {
            throw new ArgumentException("Queue name cannot be empty", nameof(queueName));
        }

        if (!_initialized)
        {
            _logger?.LogDebug("Queue storage not yet initialized, returning null for queue {QueueName}", queueName);
            return null;
        }

        var tableName = $"queue_{queueName.Replace(".", "_")}";
        var cacheKey = $"{tableName}_{typeof(T).FullName}";

        var queue = _queues.GetOrAdd(cacheKey, _ =>
        {
            var logger = _loggerFactory?.CreateLogger<SqliteQueue<T>>();
            return new SqliteQueue<T>(_connectionString, tableName, logger);
        });

        return (IQueue<T>)queue;
    }

    public async Task<IQueue<T>?> GetQueueAsync<T>(string queueName) where T : class
    {
        await EnsureInitializedAsync();
        var queue = GetQueue<T>(queueName);
        if (queue is SqliteQueue<T> sqliteQueue)
        {
            await sqliteQueue.EnsureTableAsync();
        }
        return queue;
    }

    /// <inheritdoc />
    public IEnumerable<string> GetQueueNames()
    {
        if (!_initialized)
            return Enumerable.Empty<string>();

        try
        {
            return GetQueueNamesAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get queue names");
            return Enumerable.Empty<string>();
        }
    }

    public async Task<IEnumerable<string>> GetQueueNamesAsync()
    {
        await EnsureInitializedAsync();
        if (!_initialized) return Enumerable.Empty<string>();

        try
        {
            await using var connection = new SqliteWasmConnection(_connectionString);
            await connection.OpenAsync();
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name LIKE 'queue_%'";

            var names = new List<string>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
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
        return DeleteQueueInternalAsync(queueName);
    }

    private async Task<bool> DeleteQueueInternalAsync(string queueName)
    {
        if (string.IsNullOrWhiteSpace(queueName))
            return false;

        await EnsureInitializedAsync();
        if (!_initialized) return false;

        try
        {
            var tableName = $"queue_{queueName.Replace(".", "_")}";

            var keysToRemove = _queues.Keys.Where(k => k.StartsWith(tableName + "_")).ToList();
            foreach (var key in keysToRemove)
            {
                _queues.TryRemove(key, out _);
            }

            await using var connection = new SqliteWasmConnection(_connectionString);
            await connection.OpenAsync();

            await using var checkCmd = connection.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(1) FROM sqlite_master WHERE type='table' AND name=$name";
            checkCmd.Parameters.Add(new SqliteWasmParameter("$name", tableName));
            var result = await checkCmd.ExecuteScalarAsync();
            var exists = result is long count && count > 0;

            if (!exists) return false;

            await using var dropCmd = connection.CreateCommand();
            dropCmd.CommandText = $"DROP TABLE [{tableName}]";
            await dropCmd.ExecuteNonQueryAsync();

            _logger?.LogDebug("Deleted queue {QueueName}", queueName);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to delete queue {QueueName}", queueName);
            return false;
        }
    }
}
