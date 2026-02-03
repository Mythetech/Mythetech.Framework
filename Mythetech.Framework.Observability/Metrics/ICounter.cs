namespace Mythetech.Framework.Observability.Metrics;

/// <summary>
/// Represents a monotonically increasing counter metric.
/// </summary>
/// <typeparam name="T">The numeric type for the counter value.</typeparam>
public interface ICounter<T> where T : struct
{
    /// <summary>
    /// Gets the name of the counter.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Adds a value to the counter.
    /// </summary>
    /// <param name="delta">The value to add (must be non-negative).</param>
    /// <param name="tags">Optional tags to associate with this measurement.</param>
    void Add(T delta, params KeyValuePair<string, object?>[] tags);

    /// <summary>
    /// Increments the counter by 1.
    /// </summary>
    /// <param name="tags">Optional tags to associate with this measurement.</param>
    void Increment(params KeyValuePair<string, object?>[] tags);
}
