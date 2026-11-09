namespace Mythetech.Framework.Infrastructure.Keyboard;

/// <summary>
/// Where a shortcut action is active.
/// </summary>
public enum KeyBindingScope
{
    /// <summary>Registered as an application-wide hotkey by <c>MtKeyBindings</c>.</summary>
    Global,

    /// <summary>Listed and rebindable in settings but handled by the editor, never registered globally.</summary>
    Editor,
}
