using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Mythetech.Framework.Infrastructure.Guards;

/// <inheritdoc />
public class JsGuardService : IJsGuardService
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);

    private readonly ConcurrentDictionary<string, bool> _readyState = new();
    private readonly ILogger<JsGuardService> _logger;

    /// <summary>
    /// Creates a new instance of <see cref="JsGuardService"/>.
    /// </summary>
    public JsGuardService(ILogger<JsGuardService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsReady(string name) =>
        _readyState.TryGetValue(name, out var ready) && ready;

    /// <inheritdoc />
    public async Task<bool> WaitForReadyAsync(IJSRuntime js, string name, TimeSpan? timeout = null)
    {
        if (IsReady(name))
            return true;

        var timeoutMs = (int)(timeout ?? DefaultTimeout).TotalMilliseconds;
        var ready = await js.InvokeAsync<bool>("waitForJsGuard", name, timeoutMs);

        if (ready)
        {
            _readyState[name] = true;
        }
        else
        {
            _logger.LogWarning("JS guard '{GuardName}' timed out — dependency may not be available", name);
        }

        return ready;
    }

    /// <inheritdoc />
    public async Task ResetAsync(IJSRuntime js, string name)
    {
        _readyState.TryRemove(name, out _);
        await js.InvokeVoidAsync("clearJsGuard", name);
    }
}
