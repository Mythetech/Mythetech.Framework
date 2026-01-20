using Microsoft.Extensions.Logging;
using Mythetech.Framework.Components.FeatureFlags;
using Mythetech.Framework.Infrastructure.FeatureFlags.Events;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Settings.Events;

namespace Mythetech.Framework.Infrastructure.FeatureFlags.Consumers;

/// <summary>
/// Listens for settings changes on FeatureFlagsSettingsBase instances
/// and emits granular FeatureFlagEnabled/FeatureFlagDisabled events.
/// </summary>
public class FeatureFlagChangeTracker : IConsumer<SettingsModelChanged>
{
    private readonly IFeatureFlagRegistry _registry;
    private readonly IMessageBus _bus;
    private readonly DefaultFeatureFlagStateProvider _stateProvider;
    private readonly ILogger<FeatureFlagChangeTracker> _logger;
    private Dictionary<string, bool> _previousState = [];

    /// <summary>
    /// Creates a new instance of <see cref="FeatureFlagChangeTracker"/>.
    /// </summary>
    /// <param name="registry">The feature flag registry.</param>
    /// <param name="bus">The message bus for publishing events.</param>
    /// <param name="stateProvider">The state provider to notify of changes.</param>
    /// <param name="logger">The logger.</param>
    public FeatureFlagChangeTracker(
        IFeatureFlagRegistry registry,
        IMessageBus bus,
        DefaultFeatureFlagStateProvider stateProvider,
        ILogger<FeatureFlagChangeTracker> logger)
    {
        _registry = registry;
        _bus = bus;
        _stateProvider = stateProvider;
        _logger = logger;
        _previousState = _registry.CaptureState();
    }

    /// <inheritdoc />
    public async Task Consume(SettingsModelChanged message)
    {
        if (message.Settings is not FeatureFlagsSettingsBase)
            return;

        var currentState = _registry.CaptureState();
        var changedFlags = new List<(string Key, bool NewValue)>();

        foreach (var (key, currentValue) in currentState)
        {
            if (!_previousState.TryGetValue(key, out var previousValue))
            {
                if (currentValue)
                    changedFlags.Add((key, true));
            }
            else if (previousValue != currentValue)
            {
                changedFlags.Add((key, currentValue));
            }
        }

        foreach (var (key, newValue) in changedFlags)
        {
            var flag = _registry.GetFlag(key);
            var settingsId = flag?.SourceSettings.SettingsId ?? "Unknown";

            if (newValue)
            {
                _logger.LogInformation("Feature flag enabled: {Key}", key);
                await _bus.PublishAsync(new FeatureFlagEnabled(key, settingsId));
            }
            else
            {
                _logger.LogInformation("Feature flag disabled: {Key}", key);
                await _bus.PublishAsync(new FeatureFlagDisabled(key, settingsId));
            }
        }

        _previousState = currentState;

        if (changedFlags.Count > 0)
        {
            _stateProvider.NotifyStateChanged();
        }
    }
}
