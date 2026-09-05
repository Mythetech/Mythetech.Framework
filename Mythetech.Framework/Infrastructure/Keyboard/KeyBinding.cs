using MudBlazor.Utilities;

namespace Mythetech.Framework.Infrastructure.Keyboard;

/// <summary>
/// A key plus its modifiers. <see cref="Ctrl"/> is the platform accelerator:
/// Ctrl on Windows and Linux, Cmd on macOS.
/// </summary>
/// <param name="Key">The key, expressed as a DOM <c>e.code</c> value.</param>
/// <param name="Ctrl">Whether the platform accelerator is required.</param>
/// <param name="Shift">Whether Shift is required.</param>
/// <param name="Alt">Whether Alt is required.</param>
public sealed record KeyBinding(JsKey Key, bool Ctrl = false, bool Shift = false, bool Alt = false)
{
    private static readonly Dictionary<string, string> SymbolTokens = new(StringComparer.Ordinal)
    {
        ["Backquote"] = "`",
        ["Minus"] = "-",
        ["Equal"] = "=",
        ["BracketLeft"] = "[",
        ["BracketRight"] = "]",
        ["Backslash"] = "\\",
        ["Semicolon"] = ";",
        ["Quote"] = "'",
        ["Comma"] = ",",
        ["Period"] = ".",
        ["Slash"] = "/",
    };

    /// <summary>Creates a binding using the platform accelerator.</summary>
    public static KeyBinding CtrlOrCmd(JsKey key, bool shift = false, bool alt = false)
        => new(key, Ctrl: true, Shift: shift, Alt: alt);

    /// <summary>Creates a binding with no modifiers.</summary>
    public static KeyBinding Plain(JsKey key) => new(key);

    /// <summary>
    /// Returns display tokens for rendering through <c>MtKbd</c>, which maps
    /// "Cmd" to the platform symbol.
    /// </summary>
    public IReadOnlyList<string> ToTokens()
    {
        var tokens = new List<string>(4);

        if (Ctrl)
        {
            tokens.Add("Cmd");
        }

        if (Shift)
        {
            tokens.Add("Shift");
        }

        if (Alt)
        {
            tokens.Add("Alt");
        }

        tokens.Add(KeyToToken(Key));
        return tokens;
    }

    private static string KeyToToken(JsKey key)
    {
        var name = key.ToString();

        if (name.Length == 4 && name.StartsWith("Key", StringComparison.Ordinal))
        {
            return name[3..];
        }

        if (name.StartsWith("Digit", StringComparison.Ordinal))
        {
            return name[5..];
        }

        return SymbolTokens.GetValueOrDefault(name, name);
    }
}
