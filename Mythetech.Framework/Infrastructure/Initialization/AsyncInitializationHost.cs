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
    private Task? _run;
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
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Later callers share the first run rather than returning while it is still going, so IsInitialized
        // is true for every caller once its await completes. The run is claimed before any hook starts, so a
        // hook that calls back in cannot start a second run.
        var run = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var existing = Interlocked.CompareExchange(ref _run, run.Task, null);
        if (existing is not null)
        {
            _logger.LogDebug("Initialization already started, waiting for it to finish");
            return existing;
        }

        return RunAsync(run, cancellationToken);
    }

    private async Task RunAsync(TaskCompletionSource run, CancellationToken cancellationToken)
    {
        try
        {
            await RunHooksAsync(cancellationToken);
        }
        finally
        {
            // Later callers only wait for the run to end. A failure is reported once, to the caller that ran
            // it; faulting the shared task too would leave an unobserved exception when nobody else waits.
            run.SetResult();
        }
    }

    private async Task RunHooksAsync(CancellationToken cancellationToken)
    {
        var orderedHooks = _hooks.OrderBy(h => h.Order).ToList();
        _logger.LogDebug("Starting async initialization with {Count} hooks", orderedHooks.Count);

        for (var i = 0; i < orderedHooks.Count; i++)
        {
            var hook = orderedHooks[i];
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Initialization cancelled before {HookName}", hook.Name);
                // Hooks that never ran are recorded as cancelled, so a cancelled run never reads as a
                // successful one.
                var cancelled = new OperationCanceledException(cancellationToken);
                _results = [.. _results, .. orderedHooks.Skip(i).Select(h => new InitializationHookResult(h.Name, h.Order, TimeSpan.Zero, cancelled))];
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
