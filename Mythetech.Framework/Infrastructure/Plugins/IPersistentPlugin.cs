namespace Mythetech.Framework.Infrastructure.Plugins;

/// <summary>
/// Marker interface for plugins that use persistent storage.
/// Plugins implementing this interface opt-in to data management capabilities:
/// - Export: Allow users to export plugin data
/// - Import: Allow users to import plugin data
/// - Delete: Clear plugin data when the plugin is removed
/// </summary>
public interface IPersistentPlugin
{
    /// <summary>
    /// Human-readable name for the data (shown in export/import dialogs)
    /// </summary>
    string DataDisplayName { get; }
    
    /// <summary>
    /// File extension for exported data (without the dot, e.g., "json")
    /// </summary>
    string ExportFileExtension => "json";
    
    /// <summary>
    /// Whether to prompt the user to delete data when the plugin is disabled/removed.
    /// Default: true
    /// </summary>
    bool PromptDeleteOnRemove => true;
}

