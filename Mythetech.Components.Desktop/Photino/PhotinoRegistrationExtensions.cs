using Microsoft.Extensions.DependencyInjection;
using Mythetech.Components.Infrastructure;
using Photino.Blazor;

namespace Mythetech.Components.Desktop.Photino;

/// <summary>
/// Photino Specific App Registration Methods
/// </summary>
public static class PhotinoRegistrationExtensions
{
    /// <summary>
    /// Registers Photino specific desktop implementations
    /// </summary>
    public static IServiceCollection AddPhotinoServices(this IServiceCollection services)
    {
        services.AddSingleton<IPhotinoAppProvider, PhotinoAppProvider>();
        services.AddTransient<IFileSaveService, PhotinoInteropFileSaveService>();

        return services;
    }

    /// <summary>
    /// Registers an instance of the running Photino App into the DI container for interop options
    /// </summary>
    public static PhotinoBlazorApp RegisterProvider(this PhotinoBlazorApp app, IServiceProvider provider)
    {
        var appProvider = ((PhotinoAppProvider)provider.GetRequiredService<IPhotinoAppProvider>());

        appProvider.Instance = app;

        return app;
    }
}