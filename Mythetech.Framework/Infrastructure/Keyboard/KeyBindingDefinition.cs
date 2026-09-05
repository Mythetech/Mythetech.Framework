using Mythetech.Framework.Infrastructure.MessageBus;

namespace Mythetech.Framework.Infrastructure.Keyboard;

/// <summary>
/// A shortcut action declared by an application.
/// </summary>
/// <param name="Id">Stable identifier, also the persistence key for user overrides.</param>
/// <param name="DisplayName">Label shown in the shortcuts settings section.</param>
/// <param name="Description">Optional secondary text shown beneath the label.</param>
/// <param name="Category">Group heading in the settings section.</param>
/// <param name="DefaultBinding">The shortcut this action ships with, or null to ship unbound.</param>
/// <param name="Handler">Invoked when the shortcut fires.</param>
public sealed record KeyBindingDefinition(
    string Id,
    string DisplayName,
    string? Description,
    string Category,
    KeyBinding? DefaultBinding,
    Func<IMessageBus, CancellationToken, Task> Handler);
