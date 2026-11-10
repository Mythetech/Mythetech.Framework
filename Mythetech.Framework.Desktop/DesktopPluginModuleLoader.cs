using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Mythetech.Framework.Infrastructure.Plugins;

namespace Mythetech.Framework.Desktop;

/// <summary>
/// Desktop module loader. Dynamic plugins can't use _content/ URLs, so it reads the module file
/// and imports its text. Each call reads the file again and returns its own module instance.
/// </summary>
public class DesktopPluginModuleLoader : IPluginModuleLoader
{
    private readonly PluginModuleInterop _interop;
    private readonly ILogger<DesktopPluginModuleLoader> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    public DesktopPluginModuleLoader(PluginModuleInterop interop, ILogger<DesktopPluginModuleLoader> logger)
    {
        _interop = interop;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IJSObjectReference> ImportAsync(PluginInfo plugin, string modulePath)
    {
        if (PluginAssetPathResolver.IsExternalUrl(modulePath))
            throw new ArgumentException("Plugin modules must be inside the plugin, not external URLs.", nameof(modulePath));

        var filePath = PluginAssetPathResolver.Resolve(plugin, modulePath, _logger);
        if (filePath == null || !File.Exists(filePath))
            throw new FileNotFoundException($"Plugin module not found: {modulePath}");

        var code = await File.ReadAllTextAsync(filePath);
        return await _interop.ImportFromSourceAsync(code);
    }
}
