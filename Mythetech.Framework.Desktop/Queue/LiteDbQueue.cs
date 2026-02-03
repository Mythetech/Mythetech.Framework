using LiteDB;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Queue;

namespace Mythetech.Framework.Desktop.Queue;

/// <summary>
/// LiteDB-based queue implementation for Desktop applications.
/// Provides persistent queue storage with retry semantics.
/// </summary>
/// <typeparam name="T">The type of items in the queue.</typeparam>
public class LiteDbQueue<T> : IQueue<T> where T : class
{
    private readonly ILiteDatabase _database;
    private readonly string _collectionName;
    private readonly ILogger? _logger;
    private readonly object _lock = new();

    /// <summary>
    /// Creates a new LiteDB queue instance.
    /// </summary>
    /// <param name="database">The LiteDB database instance.</param>
    /// <param name="collectionName">Name of the collection to use for this queue.</param>
    /// <param name="logger">Optional logger for error reporting.</param>
    public LiteDbQueue(ILiteDatabase database, string collectionName, ILogger? logger = null)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _collectionName = collectionName ?? throw new ArgumentNullException(nameof(collectionName));
        _logger = logger;

        EnsureIndexes();
    }

    private void EnsureIndexes()
    {
        try
        {
            var collection = GetCollection();

            collection.EnsureIndex(x => x.Status);
            collection.EnsureIndex(x => x.CreatedAt);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to ensure indexes for queue {CollectionName}", _collectionName);
        }
    }

    private ILiteCollection<LiteDbQueueDocument> GetCollection()
    {
        return _database.GetCollection<LiteDbQueueDocument>(_collectionName);
    }

    /// <inheritdoc />
    public Task<string> EnqueueAsync(T item, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var id = Guid.NewGuid().ToString("N");
            var document = new LiteDbQueueDocument
            {
                Id = id,
                ItemJson = JsonSerializer.Serialize(item),
                Status = QueueEntryStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                RetryCount = 0
            };

            lock (_lock)
            {
                GetCollection().Insert(document);
            }

            _logger?.LogDebug("Enqueued item {Id} to queue {QueueName}", id, _collectionName);
            return Task.FromResult(id);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to enqueue item to queue {QueueName}", _collectionName);
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
                var collection = GetCollection();

                var document = collection
                    .Find(x => x.Status == QueueEntryStatus.Pending)
                    .OrderBy(x => x.CreatedAt)
                    .FirstOrDefault();

                if (document == null)
                {
                    return Task.FromResult<QueueEntry<T>?>(null);
                }

                document.Status = QueueEntryStatus.Processing;
                collection.Update(document);

                var entry = ToQueueEntry(document);
                _logger?.LogDebug("Dequeued item {Id} from queue {QueueName}", document.Id, _collectionName);
                return Task.FromResult<QueueEntry<T>?>(entry);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to dequeue from queue {QueueName}", _collectionName);
            throw;
        }
    }

    /// <inheritdoc />
    public Task<QueueEntry<T>?> PeekAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var collection = GetCollection();

            var document = collection
                .Find(x => x.Status == QueueEntryStatus.Pending)
                .OrderBy(x => x.CreatedAt)
                .FirstOrDefault();

            if (document == null)
            {
                return Task.FromResult<QueueEntry<T>?>(null);
            }

            return Task.FromResult<QueueEntry<T>?>(ToQueueEntry(document));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to peek queue {QueueName}", _collectionName);
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
                var collection = GetCollection();
                var document = collection.FindById(entryId);

                if (document == null)
                {
                    _logger?.LogWarning("Attempted to complete non-existent entry {Id} in queue {QueueName}", entryId, _collectionName);
                    return Task.CompletedTask;
                }

                document.Status = QueueEntryStatus.Completed;
                document.ProcessedAt = DateTime.UtcNow;
                collection.Update(document);

                _logger?.LogDebug("Completed entry {Id} in queue {QueueName}", entryId, _collectionName);
            }

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to complete entry {Id} in queue {QueueName}", entryId, _collectionName);
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
                var collection = GetCollection();
                var document = collection.FindById(entryId);

                if (document == null)
                {
                    _logger?.LogWarning("Attempted to fail non-existent entry {Id} in queue {QueueName}", entryId, _collectionName);
                    return Task.CompletedTask;
                }

                document.Status = QueueEntryStatus.Failed;
                document.ProcessedAt = DateTime.UtcNow;
                document.FailureReason = reason;
                document.RetryCount++;
                collection.Update(document);

                _logger?.LogDebug("Failed entry {Id} in queue {QueueName}: {Reason}", entryId, _collectionName, reason);
            }

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to mark entry {Id} as failed in queue {QueueName}", entryId, _collectionName);
            throw;
        }
    }

    /// <inheritdoc />
    public Task<int> GetPendingCountAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var count = GetCollection().Count(x => x.Status == QueueEntryStatus.Pending);
            return Task.FromResult(count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get pending count for queue {QueueName}", _collectionName);
            throw;
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<QueueEntry<T>>> GetFailedAsync(int limit = 100, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var documents = GetCollection()
                .Find(x => x.Status == QueueEntryStatus.Failed)
                .OrderByDescending(x => x.ProcessedAt)
                .Take(limit)
                .ToList();

            var entries = documents.Select(ToQueueEntry).ToList();
            return Task.FromResult<IReadOnlyList<QueueEntry<T>>>(entries);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get failed entries from queue {QueueName}", _collectionName);
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
                var collection = GetCollection();
                var document = collection.FindById(entryId);

                if (document == null)
                {
                    _logger?.LogWarning("Attempted to retry non-existent entry {Id} in queue {QueueName}", entryId, _collectionName);
                    return Task.CompletedTask;
                }

                document.Status = QueueEntryStatus.Pending;
                document.ProcessedAt = null;
                document.FailureReason = null;
                collection.Update(document);

                _logger?.LogDebug("Retrying entry {Id} in queue {QueueName} (attempt {RetryCount})", entryId, _collectionName, document.RetryCount + 1);
            }

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to retry entry {Id} in queue {QueueName}", entryId, _collectionName);
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
                var collection = GetCollection();
                var count = collection.DeleteMany(x =>
                    x.Status == QueueEntryStatus.Completed &&
                    x.ProcessedAt != null &&
                    x.ProcessedAt < olderThan);

                _logger?.LogDebug("Purged {Count} completed entries from queue {QueueName}", count, _collectionName);
                return Task.FromResult(count);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to purge completed entries from queue {QueueName}", _collectionName);
            throw;
        }
    }

    private QueueEntry<T> ToQueueEntry(LiteDbQueueDocument document)
    {
        var item = JsonSerializer.Deserialize<T>(document.ItemJson)!;

        return new QueueEntry<T>
        {
            Id = document.Id,
            Item = item,
            Status = document.Status,
            CreatedAt = document.CreatedAt,
            ProcessedAt = document.ProcessedAt,
            RetryCount = document.RetryCount,
            FailureReason = document.FailureReason
        };
    }
}
