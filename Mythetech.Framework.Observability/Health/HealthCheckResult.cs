namespace Mythetech.Framework.Observability.Health;

/// <summary>
/// Represents the result of a health check.
/// </summary>
public record HealthCheckResult
{
    /// <summary>
    /// Gets the health status.
    /// </summary>
    public required HealthStatus Status { get; init; }

    /// <summary>
    /// Gets an optional description of the health check result.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the exception if the health check failed.
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Gets additional data associated with this health check.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Data { get; init; }

    /// <summary>
    /// Gets the duration of the health check.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Creates a healthy result.
    /// </summary>
    public static HealthCheckResult Healthy(string? description = null, IReadOnlyDictionary<string, object>? data = null)
        => new()
        {
            Status = HealthStatus.Healthy,
            Description = description,
            Data = data
        };

    /// <summary>
    /// Creates a degraded result.
    /// </summary>
    public static HealthCheckResult Degraded(string? description = null, Exception? exception = null, IReadOnlyDictionary<string, object>? data = null)
        => new()
        {
            Status = HealthStatus.Degraded,
            Description = description,
            Exception = exception,
            Data = data
        };

    /// <summary>
    /// Creates an unhealthy result.
    /// </summary>
    public static HealthCheckResult Unhealthy(string? description = null, Exception? exception = null, IReadOnlyDictionary<string, object>? data = null)
        => new()
        {
            Status = HealthStatus.Unhealthy,
            Description = description,
            Exception = exception,
            Data = data
        };
}
