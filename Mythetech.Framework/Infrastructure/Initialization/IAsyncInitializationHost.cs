namespace Mythetech.Framework.Infrastructure.Initialization;

/// <summary>
/// Host service that coordinates async initialization.
/// Runs all registered <see cref="IAsyncInitializationHook"/> instances in order.
/// </summary>
public interface IAsyncInitializationHost
{
    /// <summary>
    /// Initializes all registered hooks.
    /// Safe to call multiple times - subsequent calls are no-ops.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether initialization has completed.
    /// </summary>
    bool IsInitialized { get; }
}
