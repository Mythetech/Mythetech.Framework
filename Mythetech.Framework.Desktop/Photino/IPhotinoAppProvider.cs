using Photino.Blazor;

namespace Mythetech.Framework.Desktop.Photino;

/// <summary>
/// Provides an instance of a running Photino Blazor App so that components can access desktop methods for particular use cases like file system access
/// </summary>
public interface IPhotinoAppProvider
{
    /// <summary>
    /// The instance of the currently running Photino Desktop Blazor App
    /// </summary>
    public PhotinoBlazorApp Instance { get; }
}

/// <summary>
/// Internal implementation of the provider
/// </summary>
internal class PhotinoAppProvider : IPhotinoAppProvider
{
    public PhotinoBlazorApp Instance { get; set; }
}