namespace Mythetech.Framework.Observability.Reporting;

/// <summary>
/// Represents an automatic crash/exception report.
/// </summary>
public record CrashReport
{
    /// <summary>
    /// Gets the unique identifier for this report.
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Gets the timestamp when the crash occurred.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the exception type name.
    /// </summary>
    public required string ExceptionType { get; init; }

    /// <summary>
    /// Gets the exception message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the full stack trace.
    /// </summary>
    public string? StackTrace { get; init; }

    /// <summary>
    /// Gets inner exception details if present.
    /// </summary>
    public CrashReport? InnerException { get; init; }

    /// <summary>
    /// Gets whether this was a fatal/unhandled exception.
    /// </summary>
    public bool IsFatal { get; init; }

    /// <summary>
    /// Gets the diagnostic context collected at crash time.
    /// </summary>
    public DiagnosticContext? Diagnostics { get; init; }

    /// <summary>
    /// Gets optional additional metadata.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Creates a CrashReport from an exception.
    /// </summary>
    /// <param name="exception">The exception to report.</param>
    /// <param name="isFatal">Whether this was a fatal exception.</param>
    /// <param name="diagnostics">Optional diagnostic context.</param>
    /// <param name="metadata">Optional additional metadata.</param>
    /// <returns>A crash report.</returns>
    public static CrashReport FromException(
        Exception exception,
        bool isFatal = false,
        DiagnosticContext? diagnostics = null,
        IReadOnlyDictionary<string, object>? metadata = null)
    {
        return new CrashReport
        {
            ExceptionType = exception.GetType().FullName ?? exception.GetType().Name,
            Message = exception.Message,
            StackTrace = exception.StackTrace,
            InnerException = exception.InnerException != null
                ? FromException(exception.InnerException, isFatal: false)
                : null,
            IsFatal = isFatal,
            Diagnostics = diagnostics,
            Metadata = metadata
        };
    }
}
