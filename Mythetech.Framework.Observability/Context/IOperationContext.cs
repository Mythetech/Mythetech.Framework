namespace Mythetech.Framework.Observability.Context;

/// <summary>
/// Provides access to the current operation context including correlation ID.
/// </summary>
public interface IOperationContext
{
    /// <summary>
    /// Gets the current correlation ID, or null if not in an operation scope.
    /// </summary>
    string? CorrelationId { get; }

    /// <summary>
    /// Gets the current operation name, or null if not in an operation scope.
    /// </summary>
    string? OperationName { get; }

    /// <summary>
    /// Gets custom properties associated with the current operation.
    /// </summary>
    IReadOnlyDictionary<string, object> Properties { get; }

    /// <summary>
    /// Begins a new operation scope with an optional correlation ID.
    /// </summary>
    /// <param name="operationName">Name of the operation.</param>
    /// <param name="correlationId">Optional correlation ID. If not provided, a new one is generated.</param>
    /// <returns>A disposable scope that restores the previous context when disposed.</returns>
    IDisposable BeginScope(string operationName, string? correlationId = null);

    /// <summary>
    /// Sets a property on the current operation context.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="value">The property value.</param>
    void SetProperty(string key, object value);
}
