using Microsoft.Extensions.Logging;

namespace Mythetech.Framework.Infrastructure.FeatureFlags;

/// <summary>
/// Central registry implementation that tracks all feature flags across all settings instances.
/// </summary>
public class FeatureFlagRegistry : IFeatureFlagRegistry
{
    private readonly List<FeatureFlagsSettingsBase> _settingsInstances = [];
    private readonly Dictionary<string, FeatureFlagInfo> _flagIndex = [];
    private readonly ILogger<FeatureFlagRegistry> _logger;

    /// <summary>
    /// Creates a new instance of <see cref="FeatureFlagRegistry"/>.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public FeatureFlagRegistry(ILogger<FeatureFlagRegistry> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public void RegisterFlagSettings(FeatureFlagsSettingsBase settings)
    {
        _settingsInstances.Add(settings);

        foreach (var flag in settings.GetFeatureFlags())
        {
            if (_flagIndex.ContainsKey(flag.Key))
            {
                _logger.LogWarning(
                    "Feature flag key '{Key}' is already registered. Skipping duplicate from {SettingsId}",
                    flag.Key, settings.SettingsId);
                continue;
            }

            _flagIndex[flag.Key] = flag;
            _logger.LogDebug("Registered feature flag: {Key} from {SettingsId}",
                flag.Key, settings.SettingsId);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<FeatureFlagInfo> GetAllFlags()
        => _flagIndex.Values.ToList().AsReadOnly();

    /// <inheritdoc />
    public FeatureFlagInfo? GetFlag(string key)
        => _flagIndex.TryGetValue(key, out var flag) ? flag : null;

    /// <inheritdoc />
    public IReadOnlyList<FeatureFlagsSettingsBase> GetAllFlagSettings()
        => _settingsInstances.AsReadOnly();

    /// <inheritdoc />
    public Dictionary<string, bool> CaptureState()
    {
        return _flagIndex.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.SourceSettings.GetFlagValue(kvp.Key));
    }
}
