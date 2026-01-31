using Microsoft.Extensions.Logging;

namespace Mythetech.Framework.Infrastructure.Initialization;

/// <summary>
/// Default implementation of <see cref="IAsyncInitializationHost"/>.
/// Runs all registered <see cref="IAsyncInitializationHook"/> instances in order by their Order property.
/// </summary>
public class AsyncInitializationHost : IAsyncInitializationHost
{
    private readonly IEnumerable<IAsyncInitializationHook> _hooks;
    private readonly ILogger<AsyncInitializationHost> _logger;
    private int _initialized;

    /// <summary>
    /// Creates a new async initialization host.
    /// </summary>
    /// <param name="hooks">All registered initialization hooks</param>
    /// <param name="logger">Logger for diagnostics</param>
    public AsyncInitializationHost(
        IEnumerable<IAsyncInitializationHook> hooks,
        ILogger<AsyncInitializationHost> logger)
    {
        _hooks = hooks;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsInitialized => _initialized == 1;

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Ensure single execution
        if (Interlocked.Exchange(ref _initialized, 1) == 1)
        {
            _logger.LogDebug("Initialization already complete or in progress, skipping");
            return;
        }

        var orderedHooks = _hooks.OrderBy(h => h.Order).ToList();
        _logger.LogDebug("Starting async initialization with {Count} hooks", orderedHooks.Count);

        foreach (var hook in orderedHooks)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Initialization cancelled before {HookName}", hook.Name);
                break;
            }

            await ExecuteHookAsync(hook, cancellationToken);
        }

        _logger.LogInformation("Async initialization complete");
    }

    private async Task ExecuteHookAsync(IAsyncInitializationHook hook, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Executing initialization hook: {HookName} (order: {Order})", hook.Name, hook.Order);
            await hook.InitializeAsync(cancellationToken);
            _logger.LogDebug("Completed initialization hook: {HookName}", hook.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute initialization hook: {HookName}", hook.Name);
            // Continue with other hooks - don't fail entire initialization
        }
    }
}
