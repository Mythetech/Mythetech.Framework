namespace Mythetech.Framework.Infrastructure.Initialization;

/// <summary>
/// A hook that participates in async application initialization.
/// Hooks are executed in order based on their <see cref="Order"/> property.
/// </summary>
public interface IAsyncInitializationHook
{
    /// <summary>
    /// The execution order for this hook. Lower values run first.
    /// Default hooks use orders like:
    /// - 100: Settings loading
    /// - 200: Feature flags
    /// - 300: MCP server
    /// Custom hooks should use higher values (e.g., 500+) to run after framework initialization.
    /// </summary>
    int Order => 500;

    /// <summary>
    /// A descriptive name for this hook, used in logging.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Performs the async initialization for this hook.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
