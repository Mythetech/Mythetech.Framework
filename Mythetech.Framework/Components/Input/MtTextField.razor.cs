using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Mythetech.Framework.Components.Input;

/// <summary>
/// A compact, styled text field component with support for adornments, labels, validation, and multiline input.
/// </summary>
public partial class MtTextField<T>
{
    private ElementReference _elementRef;
    private string _text = "";
    private bool _focused;

    /// <summary>The current value of the text field.</summary>
    [Parameter] public T? Value { get; set; }

    /// <summary>Callback fired when the value changes.</summary>
    [Parameter] public EventCallback<T> ValueChanged { get; set; }

    /// <summary>Placeholder text shown when the field is empty.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>Floating label displayed on the field border.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>Position of the adornment icon (Start, End, or None).</summary>
    [Parameter] public Adornment Adornment { get; set; } = Adornment.None;

    /// <summary>Icon string for the adornment.</summary>
    [Parameter] public string? AdornmentIcon { get; set; }

    /// <summary>Color of the adornment icon.</summary>
    [Parameter] public Color AdornmentColor { get; set; } = Color.Default;

    /// <summary>Callback fired when the adornment icon is clicked.</summary>
    [Parameter] public EventCallback<MouseEventArgs> OnAdornmentClick { get; set; }

    /// <summary>When true, value updates fire on every keystroke. When false, updates fire on blur/change.</summary>
    [Parameter] public bool Immediate { get; set; } = true;

    /// <summary>Callback fired on key down events.</summary>
    [Parameter] public EventCallback<KeyboardEventArgs> OnKeyDown { get; set; }

    /// <summary>Callback fired when the field loses focus.</summary>
    [Parameter] public EventCallback<FocusEventArgs> OnBlur { get; set; }

    /// <summary>Additional CSS class(es) applied to the root element.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Inline style applied to the root element.</summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>When true, displays the field in an error state.</summary>
    [Parameter] public bool Error { get; set; }

    /// <summary>Error message displayed below the field when in error state.</summary>
    [Parameter] public string? ErrorText { get; set; }

    /// <summary>
    /// Number of visible text lines. Values greater than 1 render a textarea, which will be taller than the compact single-line style.
    /// </summary>
    [Parameter] public int Lines { get; set; } = 1;

    /// <summary>When true, the field is disabled and non-interactive.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>When true, the field is read-only.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>When true, a clear button appears when the field has a value.</summary>
    [Parameter] public bool Clearable { get; set; }

    /// <summary>When true, the field receives focus on first render.</summary>
    [Parameter] public bool AutoFocus { get; set; }

    /// <summary>The HTML input type (e.g. text, password, email). Ignored for multiline (Lines > 1).</summary>
    [Parameter] public InputType InputType { get; set; } = InputType.Text;

    /// <summary>Additional HTML attributes applied to the input/textarea element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? UserAttributes { get; set; }

    private int _inputSize => Placeholder?.Length ?? 20;

    private string _inputTypeString => InputType switch
    {
        InputType.Password => "password",
        InputType.Email => "email",
        InputType.Number => "number",
        InputType.Telephone => "tel",
        InputType.Url => "url",
        InputType.Search => "search",
        InputType.Color => "color",
        InputType.Date => "date",
        InputType.DateTimeLocal => "datetime-local",
        InputType.Month => "month",
        InputType.Time => "time",
        InputType.Week => "week",
        InputType.Hidden => "hidden",
        _ => "text"
    };

    private string _rootClass
    {
        get
        {
            var classes = new List<string>();
            if (_focused) classes.Add("mt-tf-focused");
            if (Disabled) classes.Add("mt-tf-disabled");
            if (Error) classes.Add("mt-tf-error-state");
            if (!string.IsNullOrEmpty(Label)) classes.Add("mt-tf-has-label");
            if (Adornment == Adornment.Start) classes.Add("mt-tf-adorned-start");
            if (Adornment == Adornment.End || Clearable) classes.Add("mt-tf-adorned-end");
            return string.Join(" ", classes);
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _text = Value?.ToString() ?? "";
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && AutoFocus)
        {
            await _elementRef.FocusAsync();
        }
    }

    private async Task OnInputAsync(ChangeEventArgs e)
    {
        _text = e.Value?.ToString() ?? "";
        if (Immediate)
        {
            await UpdateValueAsync();
        }
    }

    private async Task OnChangeAsync(ChangeEventArgs e)
    {
        _text = e.Value?.ToString() ?? "";
        if (!Immediate)
        {
            await UpdateValueAsync();
        }
    }

    private async Task UpdateValueAsync()
    {
        if (typeof(T) == typeof(string))
        {
            await ValueChanged.InvokeAsync((T)(object)_text);
        }
        else
        {
            try
            {
                var converted = (T)Convert.ChangeType(_text, typeof(T));
                await ValueChanged.InvokeAsync(converted);
            }
            catch (FormatException)
            {
            }
        }
    }

    private void OnFocusInternal()
    {
        _focused = true;
    }

    private async Task OnBlurInternal(FocusEventArgs e)
    {
        _focused = false;
        if (!Immediate)
        {
            await UpdateValueAsync();
        }
        await OnBlur.InvokeAsync(e);
    }

    private async Task HandleClearAsync()
    {
        _text = "";
        if (typeof(T) == typeof(string))
        {
            await ValueChanged.InvokeAsync((T)(object)"");
        }
        else
        {
            await ValueChanged.InvokeAsync(default);
        }
        await _elementRef.FocusAsync();
    }

    /// <summary>Sets focus to the input element.</summary>
    public ValueTask FocusAsync() => _elementRef.FocusAsync();

    /// <summary>Clears the field value and refocuses the input.</summary>
    public async Task ClearAsync() => await HandleClearAsync();

    /// <summary>Selects the input element (focuses it).</summary>
    public ValueTask SelectAsync() => _elementRef.FocusAsync();
}
