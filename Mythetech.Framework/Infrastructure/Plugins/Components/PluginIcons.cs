using MudBlazor;

namespace Mythetech.Framework.Infrastructure.Plugins.Components;

/// <summary>
/// Common icon SVG paths for use in PluginComponentMetadataAttribute.
/// These are compile-time constants that can be used in attributes.
/// Values are sourced from MudBlazor's Material Design icons.
/// </summary>
public static class PluginIcons
{
    /// <summary>Extension/puzzle piece icon (default for plugins)</summary>
    public const string DefaultPluginIcon = Icons.Material.Filled.Extension;
}

