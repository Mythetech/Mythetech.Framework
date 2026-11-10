using Microsoft.Extensions.DependencyInjection;
using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Framework.Infrastructure.Settings;

namespace Mythetech.Framework.WebAssembly.Storage.LocalStorage;

public static class LocalStorageRegistrationExtensions
{
    public static IServiceCollection AddLocalStoragePluginStorage(this IServiceCollection services)
    {
        services.AddSingleton<IPluginStorageFactory, LocalStoragePluginStorageFactory>();
        return services;
    }

    public static IServiceCollection AddLocalStorageSettingsStorage(this IServiceCollection services)
    {
        services.AddSingleton<ISettingsStorage, LocalStorageSettingsStorage>();
        return services;
    }

    public static IServiceCollection AddLocalStoragePluginStateProvider(this IServiceCollection services)
    {
        services.AddSingleton<IPluginStateProvider, LocalStoragePluginStateProvider>();
        return services;
    }
}
