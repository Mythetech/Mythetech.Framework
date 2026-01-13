using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using MudBlazor.Extensions;
using MudBlazor.Services;
using MudBlazor.Utilities;

namespace Mythetech.Framework.Components.Switch;

/// <summary>
/// Switch component with Material Design 3 specifications.
/// </summary>
/// <typeparam name="T">The value type (typically bool).</typeparam>
public partial class Switch<T> : MudBooleanInput<T>
{
    /// <inheritdoc />
    protected override string? Classname =>
        new CssBuilder("mud-switch-m3")
            .AddClass($"mud-disabled", GetDisabledState())
            .AddClass($"mud-readonly", GetReadOnlyState())
            .AddClass($"mud-switch-label-{Size.ToDescriptionString()}")
            .AddClass($"mud-input-content-placement-{ConvertPlacement(LabelPlacement).ToDescriptionString()}")
            .AddClass(Class)
            .Build();

    /// <summary>
    /// CSS class for the switch span wrapper.
    /// </summary>
    protected string? SwitchSpanClassname =>
        new CssBuilder("mud-switch-span-m3 mud-flip-x-rtl")
            .AddClass("mud-switch-child-content-m3", ChildContent != null || !string.IsNullOrEmpty(Label))
            .Build();

    /// <summary>
    /// CSS class for the switch control.
    /// </summary>
    protected string? SwitchClassname =>
        new CssBuilder("mud-button-root mud-icon-button mud-switch-base-m3")
            .AddClass($"mud-ripple mud-ripple-switch", Ripple && !GetReadOnlyState() && !GetDisabledState())
            .AddClass($"mud-{Color.ToDescriptionString()}-text hover:mud-{Color.ToDescriptionString()}-hover", BoolValue == true)
            .AddClass($"mud-switch-disabled", GetDisabledState())
            .AddClass($"mud-readonly", GetReadOnlyState())
            .AddClass($"mud-checked", BoolValue)
            .AddClass("mud-switch-base-dense-m3", !string.IsNullOrEmpty(ThumbOffIcon))
            .Build();

    /// <summary>
    /// CSS class for the track element.
    /// </summary>
    protected string? TrackClassname =>
        new CssBuilder("mud-switch-track-m3")
            .AddClass($"mud-{Color.ToDescriptionString()}", BoolValue == true)
            .AddClass($"mud-switch-track-{Color.ToDescriptionString()}-m3")
            .Build();

    /// <summary>
    /// Style for the thumb icon.
    /// </summary>
    protected string? IconStylename =>
        new StyleBuilder()
            .AddStyle("width: 16px; height: 16px;")
            .AddStyle("color", "var(--mud-palette-background)", !string.IsNullOrEmpty(ThumbOffIcon))
            .Build();

    [Inject] private IKeyInterceptorService KeyInterceptorService { get; set; } = null!;

    /// <summary>
    /// Shows an icon on Switch's thumb when checked.
    /// </summary>
    [Parameter]
    [Category(CategoryTypes.FormComponent.Appearance)]
    public string? ThumbIcon { get; set; }

    /// <summary>
    /// Shows an icon on Switch's thumb when unchecked.
    /// </summary>
    [Parameter]
    [Category(CategoryTypes.FormComponent.Appearance)]
    public string? ThumbOffIcon { get; set; }

    /// <summary>
    /// Handles keyboard navigation for the switch.
    /// </summary>
    protected internal async Task HandleKeyDownAsync(KeyboardEventArgs obj)
    {
        if (Disabled || ReadOnly)
            return;

        switch (obj.Key)
        {
            case "ArrowLeft":
            case "Delete":
                await SetBoolValueAsync(false);
                break;
            case "ArrowRight":
            case "Enter":
            case "NumpadEnter":
                await SetBoolValueAsync(true);
                break;
            case " ":
                if (BoolValue == true)
                {
                    await SetBoolValueAsync(false);
                }
                else
                {
                    await SetBoolValueAsync(true);
                }
                break;
        }
    }

    private readonly string _elementId = "switchm3_" + Guid.NewGuid().ToString().Substring(0, 8);

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (Label == null && For != null)
            Label = For.GetLabelString();
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var options = new KeyInterceptorOptions(
                "mud-switch-base",
                [
                    new("ArrowUp", preventDown: "key+none"),
                    new("ArrowDown", preventDown: "key+none"),
                    new(" ", preventDown: "key+none", preventUp: "key+none")
                ]);

            await KeyInterceptorService.SubscribeAsync(_elementId, options, keyDown: HandleKeyDownAsync);
        }
        await base.OnAfterRenderAsync(firstRender);
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        await base.DisposeAsyncCore();

        if (IsJSRuntimeAvailable)
        {
            await KeyInterceptorService.UnsubscribeAsync(_elementId);
        }
    }
}
