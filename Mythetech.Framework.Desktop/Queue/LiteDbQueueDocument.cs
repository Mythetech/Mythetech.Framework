using LiteDB;
using Mythetech.Framework.Infrastructure.Queue;

namespace Mythetech.Framework.Desktop.Queue;

/// <summary>
/// Document stored in LiteDB for queue entries.
/// </summary>
internal class LiteDbQueueDocument
{
    /// <summary>
    /// Unique identifier for this entry.
    /// </summary>
    [BsonId]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The JSON-serialized item.
    /// </summary>
    public string ItemJson { get; set; } = string.Empty;

    /// <summary>
    /// Current status of this entry.
    /// </summary>
    public QueueEntryStatus Status { get; set; }

    /// <summary>
    /// When the entry was added to the queue.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the entry was last processed (completed or failed).
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Number of times processing has been attempted.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Reason for failure, if status is Failed.
    /// </summary>
    public string? FailureReason { get; set; }
}
