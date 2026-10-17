using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.FeatureFlags;

namespace Mythetech.Framework.Infrastructure.Initialization;

/// <summary>
/// Built-in initialization hook that registers the app's feature flags.
/// Runs at order 200, after <see cref="SettingsInitializationHook"/> has loaded the persisted flag values.
/// </summary>
public class FeatureFlagsInitializationHook : IAsyncInitializationHook
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FeatureFlagsInitializationHook> _logger;

    /// <summary>
    /// Creates a new feature flags initialization hook.
    /// </summary>
    public FeatureFlagsInitializationHook(
        IServiceProvider serviceProvider,
        ILogger<FeatureFlagsInitializationHook> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public int Order => 200;

    /// <inheritdoc />
    public string Name => "Feature flags";

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _serviceProvider.UseFeatureFlags();
        _logger.LogDebug("Feature flags initialized");
    }
}
