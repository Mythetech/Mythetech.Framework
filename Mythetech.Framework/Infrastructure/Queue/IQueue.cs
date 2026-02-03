namespace Mythetech.Framework.Infrastructure.Queue;

/// <summary>
/// A persistent queue for storing items that can be processed asynchronously.
/// Supports retry semantics and failure tracking.
/// </summary>
/// <typeparam name="T">The type of items in the queue.</typeparam>
public interface IQueue<T> where T : class
{
    /// <summary>
    /// Add an item to the queue.
    /// </summary>
    /// <param name="item">The item to enqueue.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The ID of the created queue entry.</returns>
    Task<string> EnqueueAsync(T item, CancellationToken ct = default);

    /// <summary>
    /// Get and lock the next pending item for processing.
    /// The entry's status is changed to Processing.
    /// Call CompleteAsync or FailAsync when done.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The next pending entry, or null if queue is empty.</returns>
    Task<QueueEntry<T>?> DequeueAsync(CancellationToken ct = default);

    /// <summary>
    /// View the next pending item without removing it from the queue.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The next pending entry, or null if queue is empty.</returns>
    Task<QueueEntry<T>?> PeekAsync(CancellationToken ct = default);

    /// <summary>
    /// Mark an entry as successfully processed.
    /// </summary>
    /// <param name="entryId">The entry ID returned from DequeueAsync.</param>
    /// <param name="ct">Cancellation token.</param>
    Task CompleteAsync(string entryId, CancellationToken ct = default);

    /// <summary>
    /// Mark an entry as failed.
    /// </summary>
    /// <param name="entryId">The entry ID returned from DequeueAsync.</param>
    /// <param name="reason">Optional reason for the failure.</param>
    /// <param name="ct">Cancellation token.</param>
    Task FailAsync(string entryId, string? reason = null, CancellationToken ct = default);

    /// <summary>
    /// Get the count of pending entries.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task<int> GetPendingCountAsync(CancellationToken ct = default);

    /// <summary>
    /// Get failed entries for inspection or retry.
    /// </summary>
    /// <param name="limit">Maximum number of entries to return.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<QueueEntry<T>>> GetFailedAsync(int limit = 100, CancellationToken ct = default);

    /// <summary>
    /// Move a failed entry back to pending status for retry.
    /// Increments the retry count.
    /// </summary>
    /// <param name="entryId">The entry ID to retry.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RetryAsync(string entryId, CancellationToken ct = default);

    /// <summary>
    /// Remove completed entries older than the specified age.
    /// </summary>
    /// <param name="olderThan">Remove entries completed before this time.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Number of entries removed.</returns>
    Task<int> PurgeCompletedAsync(DateTime olderThan, CancellationToken ct = default);
}
