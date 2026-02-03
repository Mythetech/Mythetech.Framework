namespace Mythetech.Framework.Infrastructure.Queue;

/// <summary>
/// Status of a queue entry.
/// </summary>
public enum QueueEntryStatus
{
    /// <summary>
    /// Entry is waiting to be processed.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Entry is currently being processed.
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Entry was processed successfully.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Entry processing failed.
    /// </summary>
    Failed = 3
}
