namespace Mythetech.Framework.Observability.Health;

/// <summary>
/// Represents the aggregated health report from all health checks.
/// </summary>
public record HealthReport
{
    /// <summary>
    /// Gets the overall health status.
    /// </summary>
    public required HealthStatus Status { get; init; }

    /// <summary>
    /// Gets the total duration of all health checks.
    /// </summary>
    public TimeSpan TotalDuration { get; init; }

    /// <summary>
    /// Gets the individual health check entries.
    /// </summary>
    public required IReadOnlyDictionary<string, HealthCheckResult> Entries { get; init; }

    /// <summary>
    /// Gets the timestamp when the report was generated.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
