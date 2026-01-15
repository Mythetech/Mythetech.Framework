namespace Mythetech.Framework.Infrastructure.Plugins.Events;

/// <summary>
/// Published when plugin loading begins.
/// </summary>
/// <param name="PluginDirectory">The directory being scanned for plugins.</param>
public record PluginsLoadingStarted(string PluginDirectory);

/// <summary>
/// Published when plugin loading completes.
/// </summary>
/// <param name="PluginDirectory">The directory that was scanned.</param>
/// <param name="PluginCount">Number of plugins loaded.</param>
public record PluginsLoadingCompleted(string PluginDirectory, int PluginCount);

/// <summary>
/// Published when a plugin is registered/loaded.
/// </summary>
/// <param name="Plugin">The plugin that was loaded.</param>
public record PluginLoaded(PluginInfo Plugin);

/// <summary>
/// Published when a plugin is enabled.
/// </summary>
/// <param name="Plugin">The plugin that was enabled.</param>
public record PluginEnabled(PluginInfo Plugin);

/// <summary>
/// Published when a plugin is disabled.
/// </summary>
/// <param name="Plugin">The plugin that was disabled.</param>
public record PluginDisabled(PluginInfo Plugin);
