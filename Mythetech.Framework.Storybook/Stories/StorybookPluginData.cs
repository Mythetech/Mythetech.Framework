using System.Reflection;
using MudBlazor;
using Mythetech.Framework.Infrastructure.Plugins;

namespace Mythetech.Framework.Storybook.Stories;

/// <summary>
/// Static data for Storybook demonstrations of PluginGuard.
/// Provides fake plugin manifests and metadata for testing different states.
/// </summary>
public static class StorybookPluginData
{
    private static readonly IPluginManifest _manifest = new FakePluginManifest
    {
        Id = "storybook.demo.plugin",
        Name = "Demo Plugin",
        Version = new Version(2, 5, 1),
        Developer = "Storybook Team",
        Description = "A demonstration plugin for Storybook examples"
    };
    
    private static readonly Assembly _assembly = typeof(StorybookPluginData).Assembly;
    
    /// <summary>
    /// Enabled plugin info
    /// </summary>
    public static readonly PluginInfo EnabledPluginInfo = new PluginInfo
    {
        Manifest = _manifest,
        Assembly = _assembly,
        IsEnabled = true,
        LoadedAt = DateTime.UtcNow.AddDays(-5)
    };
    
    /// <summary>
    /// Disabled plugin ino
    /// </summary>
    public static readonly PluginInfo DisabledPluginInfo = new PluginInfo
    {
        Manifest = _manifest,
        Assembly = _assembly,
        IsEnabled = false,
        LoadedAt = DateTime.UtcNow.AddDays(-5)
    };
    
    /// <summary>
    /// Deleted plugin info
    /// </summary>
    public static readonly PluginInfo DeletedPluginInfo = new PluginInfo
    {
        Manifest = _manifest,
        Assembly = _assembly,
        IsEnabled = true,
        LoadedAt = DateTime.UtcNow.AddDays(-5)
    };
    
    /// <summary>
    /// Component metadata
    /// </summary>
    public static readonly PluginComponentMetadata ComponentMetadata = new PluginComponentMetadata
    {
        ComponentType = typeof(StorybookPluginData),
        Title = "Demo Component",
        Icon = Icons.Material.Filled.Extension,
        Order = 1,
        Tooltip = "A demonstration plugin component"
    };
    
    private class FakePluginManifest : IPluginManifest
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
        public required Version Version { get; init; }
        public required string Developer { get; init; }
        public required string Description { get; init; }
        public string? Icon => Icons.Material.Filled.Extension;
        public string? ProjectUrl => "https://github.com/mythetech/Mythetech.Framework";
        public Version? MinimumFrameworkVersion => new Version(10, 0, 0);
        public PluginAsset[] Assets => [];
    }
}

/// <summary>
/// Type of plugin scenario
/// </summary>
public enum PluginScenario
{
    /// <summary>
    /// Enabled plugin
    /// </summary>
    Enabled,
    /// <summary>
    /// Disabled plugin
    /// </summary>
    Disabled,
    /// <summary>
    /// Deleted plugin
    /// </summary>
    Deleted
}

