using LiteDB;
using Mythetech.Framework.Infrastructure.Queue;

namespace Mythetech.Framework.Desktop.Storage.LiteDb;

internal class LiteDbQueueDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    public string ItemJson { get; set; } = string.Empty;

    public QueueEntryStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public int RetryCount { get; set; }

    public string? FailureReason { get; set; }
}
