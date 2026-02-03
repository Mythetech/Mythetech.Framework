using Mythetech.Framework.Observability.Reporting;

namespace Mythetech.Framework.Observability.Outbox;

/// <summary>
/// Outbox for storing reports before they are synced to the backend.
/// Provides reliable storage with retry semantics.
/// </summary>
public interface IReportOutbox
{
    /// <summary>
    /// Enqueues a crash report.
    /// </summary>
    /// <param name="report">The crash report to enqueue.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The queue entry ID.</returns>
    Task<string?> EnqueueCrashReportAsync(CrashReport report, CancellationToken ct = default);

    /// <summary>
    /// Enqueues a bug report.
    /// </summary>
    /// <param name="report">The bug report to enqueue.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The queue entry ID.</returns>
    Task<string?> EnqueueBugReportAsync(BugReport report, CancellationToken ct = default);

    /// <summary>
    /// Gets the count of pending reports.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The count of pending reports.</returns>
    Task<int> GetPendingCountAsync(CancellationToken ct = default);

    /// <summary>
    /// Dequeues the next report for processing.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The next report entry, or null if empty.</returns>
    Task<OutboxEntry?> DequeueAsync(CancellationToken ct = default);

    /// <summary>
    /// Marks an entry as completed (successfully sent).
    /// </summary>
    /// <param name="entryId">The entry ID.</param>
    /// <param name="ct">Cancellation token.</param>
    Task CompleteAsync(string entryId, CancellationToken ct = default);

    /// <summary>
    /// Marks an entry as failed.
    /// </summary>
    /// <param name="entryId">The entry ID.</param>
    /// <param name="reason">The failure reason.</param>
    /// <param name="ct">Cancellation token.</param>
    Task FailAsync(string entryId, string? reason = null, CancellationToken ct = default);

    /// <summary>
    /// Retries a failed entry.
    /// </summary>
    /// <param name="entryId">The entry ID.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RetryAsync(string entryId, CancellationToken ct = default);
}

/// <summary>
/// An entry from the report outbox.
/// </summary>
public record OutboxEntry
{
    /// <summary>
    /// Gets the queue entry ID.
    /// </summary>
    public required string EntryId { get; init; }

    /// <summary>
    /// Gets the report entry data.
    /// </summary>
    public required ReportEntry Report { get; init; }

    /// <summary>
    /// Gets the retry count.
    /// </summary>
    public int RetryCount { get; init; }
}
