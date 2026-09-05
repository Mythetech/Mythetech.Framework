namespace Mythetech.Framework.Infrastructure.Keyboard;

/// <summary>
/// Display metadata for the shortcuts settings section.
/// </summary>
public sealed class KeyBindingSettingsOptions
{
    /// <summary>Name shown in the settings navigation.</summary>
    public string DisplayName { get; set; } = "Shortcuts";

    /// <summary>Icon shown in the settings navigation.</summary>
    public string Icon { get; set; } = MythetechFrameworkIcons.Keyboard;

    /// <summary>Sort order within the settings navigation.</summary>
    public int Order { get; set; } = 60;
}
