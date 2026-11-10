using Microsoft.JSInterop;

namespace Mythetech.Framework.Infrastructure.Plugins;

/// <summary>
/// Imports plugin modules from the host's static web assets, which is where WebAssembly plugins are served from.
/// </summary>
public class WebPluginModuleLoader : IPluginModuleLoader
{
    private const string ImportIdentifier = "import";
    private const string ContentPrefix = "_content/";

    private readonly IJSRuntime _jsRuntime;

    /// <summary>
    /// Constructor
    /// </summary>
    public WebPluginModuleLoader(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <inheritdoc />
    public async Task<IJSObjectReference> ImportAsync(PluginInfo plugin, string modulePath)
    {
        if (PluginUrls.IsExternalUrl(modulePath))
            throw new ArgumentException("Plugin modules must be inside the plugin, not external URLs.", nameof(modulePath));

        var trimmed = modulePath.TrimStart('/');
        var url = trimmed.StartsWith(ContentPrefix)
            ? $"./{trimmed}"
            : $"./{ContentPrefix}{plugin.Assembly.GetName().Name}/{trimmed}";

        return await _jsRuntime.InvokeAsync<IJSObjectReference>(ImportIdentifier, url);
    }
}
