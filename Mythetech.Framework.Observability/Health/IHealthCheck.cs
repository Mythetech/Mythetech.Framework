namespace Mythetech.Framework.Observability.Health;

/// <summary>
/// Represents a health check for a component or dependency.
/// </summary>
public interface IHealthCheck
{
    /// <summary>
    /// Gets the name of the health check.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets optional tags for categorizing this health check.
    /// </summary>
    IEnumerable<string> Tags => [];

    /// <summary>
    /// Runs the health check.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The health check result.</returns>
    Task<HealthCheckResult> CheckAsync(CancellationToken ct = default);
}
