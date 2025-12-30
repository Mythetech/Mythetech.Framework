namespace Mythetech.Framework.Infrastructure.Plugins;

/// <summary>
/// Represents an asset (CSS or JS) that a plugin requires
/// </summary>
public record PluginAsset
{
    /// <summary>
    /// The asset path. Can be:
    /// - Relative path (resolved to _content/{AssemblyName}/{Path})
    /// - Absolute URL (CDN, starts with http:// or https://)
    /// </summary>
    public required string Path { get; init; }
    
    /// <summary>
    /// The type of asset
    /// </summary>
    public required PluginAssetType Type { get; init; }
    
    /// <summary>
    /// Optional integrity hash for CDN resources (SRI)
    /// </summary>
    public string? Integrity { get; init; }
    
    /// <summary>
    /// Optional crossorigin attribute for CDN resources
    /// </summary>
    public string? CrossOrigin { get; init; }
    
    /// <summary>
    /// Create a CSS asset
    /// </summary>
    public static PluginAsset Css(string path, string? integrity = null, string? crossOrigin = null) 
        => new() { Path = path, Type = PluginAssetType.Css, Integrity = integrity, CrossOrigin = crossOrigin };
    
    /// <summary>
    /// Create a JavaScript asset
    /// </summary>
    public static PluginAsset Js(string path, string? integrity = null, string? crossOrigin = null) 
        => new() { Path = path, Type = PluginAssetType.JavaScript, Integrity = integrity, CrossOrigin = crossOrigin };
}

/// <summary>
/// Type of plugin asset
/// </summary>
public enum PluginAssetType
{
    /// <summary>
    /// CSS stylesheet
    /// </summary>
    Css,
    
    /// <summary>
    /// JavaScript file
    /// </summary>
    JavaScript
}

