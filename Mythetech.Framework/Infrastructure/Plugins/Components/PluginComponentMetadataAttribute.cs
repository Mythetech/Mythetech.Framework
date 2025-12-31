namespace Mythetech.Framework.Infrastructure.Plugins.Components;

/// <summary>
/// Attribute to declare plugin component metadata without requiring instantiation.
/// When present, this takes priority over property-based metadata extraction.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class PluginComponentMetadataAttribute : Attribute
{
    /// <summary>
    /// Icon to display 
    /// </summary>
    public string? Icon { get; set; }
    
    /// <summary>
    /// Display title for the component
    /// </summary>
    public string? Title { get; set; }
    
    /// <summary>
    /// Display order (lower values appear first)
    /// </summary>
    public int Order { get; set; } = 100;
    
    /// <summary>
    /// Optional tooltip text
    /// </summary>
    public string? Tooltip { get; set; }
}

