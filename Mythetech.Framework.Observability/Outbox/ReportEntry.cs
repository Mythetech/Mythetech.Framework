namespace Mythetech.Framework.Observability.Outbox;

/// <summary>
/// Represents a report entry in the outbox queue.
/// This is a union type that can hold either a crash or bug report.
/// </summary>
public record ReportEntry
{
    /// <summary>
    /// Gets the type of report.
    /// </summary>
    public required ReportType Type { get; init; }

    /// <summary>
    /// Gets the serialized report payload (JSON).
    /// </summary>
    public required string PayloadJson { get; init; }

    /// <summary>
    /// Gets the timestamp when the report was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the report ID.
    /// </summary>
    public required string ReportId { get; init; }
}
