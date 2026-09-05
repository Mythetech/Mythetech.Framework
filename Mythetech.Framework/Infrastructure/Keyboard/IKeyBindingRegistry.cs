namespace Mythetech.Framework.Infrastructure.Keyboard;

/// <summary>
/// Holds every shortcut action declared through <c>AddKeyBindings</c>.
/// </summary>
public interface IKeyBindingRegistry
{
    /// <summary>Returns all declared definitions in registration order.</summary>
    IReadOnlyList<KeyBindingDefinition> GetAll();

    /// <summary>Returns the definition with the given id, or null when none is declared.</summary>
    KeyBindingDefinition? Get(string id);
}
