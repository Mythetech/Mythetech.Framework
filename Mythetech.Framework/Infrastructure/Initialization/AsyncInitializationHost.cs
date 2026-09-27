using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.MessageBus;

namespace Mythetech.Framework.Infrastructure.Initialization;

/// <summary>
/// Default implementation of <see cref="IAsyncInitializationHost"/>.
/// Runs all registered <see cref="IAsyncInitializationHook"/> instances in order by their Order property,
/// records each outcome, and publishes <see cref="ApplicationReady"/> when the run finishes.
/// </summary>
public class AsyncInitializationHost : IAsyncInitializationHost
{
    private readonly IEnumerable<IAsyncInitializationHook> _hooks;
    private readonly ILogger<AsyncInitializationHost> _logger;
    private readonly IMessageBus? _messageBus;
    private int _started;
    private volatile bool _completed;
    private volatile InitializationHookResult[] _results = [];

    /// <summary>
    /// Creates a new async initialization host.
    /// </summary>
    /// <param name="hooks">All registered initialization hooks</param>
    /// <param name="logger">Logger for diagnostics</param>
    /// <param name="messageBus">Bus to publish <see cref="ApplicationReady"/> on, when the app has one</param>
    public AsyncInitializationHost(
        IEnumerable<IAsyncInitializationHook> hooks,
        ILogger<AsyncInitializationHost> logger,
        IMessageBus? messageBus = null)
    {
        _hooks = hooks;
        _logger = logger;
        _messageBus = messageBus;
    }

    /// <inheritdoc />
    public bool IsInitialized => _completed;

    /// <inheritdoc />
    public IReadOnlyList<InitializationHookResult> Results => _results;

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Ensure single execution
        if (Interlocked.Exchange(ref _started, 1) == 1)
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

            var result = await ExecuteHookAsync(hook, cancellationToken);
            _results = [.. _results, result];
        }

        _completed = true;
        _logger.LogInformation("Async initialization complete");

        if (_messageBus is not null)
        {
            await _messageBus.PublishAsync(new ApplicationReady(_results));
        }
    }

    private async Task<InitializationHookResult> ExecuteHookAsync(IAsyncInitializationHook hook, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogDebug("Executing initialization hook: {HookName} (order: {Order})", hook.Name, hook.Order);
            await hook.InitializeAsync(cancellationToken);
            _logger.LogDebug("Completed initialization hook: {HookName}", hook.Name);
            return new InitializationHookResult(hook.Name, hook.Order, stopwatch.Elapsed, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute initialization hook: {HookName}", hook.Name);
            // Continue with other hooks - don't fail entire initialization
            return new InitializationHookResult(hook.Name, hook.Order, stopwatch.Elapsed, ex);
        }
    }
}
