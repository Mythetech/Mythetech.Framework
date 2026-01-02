namespace Mythetech.Framework.Infrastructure.Plugins;

/// <summary>
/// Service for fetching and managing the plugin registry
/// </summary>
public interface IPluginRegistryService
{
    /// <summary>
    /// Fetches the plugin registry from the configured URI
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The plugin registry, or null if fetch failed</returns>
    Task<PluginRegistry?> FetchRegistryAsync(CancellationToken cancellationToken = default);
}

