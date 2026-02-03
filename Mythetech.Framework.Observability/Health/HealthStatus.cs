namespace Mythetech.Framework.Observability.Health;

/// <summary>
/// Represents the health status of a component.
/// </summary>
public enum HealthStatus
{
    /// <summary>
    /// The component is healthy and functioning normally.
    /// </summary>
    Healthy = 0,

    /// <summary>
    /// The component is degraded but still operational.
    /// </summary>
    Degraded = 1,

    /// <summary>
    /// The component is unhealthy and not functioning properly.
    /// </summary>
    Unhealthy = 2
}
