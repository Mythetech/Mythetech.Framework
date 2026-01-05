namespace Mythetech.Framework.Infrastructure.Plugins;

/// <summary>
/// Represents the hosting platform for the application.
/// This is the runtime environment (Desktop/WebAssembly), not the operating system.
/// </summary>
public enum Platform
{
    /// <summary>
    /// Desktop application (Photino/WebView-based)
    /// </summary>
    Desktop,

    /// <summary>
    /// WebAssembly running in browser
    /// </summary>
    WebAssembly
}
