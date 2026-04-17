namespace Mythetech.Framework.Components.Kbd;

/// <summary>
/// Fallback platform detector that always reports non-macOS.
/// Used in tests and Storybook where no platform-specific implementation is registered.
/// Desktop and WebAssembly projects override this with their own implementations.
/// </summary>
public sealed class DefaultPlatformDetector : IPlatformDetector
{
    /// <inheritdoc />
    public bool IsMacOS => false;
}
