using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Queue;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Mythetech.Framework.Desktop.Storage.Sqlite;

public class SqliteQueue<T> : IQueue<T> where T : class
{
    private readonly string _connectionString;
    private readonly string _tableName;
    private readonly ILogger? _logger;
    private readonly object _lock = new();

    public SqliteQueue(string connectionString, string tableName, ILogger? logger = null)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
        _logger = logger;

        EnsureTable();
    }

    private void EnsureTable()
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"""
                CREATE TABLE IF NOT EXISTS [{_tableName}] (
                    id TEXT PRIMARY KEY,
                    item_json TEXT NOT NULL,
                    status INTEGER NOT NULL DEFAULT 0,
                    created_at TEXT NOT NULL,
                    processed_at TEXT,
                    retry_count INTEGER NOT NULL DEFAULT 0,
                    failure_reason TEXT
                )
                """;
            cmd.ExecuteNonQuery();

            using var idxCmd = connection.CreateCommand();
            idxCmd.CommandText = $"CREATE INDEX IF NOT EXISTS [idx_{_tableName}_status] ON [{_tableName}](status, created_at)";
            idxCmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to ensure table for queue {TableName}", _tableName);
        }
    }

    /// <inheritdoc />
    public Task<string> EnqueueAsync(T item, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var id = Guid.NewGuid().ToString("N");

            lock (_lock)
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = $"INSERT INTO [{_tableName}] (id, item_json, status, created_at, retry_count) VALUES (@id, @json, @status, @created, 0)";
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@json", JsonSerializer.Serialize(item));
                cmd.Parameters.AddWithValue("@status", (int)QueueEntryStatus.Pending);
                cmd.Parameters.AddWithValue("@created", DateTime.UtcNow.ToString("O"));
                cmd.ExecuteNonQuery();
            }

            _logger?.LogDebug("Enqueued item {Id} to queue {QueueName}", id, _tableName);
            return Task.FromResult(id);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to enqueue item to queue {QueueName}", _tableName);
            throw;
        }
    }

    /// <inheritdoc />
    public Task<QueueEntry<T>?> DequeueAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            lock (_lock)
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();

                using var selectCmd = connection.CreateCommand();
                selectCmd.CommandText = $"SELECT id, item_json, status, created_at, processed_at, retry_count, failure_reason FROM [{_tableName}] WHERE status = @status ORDER BY created_at ASC LIMIT 1";
                selectCmd.Parameters.AddWithValue("@status", (int)QueueEntryStatus.Pending);

                using var reader = selectCmd.ExecuteReader();
                if (!reader.Read())
                {
                    return Task.FromResult<QueueEntry<T>?>(null);
                }

                var id = reader.GetString(0);
                var entry = ReadQueueEntry(reader);
                reader.Close();

                using var updateCmd = connection.CreateCommand();
                updateCmd.CommandText = $"UPDATE [{_tableName}] SET status = @status WHERE id = @id";
                updateCmd.Parameters.AddWithValue("@status", (int)QueueEntryStatus.Processing);
                updateCmd.Parameters.AddWithValue("@id", id);
                updateCmd.ExecuteNonQuery();

                entry = entry with { Status = QueueEntryStatus.Processing };

                _logger?.LogDebug("Dequeued item {Id} from queue {QueueName}", id, _tableName);
                return Task.FromResult<QueueEntry<T>?>(entry);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to dequeue from queue {QueueName}", _tableName);
            throw;
        }
    }

    /// <inheritdoc />
    public Task<QueueEntry<T>?> PeekAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT id, item_json, status, created_at, processed_at, retry_count, failure_reason FROM [{_tableName}] WHERE status = @status ORDER BY created_at ASC LIMIT 1";
            cmd.Parameters.AddWithValue("@status", (int)QueueEntryStatus.Pending);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                return Task.FromResult<QueueEntry<T>?>(null);
            }

            return Task.FromResult<QueueEntry<T>?>(ReadQueueEntry(reader));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to peek queue {QueueName}", _tableName);
            throw;
        }
    }

    /// <inheritdoc />
    public Task CompleteAsync(string entryId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            lock (_lock)
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = $"UPDATE [{_tableName}] SET status = @status, processed_at = @processed WHERE id = @id";
                cmd.Parameters.AddWithValue("@status", (int)QueueEntryStatus.Completed);
                cmd.Parameters.AddWithValue("@processed", DateTime.UtcNow.ToString("O"));
                cmd.Parameters.AddWithValue("@id", entryId);
                var rows = cmd.ExecuteNonQuery();

                if (rows == 0)
                {
                    _logger?.LogWarning("Attempted to complete non-existent entry {Id} in queue {QueueName}", entryId, _tableName);
                }
                else
                {
                    _logger?.LogDebug("Completed entry {Id} in queue {QueueName}", entryId, _tableName);
                }
            }

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to complete entry {Id} in queue {QueueName}", entryId, _tableName);
            throw;
        }
    }

    /// <inheritdoc />
    public Task FailAsync(string entryId, string? reason = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            lock (_lock)
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = $"UPDATE [{_tableName}] SET status = @status, processed_at = @processed, failure_reason = @reason, retry_count = retry_count + 1 WHERE id = @id";
                cmd.Parameters.AddWithValue("@status", (int)QueueEntryStatus.Failed);
                cmd.Parameters.AddWithValue("@processed", DateTime.UtcNow.ToString("O"));
                cmd.Parameters.AddWithValue("@reason", (object?)reason ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@id", entryId);
                var rows = cmd.ExecuteNonQuery();

                if (rows == 0)
                {
                    _logger?.LogWarning("Attempted to fail non-existent entry {Id} in queue {QueueName}", entryId, _tableName);
                }
                else
                {
                    _logger?.LogDebug("Failed entry {Id} in queue {QueueName}: {Reason}", entryId, _tableName, reason);
                }
            }

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to mark entry {Id} as failed in queue {QueueName}", entryId, _tableName);
            throw;
        }
    }

    /// <inheritdoc />
    public Task<int> GetPendingCountAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(1) FROM [{_tableName}] WHERE status = @status";
            cmd.Parameters.AddWithValue("@status", (int)QueueEntryStatus.Pending);
            var count = (long)cmd.ExecuteScalar()!;
            return Task.FromResult((int)count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get pending count for queue {QueueName}", _tableName);
            throw;
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<QueueEntry<T>>> GetFailedAsync(int limit = 100, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT id, item_json, status, created_at, processed_at, retry_count, failure_reason FROM [{_tableName}] WHERE status = @status ORDER BY processed_at DESC LIMIT @limit";
            cmd.Parameters.AddWithValue("@status", (int)QueueEntryStatus.Failed);
            cmd.Parameters.AddWithValue("@limit", limit);

            var entries = new List<QueueEntry<T>>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                entries.Add(ReadQueueEntry(reader));
            }

            return Task.FromResult<IReadOnlyList<QueueEntry<T>>>(entries);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get failed entries from queue {QueueName}", _tableName);
            throw;
        }
    }

    /// <inheritdoc />
    public Task RetryAsync(string entryId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            lock (_lock)
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = $"UPDATE [{_tableName}] SET status = @status, processed_at = NULL, failure_reason = NULL WHERE id = @id";
                cmd.Parameters.AddWithValue("@status", (int)QueueEntryStatus.Pending);
                cmd.Parameters.AddWithValue("@id", entryId);
                var rows = cmd.ExecuteNonQuery();

                if (rows == 0)
                {
                    _logger?.LogWarning("Attempted to retry non-existent entry {Id} in queue {QueueName}", entryId, _tableName);
                }
                else
                {
                    _logger?.LogDebug("Retrying entry {Id} in queue {QueueName}", entryId, _tableName);
                }
            }

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to retry entry {Id} in queue {QueueName}", entryId, _tableName);
            throw;
        }
    }

    /// <inheritdoc />
    public Task<int> PurgeCompletedAsync(DateTime olderThan, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            lock (_lock)
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = $"DELETE FROM [{_tableName}] WHERE status = @status AND processed_at IS NOT NULL AND processed_at < @olderThan";
                cmd.Parameters.AddWithValue("@status", (int)QueueEntryStatus.Completed);
                cmd.Parameters.AddWithValue("@olderThan", olderThan.ToString("O"));
                var count = cmd.ExecuteNonQuery();

                _logger?.LogDebug("Purged {Count} completed entries from queue {QueueName}", count, _tableName);
                return Task.FromResult(count);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to purge completed entries from queue {QueueName}", _tableName);
            throw;
        }
    }

    private QueueEntry<T> ReadQueueEntry(SqliteDataReader reader)
    {
        var item = JsonSerializer.Deserialize<T>(reader.GetString(1))!;
        var processedAt = reader.IsDBNull(4) ? (DateTime?)null : DateTime.Parse(reader.GetString(4));

        return new QueueEntry<T>
        {
            Id = reader.GetString(0),
            Item = item,
            Status = (QueueEntryStatus)reader.GetInt32(2),
            CreatedAt = DateTime.Parse(reader.GetString(3)),
            ProcessedAt = processedAt,
            RetryCount = reader.GetInt32(5),
            FailureReason = reader.IsDBNull(6) ? null : reader.GetString(6)
        };
    }
}
