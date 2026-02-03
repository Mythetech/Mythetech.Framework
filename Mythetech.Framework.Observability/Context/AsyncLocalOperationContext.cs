namespace Mythetech.Framework.Observability.Context;

/// <summary>
/// AsyncLocal-based implementation of operation context.
/// Maintains context across async/await boundaries.
/// </summary>
public class AsyncLocalOperationContext : IOperationContext
{
    private static readonly AsyncLocal<OperationScope?> _current = new();

    /// <inheritdoc />
    public string? CorrelationId => _current.Value?.CorrelationId;

    /// <inheritdoc />
    public string? OperationName => _current.Value?.OperationName;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> Properties =>
        _current.Value?.Properties ?? new Dictionary<string, object>();

    /// <inheritdoc />
    public IDisposable BeginScope(string operationName, string? correlationId = null)
    {
        var previousScope = _current.Value;
        var newCorrelationId = correlationId ?? previousScope?.CorrelationId ?? GenerateCorrelationId();

        var newScope = new OperationScope(
            operationName,
            newCorrelationId,
            new Dictionary<string, object>(previousScope?.Properties ?? new Dictionary<string, object>()),
            previousScope);

        _current.Value = newScope;

        return new ScopeDisposer(() => _current.Value = previousScope);
    }

    /// <inheritdoc />
    public void SetProperty(string key, object value)
    {
        if (_current.Value is { } scope)
        {
            scope.Properties[key] = value;
        }
    }

    private static string GenerateCorrelationId()
        => Guid.NewGuid().ToString("N")[..16];

    private sealed class OperationScope
    {
        public string OperationName { get; }
        public string CorrelationId { get; }
        public Dictionary<string, object> Properties { get; }
        public OperationScope? Parent { get; }

        public OperationScope(string operationName, string correlationId, Dictionary<string, object> properties, OperationScope? parent)
        {
            OperationName = operationName;
            CorrelationId = correlationId;
            Properties = properties;
            Parent = parent;
        }
    }

    private sealed class ScopeDisposer : IDisposable
    {
        private Action? _onDispose;

        public ScopeDisposer(Action onDispose)
        {
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            _onDispose?.Invoke();
            _onDispose = null;
        }
    }
}
