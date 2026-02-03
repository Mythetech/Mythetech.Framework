namespace Mythetech.Framework.Observability.Metrics;

/// <summary>
/// Factory for creating metric instruments (counters, histograms, gauges).
/// </summary>
public interface IMeterFactory
{
    /// <summary>
    /// Creates a counter instrument.
    /// </summary>
    /// <typeparam name="T">The numeric type for the counter.</typeparam>
    /// <param name="name">The name of the counter.</param>
    /// <param name="unit">Optional unit of measurement.</param>
    /// <param name="description">Optional description.</param>
    /// <returns>A counter instrument.</returns>
    ICounter<T> CreateCounter<T>(string name, string? unit = null, string? description = null) where T : struct;

    /// <summary>
    /// Creates a histogram instrument.
    /// </summary>
    /// <typeparam name="T">The numeric type for the histogram.</typeparam>
    /// <param name="name">The name of the histogram.</param>
    /// <param name="unit">Optional unit of measurement.</param>
    /// <param name="description">Optional description.</param>
    /// <returns>A histogram instrument.</returns>
    IHistogram<T> CreateHistogram<T>(string name, string? unit = null, string? description = null) where T : struct;

    /// <summary>
    /// Creates an observable gauge instrument.
    /// </summary>
    /// <typeparam name="T">The numeric type for the gauge.</typeparam>
    /// <param name="name">The name of the gauge.</param>
    /// <param name="observeValue">Callback that returns the current value.</param>
    /// <param name="unit">Optional unit of measurement.</param>
    /// <param name="description">Optional description.</param>
    /// <returns>An observable gauge instrument.</returns>
    IObservableGauge<T> CreateObservableGauge<T>(
        string name,
        Func<T> observeValue,
        string? unit = null,
        string? description = null) where T : struct;
}
