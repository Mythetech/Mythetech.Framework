namespace Mythetech.Framework.Infrastructure.Keyboard;

/// <summary>
/// A shortcut the user can't assign to any action in <see cref="Scope"/>, such as a key an editor handles itself.
/// </summary>
/// <param name="Scope">The scope the shortcut is reserved in.</param>
/// <param name="Binding">The reserved shortcut.</param>
public sealed record ReservedKeyBinding(KeyBindingScope Scope, KeyBinding Binding);
