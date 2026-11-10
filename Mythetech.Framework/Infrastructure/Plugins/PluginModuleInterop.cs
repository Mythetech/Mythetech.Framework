using Microsoft.JSInterop;

namespace Mythetech.Framework.Infrastructure.Plugins;

/// <summary>
/// Wraps the Framework's plugin-modules.js, which imports an ES module from source text.
/// </summary>
public sealed class PluginModuleInterop : IAsyncDisposable
{
    private const string ModulePath = "./_content/Mythetech.Framework/js/plugin-modules.js";
    private const string ImportIdentifier = "import";
    private const string ImportFromSourceFunction = "importFromSource";

    private readonly IJSRuntime _jsRuntime;
    private readonly Lock _moduleLock = new();
    private Task<IJSObjectReference>? _moduleTask;

    /// <summary>
    /// Constructor
    /// </summary>
    public PluginModuleInterop(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Imports a module from its source text through a Blob URL.
    /// </summary>
    /// <param name="code">The module's JavaScript source</param>
    public async Task<IJSObjectReference> ImportFromSourceAsync(string code)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<IJSObjectReference>(ImportFromSourceFunction, code);
    }

    // Concurrent first calls share one import, and a failed import is cleared so the next call tries again.
    private async Task<IJSObjectReference> GetModuleAsync()
    {
        Task<IJSObjectReference> moduleTask;
        lock (_moduleLock)
        {
            moduleTask = _moduleTask ??= _jsRuntime.InvokeAsync<IJSObjectReference>(ImportIdentifier, ModulePath).AsTask();
        }

        try
        {
            return await moduleTask;
        }
        catch
        {
            lock (_moduleLock)
            {
                if (ReferenceEquals(_moduleTask, moduleTask))
                    _moduleTask = null;
            }

            throw;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        Task<IJSObjectReference>? moduleTask;
        lock (_moduleLock)
        {
            moduleTask = _moduleTask;
            _moduleTask = null;
        }

        if (moduleTask is not { IsCompletedSuccessfully: true })
            return;

        try
        {
            var module = await moduleTask;
            await module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
        }
    }
}
