using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Settings;

namespace Mythetech.Framework.Infrastructure.Initialization;

/// <summary>
/// Built-in initialization hook that loads persisted settings.
/// Runs at order 100 to ensure settings are available for other hooks.
/// </summary>
public class SettingsInitializationHook : IAsyncInitializationHook
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SettingsInitializationHook> _logger;

    /// <summary>
    /// Creates a new settings initialization hook.
    /// </summary>
    public SettingsInitializationHook(
        IServiceProvider serviceProvider,
        ILogger<SettingsInitializationHook> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public int Order => 100;

    /// <inheritdoc />
    public string Name => "Settings";

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _serviceProvider.LoadPersistedSettingsAsync();
        _logger.LogDebug("Settings loaded successfully");
    }
}
