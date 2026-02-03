using System.Diagnostics.Metrics;

namespace Mythetech.Framework.Observability.Metrics;

/// <summary>
/// Default meter factory implementation using System.Diagnostics.Metrics.
/// </summary>
public class DiagnosticsMeterFactory : IMeterFactory, IDisposable
{
    private readonly Meter _meter;
    private bool _disposed;

    /// <summary>
    /// Creates a new DiagnosticsMeterFactory with the specified meter name.
    /// </summary>
    /// <param name="meterName">Name for the meter (typically the application name).</param>
    /// <param name="version">Optional version string.</param>
    public DiagnosticsMeterFactory(string meterName, string? version = null)
    {
        _meter = new Meter(meterName, version);
    }

    /// <inheritdoc />
    public ICounter<T> CreateCounter<T>(string name, string? unit = null, string? description = null) where T : struct
    {
        var counter = _meter.CreateCounter<T>(name, unit, description);
        return new DiagnosticsCounter<T>(counter);
    }

    /// <inheritdoc />
    public IHistogram<T> CreateHistogram<T>(string name, string? unit = null, string? description = null) where T : struct
    {
        var histogram = _meter.CreateHistogram<T>(name, unit, description);
        return new DiagnosticsHistogram<T>(histogram);
    }

    /// <inheritdoc />
    public IObservableGauge<T> CreateObservableGauge<T>(
        string name,
        Func<T> observeValue,
        string? unit = null,
        string? description = null) where T : struct
    {
        var gauge = _meter.CreateObservableGauge(name, observeValue, unit, description);
        return new DiagnosticsObservableGauge<T>(gauge);
    }

    /// <summary>
    /// Disposes the underlying meter.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _meter.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

internal sealed class DiagnosticsCounter<T> : ICounter<T> where T : struct
{
    private readonly Counter<T> _counter;

    public DiagnosticsCounter(Counter<T> counter)
    {
        _counter = counter;
    }

    public string Name => _counter.Name;

    public void Add(T delta, params KeyValuePair<string, object?>[] tags)
    {
        _counter.Add(delta, tags);
    }

    public void Increment(params KeyValuePair<string, object?>[] tags)
    {
        if (typeof(T) == typeof(int))
        {
            _counter.Add((T)(object)1, tags);
        }
        else if (typeof(T) == typeof(long))
        {
            _counter.Add((T)(object)1L, tags);
        }
        else if (typeof(T) == typeof(double))
        {
            _counter.Add((T)(object)1.0, tags);
        }
        else if (typeof(T) == typeof(float))
        {
            _counter.Add((T)(object)1.0f, tags);
        }
    }
}

internal sealed class DiagnosticsHistogram<T> : IHistogram<T> where T : struct
{
    private readonly Histogram<T> _histogram;

    public DiagnosticsHistogram(Histogram<T> histogram)
    {
        _histogram = histogram;
    }

    public string Name => _histogram.Name;

    public void Record(T value, params KeyValuePair<string, object?>[] tags)
    {
        _histogram.Record(value, tags);
    }
}

internal sealed class DiagnosticsObservableGauge<T> : IObservableGauge<T> where T : struct
{
    private readonly ObservableGauge<T> _gauge;

    public DiagnosticsObservableGauge(ObservableGauge<T> gauge)
    {
        _gauge = gauge;
    }

    public string Name => _gauge.Name;
}
