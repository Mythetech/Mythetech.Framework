using Mythetech.Framework.Infrastructure.FeatureFlags;

namespace Mythetech.Framework.Components.FeatureFlags;

/// <summary>
/// Default implementation of FeatureFlagStateProvider that integrates with
/// the framework's IFeatureFlagService.
/// </summary>
public class DefaultFeatureFlagStateProvider : FeatureFlagStateProvider
{
    private readonly IFeatureFlagService _flagService;

    /// <summary>
    /// Creates a new instance of <see cref="DefaultFeatureFlagStateProvider"/>.
    /// </summary>
    /// <param name="flagService">The feature flag service.</param>
    public DefaultFeatureFlagStateProvider(IFeatureFlagService flagService)
    {
        _flagService = flagService;
    }

    /// <inheritdoc />
    public override async Task<FeatureFlagState> GetFeatureFlagState()
    {
        var flags = await _flagService.GetFeatureFlagsAsync(enabledOnly: true);
        var state = new FeatureFlagState(flags.Select(f => f.Feature).ToList());
        return state;
    }

    /// <summary>
    /// Called when flags change to notify all CascadingFeatureFlagState components to re-render.
    /// </summary>
    public void NotifyStateChanged()
    {
        NotifyFeatureFlagStateChanged(GetFeatureFlagState());
    }
}
