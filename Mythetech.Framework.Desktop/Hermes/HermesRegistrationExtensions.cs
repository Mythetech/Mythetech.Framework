using Hermes.Blazor;
using Microsoft.Extensions.DependencyInjection;
using Mythetech.Framework.Infrastructure.Files;

namespace Mythetech.Framework.Desktop.Hermes;

/// <summary>
/// Hermes specific app registration methods
/// </summary>
public static class HermesRegistrationExtensions
{
    /// <summary>
    /// Registers Hermes specific desktop implementations
    /// </summary>
    public static IServiceCollection AddHermesServices(this IServiceCollection services)
    {
        services.AddSingleton<IHermesAppProvider, HermesAppProvider>();
        services.AddTransient<IFileSaveService, HermesInteropFileSaveService>();
        services.AddTransient<IFileOpenService, HermesInteropFileOpenService>();

        return services;
    }

    /// <summary>
    /// Registers an instance of the running Hermes App into the DI container for interop options
    /// </summary>
    public static HermesBlazorApp RegisterHermesProvider(this HermesBlazorApp app)
    {
        var provider = app.Services;

        return RegisterHermesProvider(app, provider);
    }

    /// <summary>
    /// Registers an instance of the running Hermes App into the DI container for interop options
    /// </summary>
    public static HermesBlazorApp RegisterHermesProvider(this HermesBlazorApp app, IServiceProvider provider)
    {
        var appProvider = (HermesAppProvider)provider.GetRequiredService<IHermesAppProvider>();

        appProvider.Instance = app;

        return app;
    }
}
