using Microsoft.AspNetCore.Components;

namespace Mythetech.Framework.Components.FeatureFlags;

/// <summary>
/// Represents a view that is controlled by a feature flag.
/// Use this component to conditionally render content based on whether a feature flag is enabled.
/// </summary>
/// <example>
/// <code>
/// &lt;FeatureFlagView Feature="NewDashboard"&gt;
///     &lt;Active&gt;New dashboard content&lt;/Active&gt;
///     &lt;Inactive&gt;Legacy dashboard content&lt;/Inactive&gt;
/// &lt;/FeatureFlagView&gt;
/// </code>
/// </example>
public class FeatureFlagView : FeatureFlagCore
{
    private string[]? _featureFlagData;

    /// <summary>
    /// Gets or sets the feature that determines access to the content.
    /// If the feature is enabled, the Active content is rendered; otherwise, the Inactive content is rendered.
    /// </summary>
    [Parameter]
    public string? Feature { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _featureFlagData = Feature != null ? [Feature] : [];
    }

    /// <inheritdoc />
    protected override string[] GetFeatureFlagData()
        => _featureFlagData ?? [];
}
