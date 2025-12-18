using Microsoft.Extensions.DependencyInjection;
using Mythetech.Components.Desktop.Photino;

namespace Mythetech.Components.Desktop;

/// <summary>
/// Generic desktop service registration extensions
/// </summary>
public static class DesktopRegistrationExtensions
{
    /// <summary>
    /// Adds required desktop services for a given host
    /// </summary>
    public static IServiceCollection AddDesktopServices(this IServiceCollection services, DesktopHost host = DesktopHost.Photino)
    {
        switch (host)
        {
            case DesktopHost.Photino:
                services.AddPhotinoServices();
                break;
            default:
                throw new ArgumentException("Invalid host type", nameof(host));
        }

        return services;
    }
}