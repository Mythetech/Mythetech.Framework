using Mythetech.Framework.Infrastructure.MessageBus;

namespace Mythetech.Framework.Infrastructure.Keyboard;

/// <summary>
/// Fluent surface for declaring shortcut actions inside <c>AddKeyBindings</c>.
/// </summary>
public sealed class KeyBindingBuilder
{
    private readonly List<KeyBindingDefinition> _definitions = new();
    private string _category = "General";

    /// <summary>Sets the category applied to every subsequent <see cref="Add"/> call.</summary>
    public KeyBindingBuilder Category(string category)
    {
        _category = category;
        return this;
    }

    /// <summary>
    /// Declares a shortcut action. Pass null for <paramref name="defaultBinding"/>
    /// to ship the action unbound so the user can assign a key later.
    /// </summary>
    public KeyBindingBuilder Add(
        string id,
        string displayName,
        KeyBinding? defaultBinding,
        Func<IMessageBus, CancellationToken, Task> handler,
        string? description = null)
    {
        _definitions.Add(new KeyBindingDefinition(id, displayName, description, _category, defaultBinding, handler));
        return this;
    }

    internal IReadOnlyList<KeyBindingDefinition> Build() => _definitions;
}
