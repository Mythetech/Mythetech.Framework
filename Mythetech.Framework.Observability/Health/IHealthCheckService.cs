namespace Mythetech.Framework.Observability.Health;

/// <summary>
/// Service for running and aggregating health checks.
/// </summary>
public interface IHealthCheckService
{
    /// <summary>
    /// Runs all registered health checks.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An aggregated health report.</returns>
    Task<HealthReport> CheckHealthAsync(CancellationToken ct = default);

    /// <summary>
    /// Runs health checks that match the specified predicate.
    /// </summary>
    /// <param name="predicate">Filter for health checks to run.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An aggregated health report.</returns>
    Task<HealthReport> CheckHealthAsync(Func<IHealthCheck, bool> predicate, CancellationToken ct = default);
}
