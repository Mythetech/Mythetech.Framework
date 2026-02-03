namespace Mythetech.Framework.Observability;

/// <summary>
/// Configuration options for the observability framework.
/// </summary>
public class ObservabilityOptions
{
    /// <summary>
    /// Gets or sets the meter name for metrics.
    /// Defaults to "Mythetech".
    /// </summary>
    public string MeterName { get; set; } = "Mythetech";

    /// <summary>
    /// Gets or sets the meter version.
    /// </summary>
    public string? MeterVersion { get; set; }

    /// <summary>
    /// Gets or sets whether to enable performance monitoring.
    /// Defaults to true.
    /// </summary>
    public bool EnablePerformanceMonitoring { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable the operation context.
    /// Defaults to true.
    /// </summary>
    public bool EnableOperationContext { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable the exception handler.
    /// Defaults to true.
    /// </summary>
    public bool EnableExceptionHandler { get; set; } = true;
}
