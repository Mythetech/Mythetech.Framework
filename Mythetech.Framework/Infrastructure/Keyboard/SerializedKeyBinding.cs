using MudBlazor.Utilities;

namespace Mythetech.Framework.Infrastructure.Keyboard;

/// <summary>
/// JSON-safe form of a <see cref="KeyBinding"/>. The key is stored by DOM code
/// name rather than by enum ordinal so persisted bindings survive a reordering
/// of <see cref="JsKey"/> in a MudBlazor upgrade.
/// </summary>
/// <param name="Key">The DOM code name, for example "KeyS".</param>
/// <param name="Ctrl">Whether the platform accelerator is required.</param>
/// <param name="Shift">Whether Shift is required.</param>
/// <param name="Alt">Whether Alt is required.</param>
public sealed record SerializedKeyBinding(string Key, bool Ctrl, bool Shift, bool Alt)
{
    /// <summary>Converts a binding to its serializable form.</summary>
    public static SerializedKeyBinding FromBinding(KeyBinding binding)
        => new(binding.Key.ToString(), binding.Ctrl, binding.Shift, binding.Alt);

    /// <summary>
    /// Converts back to a binding, or null when the stored code name is no
    /// longer recognized. A stale override resolves to unbound rather than
    /// failing the whole settings load.
    /// </summary>
    public KeyBinding? ToBinding()
        => Enum.TryParse<JsKey>(Key, ignoreCase: false, out var key) && Enum.IsDefined(typeof(JsKey), key)
            ? new KeyBinding(key, Ctrl, Shift, Alt)
            : null;
}
