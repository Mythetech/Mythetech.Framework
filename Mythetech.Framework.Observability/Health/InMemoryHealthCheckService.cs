using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Mythetech.Framework.Observability.Health;

/// <summary>
/// Default in-memory implementation of the health check service.
/// </summary>
public class InMemoryHealthCheckService : IHealthCheckService
{
    private readonly IEnumerable<IHealthCheck> _healthChecks;
    private readonly ILogger<InMemoryHealthCheckService>? _logger;

    /// <summary>
    /// Creates a new InMemoryHealthCheckService.
    /// </summary>
    /// <param name="healthChecks">The registered health checks.</param>
    /// <param name="logger">Optional logger.</param>
    public InMemoryHealthCheckService(IEnumerable<IHealthCheck> healthChecks, ILogger<InMemoryHealthCheckService>? logger = null)
    {
        _healthChecks = healthChecks;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<HealthReport> CheckHealthAsync(CancellationToken ct = default)
        => CheckHealthAsync(_ => true, ct);

    /// <inheritdoc />
    public async Task<HealthReport> CheckHealthAsync(Func<IHealthCheck, bool> predicate, CancellationToken ct = default)
    {
        var totalStopwatch = Stopwatch.StartNew();
        var entries = new Dictionary<string, HealthCheckResult>();
        var overallStatus = HealthStatus.Healthy;

        foreach (var check in _healthChecks.Where(predicate))
        {
            ct.ThrowIfCancellationRequested();

            var checkStopwatch = Stopwatch.StartNew();
            HealthCheckResult result;

            try
            {
                result = await check.CheckAsync(ct);
                checkStopwatch.Stop();
                result = result with { Duration = checkStopwatch.Elapsed };

                _logger?.LogDebug("Health check {Name} completed with status {Status} in {Duration}ms",
                    check.Name, result.Status, checkStopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                checkStopwatch.Stop();
                result = HealthCheckResult.Unhealthy(
                    description: $"Health check threw an exception: {ex.Message}",
                    exception: ex) with { Duration = checkStopwatch.Elapsed };

                _logger?.LogError(ex, "Health check {Name} threw an exception", check.Name);
            }

            entries[check.Name] = result;

            if (result.Status > overallStatus)
            {
                overallStatus = result.Status;
            }
        }

        totalStopwatch.Stop();

        return new HealthReport
        {
            Status = overallStatus,
            TotalDuration = totalStopwatch.Elapsed,
            Entries = entries
        };
    }
}
