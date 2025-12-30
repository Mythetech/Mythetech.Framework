namespace Mythetech.Framework.Infrastructure.Plugins;

/// <summary>
/// Interface that plugin assemblies must implement to provide metadata.
/// Discovered via reflection when inspecting uploaded DLLs.
/// A single implementation of this interface must exist in each plugin assembly.
/// </summary>
public interface IPluginManifest
{
    /// <summary>
    /// Unique identifier for the plugin (recommend: reverse domain notation, e.g., "com.mythetech.siren.myplugin")
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Human-readable display name
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Plugin version (semantic versioning recommended)
    /// </summary>
    Version Version { get; }
    
    /// <summary>
    /// Developer or organization name
    /// </summary>
    string Developer { get; }
    
    /// <summary>
    /// Brief description of the plugin's functionality
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// Optional URL for more information
    /// </summary>
    string? ProjectUrl => null;
    
    /// <summary>
    /// Optional minimum framework version required
    /// </summary>
    Version? MinimumFrameworkVersion => null;
    
    /// <summary>
    /// CSS and JavaScript assets required by this plugin.
    /// These are loaded when the plugin is enabled.
    /// </summary>
    PluginAsset[] Assets => [];
}

