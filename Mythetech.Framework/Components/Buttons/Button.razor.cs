using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using Mythetech.Framework.Enums;

namespace Mythetech.Framework.Components.Buttons;

/// <summary>
/// Custom Button component wrapping MudButton with framework defaults.
/// </summary>
public partial class Button : MudComponentBase
{
    /// <summary>
    /// Text to display in the button. If ChildContent is provided, it takes precedence.
    /// </summary>
    [Parameter]
    public string? Text { get; set; }

    /// <summary>
    /// Child content to render inside the button.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Variant of the button. Default is Filled.
    /// </summary>
    [Parameter]
    public Variant Variant { get; set; } = Variant.Filled;

    /// <summary>
    /// Color theme of the button. Default is Primary.
    /// </summary>
    [Parameter]
    public Color Color { get; set; } = Color.Primary;

    /// <summary>
    /// Roundedness of the button corners. Default is Large.
    /// </summary>
    [Parameter]
    public Roundedness Roundedness { get; set; } = Roundedness.Large;

    /// <summary>
    /// Icon placed before the text/content.
    /// </summary>
    [Parameter]
    public string? StartIcon { get; set; }

    /// <summary>
    /// Icon placed after the text/content.
    /// </summary>
    [Parameter]
    public string? EndIcon { get; set; }

    /// <summary>
    /// Size of the icons.
    /// </summary>
    [Parameter]
    public Size IconSize { get; set; } = Size.Medium;

    /// <summary>
    /// Color of the icons.
    /// </summary>
    [Parameter]
    public Color IconColor { get; set; } = Color.Inherit;

    /// <summary>
    /// Size of the button.
    /// </summary>
    [Parameter]
    public Size Size { get; set; } = Size.Medium;

    /// <summary>
    /// If true, the button is disabled.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// If true, the button will take up the full width of its container.
    /// </summary>
    [Parameter]
    public bool FullWidth { get; set; }

    /// <summary>
    /// The type of button (button, submit, reset).
    /// </summary>
    [Parameter]
    public ButtonType ButtonType { get; set; } = ButtonType.Button;

    /// <summary>
    /// URL to navigate to when the button is clicked.
    /// </summary>
    [Parameter]
    public string? Href { get; set; }

    /// <summary>
    /// Target for the href (_blank, _self, etc.).
    /// </summary>
    [Parameter]
    public string? Target { get; set; }

    /// <summary>
    /// Callback invoked when the button is clicked.
    /// </summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Gets the combined CSS class including roundedness.
    /// </summary>
    protected string CombinedClass => $"{Roundedness.Class()} {Class}".Trim();
}
