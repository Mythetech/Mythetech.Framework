namespace Mythetech.Framework.Infrastructure.Keyboard;

/// <summary>
/// A declared action paired with the binding currently in effect.
/// </summary>
/// <param name="Definition">The declared action.</param>
/// <param name="CurrentBinding">The binding in effect, or null when the action is unbound.</param>
/// <param name="IsCustomized">Whether the user has an override stored for this action.</param>
public sealed record ResolvedKeyBinding(
    KeyBindingDefinition Definition,
    KeyBinding? CurrentBinding,
    bool IsCustomized)
{
    /// <summary>The action's stable id.</summary>
    public string Id => Definition.Id;

    /// <summary>The action's display label.</summary>
    public string DisplayName => Definition.DisplayName;

    /// <summary>The action's optional secondary text.</summary>
    public string? Description => Definition.Description;

    /// <summary>The action's category.</summary>
    public string Category => Definition.Category;
}
