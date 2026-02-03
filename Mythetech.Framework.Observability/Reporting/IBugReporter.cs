namespace Mythetech.Framework.Observability.Reporting;

/// <summary>
/// Reports user-initiated bug reports.
/// Implementations can send to various backends (Platform API, email, etc.).
/// </summary>
public interface IBugReporter
{
    /// <summary>
    /// Reports a bug.
    /// </summary>
    /// <param name="request">The bug report request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The report ID if successfully queued.</returns>
    Task<string?> ReportAsync(BugReportRequest request, CancellationToken ct = default);
}
