namespace Mythetech.Framework.Components.Kbd;

/// <summary>
/// Controls platform-specific key symbol rendering in the <see cref="Kbd"/> component.
/// </summary>
public enum KbdPlatform
{
    /// <summary>Auto-detect platform via <see cref="IPlatformDetector"/>.</summary>
    Auto,

    /// <summary>Force macOS symbol rendering (⌘, ⌃, ⌥, ⇧).</summary>
    MacOS,

    /// <summary>Force default (Windows/Linux) text rendering.</summary>
    Default
}
