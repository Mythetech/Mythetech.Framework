using Mythetech.Framework.Infrastructure.MessageBus;

namespace Mythetech.Framework.Infrastructure.Keyboard;

/// <summary>
/// Fluent surface for declaring shortcut actions inside <c>AddKeyBindings</c>.
/// </summary>
public sealed class KeyBindingBuilder
{
    private readonly List<KeyBindingDefinition> _definitions = new();
    private readonly List<ReservedKeyBinding> _reserved = new();
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
        string? description = null,
        KeyBindingScope scope = KeyBindingScope.Global)
    {
        _definitions.Add(new KeyBindingDefinition(id, displayName, description, _category, defaultBinding, handler, scope));
        return this;
    }

    /// <summary>
    /// Reserves shortcuts in a scope so the user can't assign them to an action there,
    /// such as keys an editor handles itself.
    /// </summary>
    public KeyBindingBuilder Reserve(KeyBindingScope scope, params KeyBinding[] bindings)
    {
        foreach (var binding in bindings)
        {
            _reserved.Add(new ReservedKeyBinding(scope, binding));
        }

        return this;
    }

    internal IReadOnlyList<KeyBindingDefinition> Build() => _definitions;

    internal IReadOnlyList<ReservedKeyBinding> BuildReserved() => _reserved;
}
