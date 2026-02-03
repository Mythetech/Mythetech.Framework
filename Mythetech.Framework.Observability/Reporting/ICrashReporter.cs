namespace Mythetech.Framework.Observability.Reporting;

/// <summary>
/// Reports crashes and unhandled exceptions.
/// Implementations can send to various backends (Platform API, Sentry, etc.).
/// </summary>
public interface ICrashReporter
{
    /// <summary>
    /// Reports an exception.
    /// </summary>
    /// <param name="exception">The exception to report.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The report ID if successfully queued.</returns>
    Task<string?> ReportAsync(Exception exception, CancellationToken ct = default);

    /// <summary>
    /// Reports an exception with additional metadata.
    /// </summary>
    /// <param name="exception">The exception to report.</param>
    /// <param name="metadata">Additional metadata to include.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The report ID if successfully queued.</returns>
    Task<string?> ReportAsync(Exception exception, IDictionary<string, object>? metadata, CancellationToken ct = default);

    /// <summary>
    /// Reports a fatal/unhandled exception.
    /// </summary>
    /// <param name="exception">The exception to report.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The report ID if successfully queued.</returns>
    Task<string?> ReportFatalAsync(Exception exception, CancellationToken ct = default);
}
