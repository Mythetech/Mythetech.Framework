using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Mythetech.Framework.Components.Input;

/// <summary>
/// A compact numeric input with increment/decrement spinner buttons, supporting min/max bounds and custom step.
/// </summary>
public partial class MtNumericField<T> where T : struct, IComparable<T>
{
    private ElementReference _elementRef;
    private string _inputText = "";
    private bool _focused;


    /// <summary>The current value of the field.</summary>
    [Parameter] public T? Value { get; set; }

    /// <summary>Callback fired when the value changes.</summary>
    [Parameter] public EventCallback<T?> ValueChanged { get; set; }

    /// <summary>Minimum allowed value.</summary>
    [Parameter] public T? Min { get; set; }

    /// <summary>Maximum allowed value.</summary>
    [Parameter] public T? Max { get; set; }

    /// <summary>Amount to increment or decrement per step. Defaults to 1.</summary>
    [Parameter] public T? Step { get; set; }

    /// <summary>Floating label displayed on the field border.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>Placeholder text shown when the field is empty.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>Position of the adornment icon (Start, End, or None).</summary>
    [Parameter] public Adornment Adornment { get; set; } = Adornment.None;

    /// <summary>Icon string for the adornment.</summary>
    [Parameter] public string? AdornmentIcon { get; set; }

    /// <summary>Color of the adornment icon.</summary>
    [Parameter] public Color AdornmentColor { get; set; } = Color.Default;

    /// <summary>Callback fired when the adornment icon is clicked.</summary>
    [Parameter] public EventCallback<MouseEventArgs> OnAdornmentClick { get; set; }

    /// <summary>When true, displays the field in an error state.</summary>
    [Parameter] public bool Error { get; set; }

    /// <summary>Error message displayed below the field when in error state.</summary>
    [Parameter] public string? ErrorText { get; set; }

    /// <summary>When true, the field is disabled and non-interactive.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>When true, the field is read-only.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>When true, a clear button appears when the field has a value.</summary>
    [Parameter] public bool Clearable { get; set; }

    /// <summary>When true, the field receives focus on first render.</summary>
    [Parameter] public bool AutoFocus { get; set; }

    /// <summary>When true, value updates fire on every keystroke. When false, updates fire on blur/change.</summary>
    [Parameter] public bool Immediate { get; set; } = true;

    /// <summary>Callback fired when the field loses focus.</summary>
    [Parameter] public EventCallback<FocusEventArgs> OnBlur { get; set; }

    /// <summary>Callback fired on key down events.</summary>
    [Parameter] public EventCallback<KeyboardEventArgs> OnKeyDown { get; set; }

    /// <summary>Additional CSS class(es) applied to the root element.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Inline style applied to the root element.</summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>Additional HTML attributes applied to the input element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? UserAttributes { get; set; }

    private T EffectiveStep => Step ?? (T)Convert.ChangeType(1, typeof(T));

    private bool IsAtMax => Value.HasValue && Max.HasValue && Value.Value.CompareTo(Max.Value) >= 0;

    private bool IsAtMin => Value.HasValue && Min.HasValue && Value.Value.CompareTo(Min.Value) <= 0;

    private string _rootClass
    {
        get
        {
            var classes = new List<string>();
            if (_focused) classes.Add("mt-nf-focused");
            if (Disabled) classes.Add("mt-nf-disabled");
            if (Error) classes.Add("mt-nf-error-state");
            if (!string.IsNullOrEmpty(Label)) classes.Add("mt-nf-has-label");
            if (Adornment == Adornment.Start) classes.Add("mt-nf-adorned-start");
            if (Adornment == Adornment.End) classes.Add("mt-nf-adorned-end");
            return string.Join(" ", classes);
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _inputText = Value.HasValue ? Value.Value.ToString() ?? "" : "";
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && AutoFocus)
        {
            await _elementRef.FocusAsync();
        }
    }

    private T Clamp(T value)
    {
        if (Min.HasValue && value.CompareTo(Min.Value) < 0) return Min.Value;
        if (Max.HasValue && value.CompareTo(Max.Value) > 0) return Max.Value;
        return value;
    }

    private T Add(T a, T b)
    {
        return (T)Convert.ChangeType(Convert.ToDecimal(a) + Convert.ToDecimal(b), typeof(T));
    }

    private T Subtract(T a, T b)
    {
        return (T)Convert.ChangeType(Convert.ToDecimal(a) - Convert.ToDecimal(b), typeof(T));
    }

    private async Task SetValueAsync(T? value)
    {
        Value = value;
        _inputText = value.HasValue ? value.Value.ToString()! : "";
        await ValueChanged.InvokeAsync(value);
    }

    internal async Task IncrementAsync()
    {
        if (Disabled || ReadOnly) return;

        if (!Value.HasValue)
        {
            var initial = Min ?? Add((T)Convert.ChangeType(0, typeof(T)), EffectiveStep);
            await SetValueAsync(Clamp(initial));
            return;
        }

        if (IsAtMax) return;

        await SetValueAsync(Clamp(Add(Value.Value, EffectiveStep)));
    }

    internal async Task DecrementAsync()
    {
        if (Disabled || ReadOnly) return;
        if (!Value.HasValue) return;
        if (IsAtMin) return;

        await SetValueAsync(Clamp(Subtract(Value.Value, EffectiveStep)));
    }

    private async Task OnInputAsync(ChangeEventArgs e)
    {
        _inputText = e.Value?.ToString() ?? "";
        if (Immediate)
        {
            await ParseAndUpdateAsync(_inputText);
        }
    }

    private async Task OnChangeAsync(ChangeEventArgs e)
    {
        _inputText = e.Value?.ToString() ?? "";
        if (!Immediate)
        {
            await ParseAndUpdateAsync(_inputText);
        }
    }

    private async Task ParseAndUpdateAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            await SetValueAsync(null);
            return;
        }

        try
        {
            var parsed = (T)Convert.ChangeType(text, typeof(T));
            await SetValueAsync(Clamp(parsed));
        }
        catch (FormatException)
        {
        }
        catch (OverflowException)
        {
        }
    }

    private void OnFocusInternal()
    {
        _focused = true;
    }

    private async Task OnBlurInternal(FocusEventArgs e)
    {
        _focused = false;
        if (Value.HasValue)
        {
            _inputText = Value.Value.ToString()!;
        }
        await OnBlur.InvokeAsync(e);
    }

    internal async Task HandleKeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Key == "ArrowUp")
        {
            await IncrementAsync();
        }
        else if (e.Key == "ArrowDown")
        {
            await DecrementAsync();
        }

        await OnKeyDown.InvokeAsync(e);
    }

    private async Task HandleClearAsync()
    {
        await SetValueAsync(null);
        await _elementRef.FocusAsync();
    }

    /// <summary>Sets focus to the input element.</summary>
    public ValueTask FocusAsync() => _elementRef.FocusAsync();

    /// <summary>Clears the field value and refocuses the input.</summary>
    public async Task ClearAsync() => await HandleClearAsync();

    /// <summary>Selects the input element (focuses it).</summary>
    public ValueTask SelectAsync() => _elementRef.FocusAsync();
}
