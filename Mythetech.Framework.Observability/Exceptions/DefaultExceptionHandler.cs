using Microsoft.Extensions.Logging;
using Mythetech.Framework.Observability.Context;
using Mythetech.Framework.Observability.Metrics;

namespace Mythetech.Framework.Observability.Exceptions;

/// <summary>
/// Default exception handler that logs exceptions and notifies observers.
/// </summary>
public class DefaultExceptionHandler : IExceptionHandler
{
    private readonly IEnumerable<IExceptionObserver> _observers;
    private readonly IOperationContext? _operationContext;
    private readonly ILogger<DefaultExceptionHandler>? _logger;
    private readonly ICounter<long>? _exceptionCounter;

    /// <summary>
    /// Creates a new DefaultExceptionHandler.
    /// </summary>
    /// <param name="observers">Registered exception observers.</param>
    /// <param name="operationContext">Optional operation context for correlation.</param>
    /// <param name="meterFactory">Optional meter factory for metrics.</param>
    /// <param name="logger">Optional logger.</param>
    public DefaultExceptionHandler(
        IEnumerable<IExceptionObserver> observers,
        IOperationContext? operationContext = null,
        IMeterFactory? meterFactory = null,
        ILogger<DefaultExceptionHandler>? logger = null)
    {
        _observers = observers;
        _operationContext = operationContext;
        _logger = logger;
        _exceptionCounter = meterFactory?.CreateCounter<long>(
            "mythetech.exceptions.count",
            unit: "{exceptions}",
            description: "Count of exceptions");
    }

    /// <inheritdoc />
    public async Task HandleAsync(
        Exception exception,
        string? operationName = null,
        IReadOnlyDictionary<string, object>? properties = null,
        bool isHandled = true,
        CancellationToken ct = default)
    {
        var context = new ExceptionContext
        {
            Exception = exception,
            CorrelationId = _operationContext?.CorrelationId,
            OperationName = operationName ?? _operationContext?.OperationName,
            Properties = properties,
            IsHandled = isHandled,
            IsFatal = false
        };

        await NotifyObserversAsync(context, ct);

        RecordMetrics(exception, isHandled, isFatal: false);

        if (isHandled)
        {
            _logger?.LogWarning(exception, "Handled exception in {OperationName}", context.OperationName ?? "unknown");
        }
        else
        {
            _logger?.LogError(exception, "Unhandled exception in {OperationName}", context.OperationName ?? "unknown");
        }
    }

    /// <inheritdoc />
    public async Task HandleFatalAsync(Exception exception, CancellationToken ct = default)
    {
        var context = new ExceptionContext
        {
            Exception = exception,
            CorrelationId = _operationContext?.CorrelationId,
            OperationName = _operationContext?.OperationName,
            IsHandled = false,
            IsFatal = true
        };

        await NotifyObserversAsync(context, ct);

        RecordMetrics(exception, isHandled: false, isFatal: true);

        _logger?.LogCritical(exception, "Fatal exception occurred");
    }

    private async Task NotifyObserversAsync(ExceptionContext context, CancellationToken ct)
    {
        foreach (var observer in _observers)
        {
            try
            {
                await observer.OnExceptionAsync(context, ct);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Exception observer {ObserverType} threw an exception", observer.GetType().Name);
            }
        }
    }

    private void RecordMetrics(Exception exception, bool isHandled, bool isFatal)
    {
        _exceptionCounter?.Increment(
            new KeyValuePair<string, object?>("exception.type", exception.GetType().Name),
            new KeyValuePair<string, object?>("exception.handled", isHandled),
            new KeyValuePair<string, object?>("exception.fatal", isFatal));
    }
}
