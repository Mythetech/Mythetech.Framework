namespace Mythetech.Framework.Observability.Exceptions;

/// <summary>
/// Observer that receives notifications about exceptions.
/// </summary>
public interface IExceptionObserver
{
    /// <summary>
    /// Called when an exception is observed.
    /// </summary>
    /// <param name="context">Context about the exception.</param>
    /// <param name="ct">Cancellation token.</param>
    Task OnExceptionAsync(ExceptionContext context, CancellationToken ct = default);
}

/// <summary>
/// Context information about an observed exception.
/// </summary>
public record ExceptionContext
{
    /// <summary>
    /// Gets the exception that was observed.
    /// </summary>
    public required Exception Exception { get; init; }

    /// <summary>
    /// Gets the timestamp when the exception was observed.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the correlation ID if available.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Gets the operation name if available.
    /// </summary>
    public string? OperationName { get; init; }

    /// <summary>
    /// Gets additional properties associated with the exception.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Properties { get; init; }

    /// <summary>
    /// Gets whether this exception was handled.
    /// </summary>
    public bool IsHandled { get; init; }

    /// <summary>
    /// Gets whether this was a fatal/unhandled exception.
    /// </summary>
    public bool IsFatal { get; init; }
}
