namespace Mythetech.Framework.Observability.Performance;

/// <summary>
/// Provides performance monitoring capabilities for timed operations.
/// </summary>
public interface IPerformanceMonitor
{
    /// <summary>
    /// Begins timing an operation.
    /// </summary>
    /// <param name="operationName">Name of the operation being timed.</param>
    /// <param name="tags">Optional tags to associate with this measurement.</param>
    /// <returns>A timer that records the duration when disposed.</returns>
    IOperationTimer BeginOperation(string operationName, params KeyValuePair<string, object?>[] tags);

    /// <summary>
    /// Records a completed operation's duration.
    /// </summary>
    /// <param name="operationName">Name of the operation.</param>
    /// <param name="duration">Duration of the operation.</param>
    /// <param name="tags">Optional tags to associate with this measurement.</param>
    void RecordOperation(string operationName, TimeSpan duration, params KeyValuePair<string, object?>[] tags);

    /// <summary>
    /// Times an asynchronous operation.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="operationName">Name of the operation.</param>
    /// <param name="operation">The operation to time.</param>
    /// <param name="tags">Optional tags to associate with this measurement.</param>
    /// <returns>The result of the operation.</returns>
    Task<T> TimeAsync<T>(string operationName, Func<Task<T>> operation, params KeyValuePair<string, object?>[] tags);

    /// <summary>
    /// Times an asynchronous operation.
    /// </summary>
    /// <param name="operationName">Name of the operation.</param>
    /// <param name="operation">The operation to time.</param>
    /// <param name="tags">Optional tags to associate with this measurement.</param>
    Task TimeAsync(string operationName, Func<Task> operation, params KeyValuePair<string, object?>[] tags);
}

/// <summary>
/// A timer for an operation that records duration when disposed.
/// </summary>
public interface IOperationTimer : IDisposable
{
    /// <summary>
    /// Gets the name of the operation being timed.
    /// </summary>
    string OperationName { get; }

    /// <summary>
    /// Gets the elapsed time since the timer was started.
    /// </summary>
    TimeSpan Elapsed { get; }

    /// <summary>
    /// Marks the operation as failed.
    /// </summary>
    /// <param name="exception">Optional exception that caused the failure.</param>
    void MarkFailed(Exception? exception = null);
}
