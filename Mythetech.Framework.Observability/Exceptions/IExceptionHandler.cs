namespace Mythetech.Framework.Observability.Exceptions;

/// <summary>
/// Centralized exception handler that notifies observers and optionally logs exceptions.
/// </summary>
public interface IExceptionHandler
{
    /// <summary>
    /// Handles an exception by notifying all registered observers.
    /// </summary>
    /// <param name="exception">The exception to handle.</param>
    /// <param name="operationName">Optional name of the operation that threw the exception.</param>
    /// <param name="properties">Optional additional properties.</param>
    /// <param name="isHandled">Whether this exception is being handled (vs unhandled/fatal).</param>
    /// <param name="ct">Cancellation token.</param>
    Task HandleAsync(
        Exception exception,
        string? operationName = null,
        IReadOnlyDictionary<string, object>? properties = null,
        bool isHandled = true,
        CancellationToken ct = default);

    /// <summary>
    /// Handles a fatal/unhandled exception.
    /// </summary>
    /// <param name="exception">The exception to handle.</param>
    /// <param name="ct">Cancellation token.</param>
    Task HandleFatalAsync(Exception exception, CancellationToken ct = default);
}
