namespace Mythetech.Framework.Infrastructure.Plugins.Installation;

/// <summary>
/// The outcome of installing or importing a plugin.
/// </summary>
/// <param name="Succeeded">Whether the plugins were registered</param>
/// <param name="Plugins">The plugins registered, empty on failure</param>
/// <param name="Message">A message to show the user</param>
public sealed record PluginInstallResult(bool Succeeded, IReadOnlyList<PluginInfo> Plugins, string Message);
