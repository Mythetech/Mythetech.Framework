using Mythetech.Framework.Infrastructure.Plugins;

namespace SamplePlugin;

/// <summary>
/// Manifest for the sample plugin demonstrating plugin framework usage
/// </summary>
public class SamplePluginManifest : IPluginManifest
{
    public string Id => "com.mythetech.sample.plugin";
    public string Name => "Sample Plugin";
    public Version Version => new(1, 0, 0);
    public string Developer => "Mythetech";
    public string Description => "A sample plugin demonstrating the plugin framework";
    public string? ProjectUrl => "https://github.com/mythetech/Mythetech.Framework";
    
    /// <summary>
    /// Assets to load when the plugin is enabled.
    /// The path is relative to the plugin's wwwroot folder.
    /// </summary>
    public PluginAsset[] Assets =>
    [
        // Relative path - will be resolved to /_content/SamplePlugin/css/sample-plugin.css
        PluginAsset.Css("css/sample-plugin.css"),
    ];
}

