namespace Mythetech.Framework.Observability.Metrics;

/// <summary>
/// Represents a gauge metric that is observed asynchronously.
/// The value is provided by a callback function.
/// </summary>
/// <typeparam name="T">The numeric type for the gauge value.</typeparam>
public interface IObservableGauge<T> where T : struct
{
    /// <summary>
    /// Gets the name of the gauge.
    /// </summary>
    string Name { get; }
}
