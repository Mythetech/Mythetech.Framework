using Microsoft.JSInterop;

namespace Mythetech.Framework.Infrastructure.Plugins;

/// <summary>
/// Wraps the Framework's plugin-modules.js, which imports an ES module from source text.
/// </summary>
public sealed class PluginModuleInterop : IAsyncDisposable
{
    private const string ModulePath = "./_content/Mythetech.Framework/js/plugin-modules.js";
    private const string ImportFromSourceFunction = "importFromSource";

    private readonly IJSRuntime _jsRuntime;
    private IJSObjectReference? _module;

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
        _module ??= await _jsRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath);
        return await _module.InvokeAsync<IJSObjectReference>(ImportFromSourceFunction, code);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_module == null)
            return;

        try
        {
            await _module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
        }
    }
}
