namespace Mythetech.Framework.Infrastructure.Queue;

/// <summary>
/// Represents an entry in a queue with metadata about its processing state.
/// </summary>
/// <typeparam name="T">The type of item in the queue.</typeparam>
public record QueueEntry<T> where T : class
{
    /// <summary>
    /// Unique identifier for this entry.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The queued item.
    /// </summary>
    public required T Item { get; init; }

    /// <summary>
    /// Current status of this entry.
    /// </summary>
    public required QueueEntryStatus Status { get; init; }

    /// <summary>
    /// When the entry was added to the queue.
    /// </summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// When the entry was last processed (completed or failed).
    /// </summary>
    public DateTime? ProcessedAt { get; init; }

    /// <summary>
    /// Number of times processing has been attempted.
    /// </summary>
    public int RetryCount { get; init; }

    /// <summary>
    /// Reason for failure, if status is Failed.
    /// </summary>
    public string? FailureReason { get; init; }
}
