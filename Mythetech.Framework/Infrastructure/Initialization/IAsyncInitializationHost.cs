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
    /// Whether initialization has finished running every hook.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// The outcome of each hook that has run, in the order they ran.
    /// </summary>
    IReadOnlyList<InitializationHookResult> Results => [];
}
