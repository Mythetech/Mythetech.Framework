using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Queue;
using SqliteWasmBlazor;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Mythetech.Framework.WebAssembly.Storage.Sqlite;

public class SqliteQueue<T> : IQueue<T> where T : class
{
    private readonly string _connectionString;
    private readonly string _tableName;
    private readonly ILogger? _logger;

    public SqliteQueue(string connectionString, string tableName, ILogger? logger = null)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
        _logger = logger;
    }

    public async Task EnsureTableAsync()
    {
        try
        {
            await using var connection = new SqliteWasmConnection(_connectionString);
            await connection.OpenAsync();

            await using var cmd = connection.CreateCommand();
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
            await cmd.ExecuteNonQueryAsync();

            await using var idxCmd = connection.CreateCommand();
            idxCmd.CommandText = $"CREATE INDEX IF NOT EXISTS [idx_{_tableName}_status] ON [{_tableName}](status, created_at)";
            await idxCmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to ensure table for queue {TableName}", _tableName);
        }
    }

    /// <inheritdoc />
    public async Task<string> EnqueueAsync(T item, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var id = Guid.NewGuid().ToString("N");

        await using var connection = new SqliteWasmConnection(_connectionString);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"INSERT INTO [{_tableName}] (id, item_json, status, created_at, retry_count) VALUES ($id, $json, $status, $created, 0)";
        cmd.Parameters.Add(new SqliteWasmParameter("$id", id));
        cmd.Parameters.Add(new SqliteWasmParameter("$json", JsonSerializer.Serialize(item)));
        cmd.Parameters.Add(new SqliteWasmParameter("$status", (int)QueueEntryStatus.Pending));
        cmd.Parameters.Add(new SqliteWasmParameter("$created", DateTime.UtcNow.ToString("O")));
        await cmd.ExecuteNonQueryAsync();

        _logger?.LogDebug("Enqueued item {Id} to queue {QueueName}", id, _tableName);
        return id;
    }

    /// <inheritdoc />
    public async Task<QueueEntry<T>?> DequeueAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await using var connection = new SqliteWasmConnection(_connectionString);
        await connection.OpenAsync();

        await using var selectCmd = connection.CreateCommand();
        selectCmd.CommandText = $"SELECT id, item_json, status, created_at, processed_at, retry_count, failure_reason FROM [{_tableName}] WHERE status = $status ORDER BY created_at ASC LIMIT 1";
        selectCmd.Parameters.Add(new SqliteWasmParameter("$status", (int)QueueEntryStatus.Pending));

        string? id = null;
        QueueEntry<T>? entry = null;

        await using var reader = await selectCmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            id = reader.GetString(0);
            entry = ReadQueueEntry(reader);
        }

        if (id == null || entry == null)
            return null;

        await using var updateCmd = connection.CreateCommand();
        updateCmd.CommandText = $"UPDATE [{_tableName}] SET status = $status WHERE id = $id";
        updateCmd.Parameters.Add(new SqliteWasmParameter("$status", (int)QueueEntryStatus.Processing));
        updateCmd.Parameters.Add(new SqliteWasmParameter("$id", id));
        await updateCmd.ExecuteNonQueryAsync();

        entry = entry with { Status = QueueEntryStatus.Processing };

        _logger?.LogDebug("Dequeued item {Id} from queue {QueueName}", id, _tableName);
        return entry;
    }

    /// <inheritdoc />
    public async Task<QueueEntry<T>?> PeekAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await using var connection = new SqliteWasmConnection(_connectionString);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT id, item_json, status, created_at, processed_at, retry_count, failure_reason FROM [{_tableName}] WHERE status = $status ORDER BY created_at ASC LIMIT 1";
        cmd.Parameters.Add(new SqliteWasmParameter("$status", (int)QueueEntryStatus.Pending));

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return ReadQueueEntry(reader);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task CompleteAsync(string entryId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await using var connection = new SqliteWasmConnection(_connectionString);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"UPDATE [{_tableName}] SET status = $status, processed_at = $processed WHERE id = $id";
        cmd.Parameters.Add(new SqliteWasmParameter("$status", (int)QueueEntryStatus.Completed));
        cmd.Parameters.Add(new SqliteWasmParameter("$processed", DateTime.UtcNow.ToString("O")));
        cmd.Parameters.Add(new SqliteWasmParameter("$id", entryId));
        var rows = await cmd.ExecuteNonQueryAsync();

        if (rows == 0)
            _logger?.LogWarning("Attempted to complete non-existent entry {Id} in queue {QueueName}", entryId, _tableName);
        else
            _logger?.LogDebug("Completed entry {Id} in queue {QueueName}", entryId, _tableName);
    }

    /// <inheritdoc />
    public async Task FailAsync(string entryId, string? reason = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await using var connection = new SqliteWasmConnection(_connectionString);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"UPDATE [{_tableName}] SET status = $status, processed_at = $processed, failure_reason = $reason, retry_count = retry_count + 1 WHERE id = $id";
        cmd.Parameters.Add(new SqliteWasmParameter("$status", (int)QueueEntryStatus.Failed));
        cmd.Parameters.Add(new SqliteWasmParameter("$processed", DateTime.UtcNow.ToString("O")));
        cmd.Parameters.Add(new SqliteWasmParameter("$reason", (object?)reason ?? DBNull.Value));
        cmd.Parameters.Add(new SqliteWasmParameter("$id", entryId));
        var rows = await cmd.ExecuteNonQueryAsync();

        if (rows == 0)
            _logger?.LogWarning("Attempted to fail non-existent entry {Id} in queue {QueueName}", entryId, _tableName);
        else
            _logger?.LogDebug("Failed entry {Id} in queue {QueueName}: {Reason}", entryId, _tableName, reason);
    }

    /// <inheritdoc />
    public async Task<int> GetPendingCountAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await using var connection = new SqliteWasmConnection(_connectionString);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(1) FROM [{_tableName}] WHERE status = $status";
        cmd.Parameters.Add(new SqliteWasmParameter("$status", (int)QueueEntryStatus.Pending));
        var result = await cmd.ExecuteScalarAsync();
        return result is long count ? (int)count : 0;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<QueueEntry<T>>> GetFailedAsync(int limit = 100, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await using var connection = new SqliteWasmConnection(_connectionString);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT id, item_json, status, created_at, processed_at, retry_count, failure_reason FROM [{_tableName}] WHERE status = $status ORDER BY processed_at DESC LIMIT $limit";
        cmd.Parameters.Add(new SqliteWasmParameter("$status", (int)QueueEntryStatus.Failed));
        cmd.Parameters.Add(new SqliteWasmParameter("$limit", limit));

        var entries = new List<QueueEntry<T>>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            entries.Add(ReadQueueEntry(reader));
        }

        return entries;
    }

    /// <inheritdoc />
    public async Task RetryAsync(string entryId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await using var connection = new SqliteWasmConnection(_connectionString);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"UPDATE [{_tableName}] SET status = $status, processed_at = NULL, failure_reason = NULL WHERE id = $id";
        cmd.Parameters.Add(new SqliteWasmParameter("$status", (int)QueueEntryStatus.Pending));
        cmd.Parameters.Add(new SqliteWasmParameter("$id", entryId));
        var rows = await cmd.ExecuteNonQueryAsync();

        if (rows == 0)
            _logger?.LogWarning("Attempted to retry non-existent entry {Id} in queue {QueueName}", entryId, _tableName);
        else
            _logger?.LogDebug("Retrying entry {Id} in queue {QueueName}", entryId, _tableName);
    }

    /// <inheritdoc />
    public async Task<int> PurgeCompletedAsync(DateTime olderThan, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await using var connection = new SqliteWasmConnection(_connectionString);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"DELETE FROM [{_tableName}] WHERE status = $status AND processed_at IS NOT NULL AND processed_at < $olderThan";
        cmd.Parameters.Add(new SqliteWasmParameter("$status", (int)QueueEntryStatus.Completed));
        cmd.Parameters.Add(new SqliteWasmParameter("$olderThan", olderThan.ToString("O")));
        var count = await cmd.ExecuteNonQueryAsync();

        _logger?.LogDebug("Purged {Count} completed entries from queue {QueueName}", count, _tableName);
        return count;
    }

    private static QueueEntry<T> ReadQueueEntry(System.Data.Common.DbDataReader reader)
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
