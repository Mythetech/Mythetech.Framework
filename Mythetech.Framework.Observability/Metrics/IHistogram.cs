namespace Mythetech.Framework.Observability.Metrics;

/// <summary>
/// Represents a histogram metric for recording distributions of values.
/// </summary>
/// <typeparam name="T">The numeric type for the histogram value.</typeparam>
public interface IHistogram<T> where T : struct
{
    /// <summary>
    /// Gets the name of the histogram.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Records a value in the histogram.
    /// </summary>
    /// <param name="value">The value to record.</param>
    /// <param name="tags">Optional tags to associate with this measurement.</param>
    void Record(T value, params KeyValuePair<string, object?>[] tags);
}
