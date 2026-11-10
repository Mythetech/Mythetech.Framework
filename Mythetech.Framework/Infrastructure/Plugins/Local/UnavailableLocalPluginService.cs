using Mythetech.Framework.Infrastructure.Plugins.Installation;

namespace Mythetech.Framework.Infrastructure.Plugins.Local;

/// <summary>
/// The <see cref="ILocalPluginService"/> for apps that can't load local plugins, such as WebAssembly apps.
/// </summary>
public sealed class UnavailableLocalPluginService : ILocalPluginService
{
    /// <inheritdoc />
    public bool IsAvailable => false;

    /// <inheritdoc />
    public bool IsEnabled => false;

    /// <inheritdoc />
    public IReadOnlyList<LocalPluginEntry> Entries => [];

    /// <inheritdoc />
    public Task LoadSavedAsync() => Task.CompletedTask;

    /// <inheritdoc />
    public Task SetEnabledAsync(bool enabled) => Task.CompletedTask;

    /// <inheritdoc />
    public Task<PluginInstallResult> AddAsync(string dllPath) =>
        Task.FromResult(new PluginInstallResult(false, [], "Local plugins aren't available in this app."));

    /// <inheritdoc />
    public Task<bool> RemoveAsync(string pluginId) => Task.FromResult(false);
}
