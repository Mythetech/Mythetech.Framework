namespace Mythetech.Framework.Infrastructure.Settings;

/// <summary>
/// Attribute to decorate settings properties with display metadata.
/// Properties without this attribute will not be rendered in the UI.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class SettingAttribute : Attribute
{
    /// <summary>
    /// Display label for this setting shown in the UI.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Optional description/help text shown below the control.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Group name for organizing related settings (e.g., "Font", "Display", "Behavior").
    /// Settings with the same group are rendered together.
    /// </summary>
    public string? Group { get; set; }

    /// <summary>
    /// Sort order within the group (lower values appear first).
    /// </summary>
    public int Order { get; set; } = 100;

    /// <summary>
    /// For numeric settings: minimum allowed value.
    /// When both Min and Max are set, renders as a slider.
    /// </summary>
    public double Min { get; set; } = double.NaN;

    /// <summary>
    /// For numeric settings: maximum allowed value.
    /// When both Min and Max are set, renders as a slider.
    /// </summary>
    public double Max { get; set; } = double.NaN;

    /// <summary>
    /// For numeric settings: step/increment value for sliders.
    /// </summary>
    public double Step { get; set; } = 1;

    /// <summary>
    /// For string settings: comma-separated list of options.
    /// When set, renders as a dropdown select instead of text field.
    /// Example: "none,boundary,selection,trailing,all"
    /// </summary>
    public string? Options { get; set; }

    /// <summary>
    /// Whether this setting has valid min/max range (for slider rendering).
    /// </summary>
    public bool HasRange => !double.IsNaN(Min) && !double.IsNaN(Max);
}
