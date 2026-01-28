using Hermes.Blazor;

namespace Mythetech.Framework.Desktop.Hermes;

/// <summary>
/// Provides an instance of a running Hermes Blazor App so that components can access desktop methods for particular use cases like file system access
/// </summary>
public interface IHermesAppProvider
{
    /// <summary>
    /// The instance of the currently running Hermes Desktop Blazor App
    /// </summary>
    HermesBlazorApp Instance { get; }
}

/// <summary>
/// Internal implementation of the provider
/// </summary>
internal class HermesAppProvider : IHermesAppProvider
{
    public HermesBlazorApp Instance { get; set; } = null!;
}
