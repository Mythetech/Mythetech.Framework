namespace Mythetech.Framework.Components.FeatureFlags;

/// <summary>
/// Provides information about the currently active feature flags, if any.
/// </summary>
public class FeatureFlagState
{
    private readonly List<string> _features = [];

    /// <summary>
    /// Constructs an instance of <see cref="FeatureFlagState"/> with a single feature.
    /// </summary>
    public FeatureFlagState(string feature)
    {
        if (!_features.Contains(feature))
        {
            _features.Add(feature);
        }
    }

    /// <summary>
    /// Constructs an instance of <see cref="FeatureFlagState"/> with multiple features.
    /// </summary>
    public FeatureFlagState(List<string> features)
    {
        _features = features;
    }

    /// <summary>
    /// Gets the currently active feature flags.
    /// </summary>
    public List<string> FeatureFlags => _features;
}
