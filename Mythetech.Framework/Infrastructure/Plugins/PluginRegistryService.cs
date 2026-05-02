using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mythetech.Framework.Infrastructure.Plugins;

/// <summary>
/// Service for fetching and managing the plugin registry
/// </summary>
public class PluginRegistryService : IPluginRegistryService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly PluginRegistryOptions _options;
    private readonly ILogger<PluginRegistryService> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    public PluginRegistryService(
        IHttpClientFactory httpClientFactory,
        IOptions<PluginRegistryOptions> options,
        ILogger<PluginRegistryService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PluginRegistry?> FetchRegistryAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, _options.PluginRegistryUri);
            request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };

            var response = await httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            var registry = await response.Content.ReadFromJsonAsync<PluginRegistry>(cancellationToken);

            if (registry is null)
            {
                _logger.LogWarning("Failed to deserialize plugin registry from {Uri}", _options.PluginRegistryUri);
                return null;
            }

            _logger.LogInformation("Fetched plugin registry with {Count} plugins", registry.Plugins.Count);
            return registry;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch plugin registry from {Uri}", _options.PluginRegistryUri);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse plugin registry JSON from {Uri}", _options.PluginRegistryUri);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching plugin registry from {Uri}", _options.PluginRegistryUri);
            return null;
        }
    }
}

