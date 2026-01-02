namespace Mythetech.Framework.Infrastructure.Plugins;

/// <summary>
/// Configuration options for the plugin registry service
/// </summary>
public class PluginRegistryOptions
{
    /// <summary>
    /// Default registry URI
    /// </summary>
    public const string DefaultRegistryUri = "https://cdn-endpnt-stmythetechglobal.azureedge.net/release/plugins/mythetech-plugin-registry.json";
    
    /// <summary>
    /// URI to the plugin registry JSON file
    /// </summary>
    public string PluginRegistryUri { get; set; } = DefaultRegistryUri;
}

