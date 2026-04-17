namespace Mythetech.Framework.Components.Kbd;

/// <summary>
/// Detects the current platform for keyboard symbol rendering.
/// Desktop and WebAssembly projects provide platform-specific implementations;
/// the core framework provides a <see cref="DefaultPlatformDetector"/> fallback.
/// </summary>
public interface IPlatformDetector
{
    /// <summary>True when running on macOS (native or browser on Mac).</summary>
    bool IsMacOS { get; }
}
