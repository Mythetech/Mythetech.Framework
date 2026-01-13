namespace Mythetech.Framework.Infrastructure.Plugins;

/// <summary>
/// Optional interface for persisting plugin enabled/disabled state.
/// If not registered in DI, plugin state will be runtime-only (resets on app restart).
/// </summary>
public interface IPluginStateProvider
{
    /// <summary>
    /// Load the set of disabled plugin IDs from persistent storage.
    /// </summary>
    /// <returns>Set of plugin IDs that are disabled.</returns>
    Task<IReadOnlySet<string>> LoadDisabledPluginsAsync();

    /// <summary>
    /// Save the set of disabled plugin IDs to persistent storage.
    /// </summary>
    /// <param name="disabledPlugins">Set of plugin IDs that are disabled.</param>
    Task SaveDisabledPluginsAsync(IReadOnlySet<string> disabledPlugins);
}
