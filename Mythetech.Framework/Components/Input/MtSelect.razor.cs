using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Mythetech.Framework.Components.Input;

/// <summary>
/// A custom select/dropdown component with keyboard navigation, outlined label support, and adornments.
/// </summary>
/// <typeparam name="T">The type of the selected value.</typeparam>
public partial class MtSelect<T>
{
    private ElementReference _rootRef;
    private bool _isOpen;
    private bool _focused;
    private readonly List<MtSelectItem<T>> _items = new();
    private MtSelectItem<T>? _selectedItem;
    private MtSelectItem<T>? _highlightedItem;

    /// <summary>The currently selected value.</summary>
    [Parameter] public T? Value { get; set; }

    /// <summary>Callback invoked when the selected value changes.</summary>
    [Parameter] public EventCallback<T> ValueChanged { get; set; }

    /// <summary>The outlined label displayed on the border.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>Placeholder text shown when no item is selected.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>Child content containing <see cref="MtSelectItem{T}"/> elements.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Whether the select is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>The adornment position.</summary>
    [Parameter] public Adornment Adornment { get; set; } = Adornment.None;

    /// <summary>Icon to display as the adornment.</summary>
    [Parameter] public string? AdornmentIcon { get; set; }

    /// <summary>Color of the adornment icon.</summary>
    [Parameter] public Color AdornmentColor { get; set; } = Color.Default;

    /// <summary>Additional CSS class for the root select element.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Inline style for the root select element.</summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>Additional CSS class for the popover.</summary>
    [Parameter] public string? PopoverClass { get; set; }

    /// <summary>Additional CSS class for the dropdown list container.</summary>
    [Parameter] public string? ListClass { get; set; }

    /// <summary>Custom function to convert the value to a display string.</summary>
    [Parameter] public Func<T?, string>? ToStringFunc { get; set; }

    /// <summary>Unmatched attributes to pass to the root element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? UserAttributes { get; set; }

    private string _rootClass
    {
        get
        {
            var classes = new List<string>();
            if (_focused || _isOpen) classes.Add("mt-sel-focused");
            if (_isOpen) classes.Add("mt-sel-open");
            if (Disabled) classes.Add("mt-sel-disabled");
            if (!string.IsNullOrEmpty(Label)) classes.Add("mt-sel-has-label");
            if (Adornment == Adornment.Start) classes.Add("mt-sel-adorned-start");
            return string.Join(" ", classes);
        }
    }

    private string _computedListClass
    {
        get
        {
            var baseClass = "pa-1 rounded";
            return string.IsNullOrEmpty(ListClass) ? baseClass : $"{baseClass} {ListClass}";
        }
    }

    internal void AddItem(MtSelectItem<T> item)
    {
        _items.Add(item);
        SyncSelection();
        InvokeAsync(StateHasChanged);
    }

    internal void RemoveItem(MtSelectItem<T> item)
    {
        _items.Remove(item);
        if (_selectedItem == item) _selectedItem = null;
        if (_highlightedItem == item) _highlightedItem = null;
        InvokeAsync(StateHasChanged);
    }

    private void SyncSelection()
    {
        _selectedItem = _items.FirstOrDefault(i =>
            EqualityComparer<T>.Default.Equals(i.Value, Value));
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        SyncSelection();
    }

    private void ToggleDropdown()
    {
        if (Disabled) return;
        _isOpen = !_isOpen;
        if (_isOpen)
        {
            _highlightedItem = _selectedItem ?? _items.FirstOrDefault();
        }
    }

    private void CloseDropdown()
    {
        _isOpen = false;
    }

    private async Task SelectItemAsync(MtSelectItem<T> item)
    {
        if (item.Disabled) return;
        _selectedItem = item;
        _isOpen = false;
        Value = item.Value;
        await ValueChanged.InvokeAsync(item.Value);
    }

    private bool IsSelected(MtSelectItem<T> item) =>
        EqualityComparer<T>.Default.Equals(item.Value, Value);

    private void OnFocusInternal()
    {
        _focused = true;
    }

    private void OnBlurInternal()
    {
        _focused = false;
    }

    private async Task HandleKeyDownAsync(KeyboardEventArgs e)
    {
        switch (e.Key)
        {
            case "Enter" or " ":
                if (_isOpen && _highlightedItem != null)
                    await SelectItemAsync(_highlightedItem);
                else
                    ToggleDropdown();
                break;
            case "Escape":
                CloseDropdown();
                break;
            case "ArrowDown":
                if (!_isOpen)
                {
                    ToggleDropdown();
                }
                else
                {
                    MoveHighlight(1);
                }
                break;
            case "ArrowUp":
                if (_isOpen) MoveHighlight(-1);
                break;
        }
    }

    private void MoveHighlight(int direction)
    {
        if (_items.Count == 0) return;

        var currentIndex = _highlightedItem != null ? _items.IndexOf(_highlightedItem) : -1;
        var newIndex = currentIndex + direction;

        if (newIndex < 0) newIndex = _items.Count - 1;
        else if (newIndex >= _items.Count) newIndex = 0;

        _highlightedItem = _items[newIndex];
    }
}
