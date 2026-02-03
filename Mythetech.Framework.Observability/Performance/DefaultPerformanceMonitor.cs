using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Observability.Metrics;

namespace Mythetech.Framework.Observability.Performance;

/// <summary>
/// Default performance monitor implementation using histogram metrics.
/// </summary>
public class DefaultPerformanceMonitor : IPerformanceMonitor
{
    private readonly IHistogram<double> _operationDuration;
    private readonly ICounter<long> _operationCount;
    private readonly ICounter<long> _operationErrors;
    private readonly ILogger<DefaultPerformanceMonitor>? _logger;

    /// <summary>
    /// Creates a new DefaultPerformanceMonitor.
    /// </summary>
    /// <param name="meterFactory">Factory for creating metrics.</param>
    /// <param name="logger">Optional logger.</param>
    public DefaultPerformanceMonitor(IMeterFactory meterFactory, ILogger<DefaultPerformanceMonitor>? logger = null)
    {
        _operationDuration = meterFactory.CreateHistogram<double>(
            "mythetech.operation.duration",
            unit: "ms",
            description: "Duration of operations in milliseconds");

        _operationCount = meterFactory.CreateCounter<long>(
            "mythetech.operation.count",
            unit: "{operations}",
            description: "Count of operations");

        _operationErrors = meterFactory.CreateCounter<long>(
            "mythetech.operation.errors",
            unit: "{errors}",
            description: "Count of operation errors");

        _logger = logger;
    }

    /// <inheritdoc />
    public IOperationTimer BeginOperation(string operationName, params KeyValuePair<string, object?>[] tags)
    {
        return new OperationTimer(this, operationName, tags, _logger);
    }

    /// <inheritdoc />
    public void RecordOperation(string operationName, TimeSpan duration, params KeyValuePair<string, object?>[] tags)
    {
        var allTags = CombineTags(tags, new KeyValuePair<string, object?>("operation", operationName));

        _operationDuration.Record(duration.TotalMilliseconds, allTags);
        _operationCount.Increment(allTags);

        _logger?.LogDebug("Operation {OperationName} completed in {Duration}ms", operationName, duration.TotalMilliseconds);
    }

    internal void RecordError(string operationName, Exception? exception, params KeyValuePair<string, object?>[] tags)
    {
        var allTags = CombineTags(tags,
            new KeyValuePair<string, object?>("operation", operationName),
            new KeyValuePair<string, object?>("error.type", exception?.GetType().Name));

        _operationErrors.Increment(allTags);
    }

    /// <inheritdoc />
    public async Task<T> TimeAsync<T>(string operationName, Func<Task<T>> operation, params KeyValuePair<string, object?>[] tags)
    {
        using var timer = BeginOperation(operationName, tags);
        try
        {
            return await operation();
        }
        catch (Exception ex)
        {
            timer.MarkFailed(ex);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task TimeAsync(string operationName, Func<Task> operation, params KeyValuePair<string, object?>[] tags)
    {
        using var timer = BeginOperation(operationName, tags);
        try
        {
            await operation();
        }
        catch (Exception ex)
        {
            timer.MarkFailed(ex);
            throw;
        }
    }

    private static KeyValuePair<string, object?>[] CombineTags(KeyValuePair<string, object?>[] existing, params KeyValuePair<string, object?>[] additional)
    {
        var result = new KeyValuePair<string, object?>[existing.Length + additional.Length];
        existing.CopyTo(result, 0);
        additional.CopyTo(result, existing.Length);
        return result;
    }

    private sealed class OperationTimer : IOperationTimer
    {
        private readonly DefaultPerformanceMonitor _monitor;
        private readonly Stopwatch _stopwatch;
        private readonly KeyValuePair<string, object?>[] _tags;
        private readonly ILogger? _logger;
        private bool _failed;
        private Exception? _exception;
        private bool _disposed;

        public string OperationName { get; }
        public TimeSpan Elapsed => _stopwatch.Elapsed;

        public OperationTimer(DefaultPerformanceMonitor monitor, string operationName, KeyValuePair<string, object?>[] tags, ILogger? logger)
        {
            _monitor = monitor;
            OperationName = operationName;
            _tags = tags;
            _logger = logger;
            _stopwatch = Stopwatch.StartNew();
        }

        public void MarkFailed(Exception? exception = null)
        {
            _failed = true;
            _exception = exception;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _stopwatch.Stop();

            if (_failed)
            {
                _monitor.RecordError(OperationName, _exception, _tags);
                _logger?.LogWarning(_exception, "Operation {OperationName} failed after {Duration}ms",
                    OperationName, _stopwatch.ElapsedMilliseconds);
            }

            _monitor.RecordOperation(OperationName, _stopwatch.Elapsed, _tags);
        }
    }
}
