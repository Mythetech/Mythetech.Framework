using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Mythetech.Framework.Infrastructure.Guards;

/// <inheritdoc />
public class JsGuardService : IJsGuardService
{
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
    public async Task<bool> WaitForReadyAsync(IJSRuntime js, string name)
    {
        if (IsReady(name))
            return true;

        var ready = await js.InvokeAsync<bool>("waitForJsGuard", name);
        _readyState[name] = ready;

        if (!ready)
            _logger.LogWarning("JS guard '{GuardName}' timed out — dependency may not be available", name);

        return ready;
    }
}
