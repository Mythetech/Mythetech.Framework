using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Mythetech.Framework.Infrastructure.FeatureFlags;

namespace Mythetech.Framework.Components.FeatureFlags;

/// <summary>
/// A base class for components that display differing content depending on active feature flags.
/// </summary>
public abstract class FeatureFlagCore : ComponentBase
{
    private FeatureFlagState? _currentFeatureFlagState;
    private bool? _isActive;

    /// <summary>
    /// The content that will be displayed if the feature flag is active.
    /// </summary>
    [Parameter]
    public RenderFragment<FeatureFlagState>? ChildContent { get; set; }

    /// <summary>
    /// The content that will be displayed if the feature flag is not active.
    /// </summary>
    [Parameter]
    public RenderFragment<FeatureFlagState>? Inactive { get; set; }

    /// <summary>
    /// The content that will be displayed if the feature flag is active.
    /// If you specify a value for this parameter, do not also specify a value for <see cref="ChildContent"/>.
    /// </summary>
    [Parameter]
    public RenderFragment<FeatureFlagState>? Active { get; set; }

    [CascadingParameter]
    private Task<FeatureFlagState>? FeatureFlagState { get; set; }

    [Inject]
    private IFeatureFlagService FeatureFlagService { get; set; } = default!;

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (_isActive ?? false)
        {
            var active = Active ?? ChildContent;
            builder.AddContent(0, active?.Invoke(_currentFeatureFlagState!));
        }
        else
        {
            builder.AddContent(0, Inactive?.Invoke(_currentFeatureFlagState!));
        }
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        // 'ChildContent' is for convenience in basic cases, and 'Active' for symmetry
        // with 'Inactive' in other cases. Besides naming, they are equivalent. To avoid
        // confusion, explicitly prevent the case where both are supplied.
        if (ChildContent != null && Active != null)
        {
            throw new InvalidOperationException($"Do not specify both '{nameof(Active)}' and '{nameof(ChildContent)}'.");
        }

        if (FeatureFlagState == null)
        {
            throw new InvalidOperationException($"Feature flags require a cascading parameter of type Task<{nameof(FeatureFlagState)}>.");
        }

        // Clear the previous result of feature flag evaluation
        _isActive = null;

        _currentFeatureFlagState = await FeatureFlagState;
        _isActive = await IsActiveAsync();
    }

    /// <summary>
    /// Gets the feature flag data.
    /// </summary>
    protected abstract string[]? GetFeatureFlagData();

    private async Task<bool> IsActiveAsync()
    {
        var flags = GetFeatureFlagData();
        if (flags == null)
        {
            return false;
        }

        foreach (var flag in flags)
        {
            if (await FeatureFlagService.IsFeatureFlagEnabled(flag) == false)
            {
                return false;
            }
        }

        return true;
    }
}
