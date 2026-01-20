using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Settings;

namespace Mythetech.Framework.Infrastructure.FeatureFlags;

/// <summary>
/// Implementation of IFeatureFlagService that reads/writes flag state
/// through the settings framework, enabling persistence and UI integration.
/// </summary>
public class FeatureFlagService : IFeatureFlagService
{
    private readonly IFeatureFlagRegistry _registry;
    private readonly ISettingsProvider _settingsProvider;
    private readonly ILogger<FeatureFlagService> _logger;

    /// <summary>
    /// Creates a new instance of <see cref="FeatureFlagService"/>.
    /// </summary>
    /// <param name="registry">The feature flag registry.</param>
    /// <param name="settingsProvider">The settings provider for persistence.</param>
    /// <param name="logger">The logger.</param>
    public FeatureFlagService(
        IFeatureFlagRegistry registry,
        ISettingsProvider settingsProvider,
        ILogger<FeatureFlagService> logger)
    {
        _registry = registry;
        _settingsProvider = settingsProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<List<FeatureFlag>> GetFeatureFlagsAsync(bool enabledOnly = true)
    {
        var flags = _registry.GetAllFlags()
            .Where(f => !enabledOnly || f.SourceSettings.GetFlagValue(f.Key))
            .Select(f => new FeatureFlag
            {
                Feature = f.Key,
                Enabled = f.SourceSettings.GetFlagValue(f.Key)
            })
            .ToList();

        return Task.FromResult(flags);
    }

    /// <inheritdoc />
    public Task<bool> IsFeatureFlagEnabled(string feature)
    {
        var flag = _registry.GetFlag(feature);
        if (flag == null)
        {
            _logger.LogDebug("Feature flag '{Feature}' not found, returning false", feature);
            return Task.FromResult(false);
        }

        return Task.FromResult(flag.SourceSettings.GetFlagValue(feature));
    }

    /// <inheritdoc />
    public async Task EnableFeatureFlag(string feature)
    {
        var flag = _registry.GetFlag(feature);
        if (flag == null)
        {
            _logger.LogWarning("Cannot enable unknown feature flag '{Feature}'", feature);
            return;
        }

        flag.SourceSettings.SetFlagValue(feature, true);
        await _settingsProvider.NotifySettingsChangedAsync(flag.SourceSettings);
    }

    /// <inheritdoc />
    public async Task DisableFeatureFlag(string feature)
    {
        var flag = _registry.GetFlag(feature);
        if (flag == null)
        {
            _logger.LogWarning("Cannot disable unknown feature flag '{Feature}'", feature);
            return;
        }

        flag.SourceSettings.SetFlagValue(feature, false);
        await _settingsProvider.NotifySettingsChangedAsync(flag.SourceSettings);
    }
}
