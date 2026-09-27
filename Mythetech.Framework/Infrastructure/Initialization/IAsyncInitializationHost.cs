namespace Mythetech.Framework.Infrastructure.Initialization;

/// <summary>
/// Host service that coordinates async initialization.
/// Runs all registered <see cref="IAsyncInitializationHook"/> instances in order.
/// </summary>
public interface IAsyncInitializationHost
{
    /// <summary>
    /// Initializes all registered hooks.
    /// Safe to call multiple times: hooks run once, and later calls wait for that run to finish.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether the initialization run has finished. A cancelled run also finishes; its skipped hooks then
    /// appear in <see cref="Results"/> as cancelled.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// The outcome of each hook, in the order they ran.
    /// </summary>
    IReadOnlyList<InitializationHookResult> Results => [];
}
