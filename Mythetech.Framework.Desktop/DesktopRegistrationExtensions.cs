using Microsoft.Extensions.DependencyInjection;
using Mythetech.Framework.Components.Kbd;
using Mythetech.Framework.Desktop.Components;
using Mythetech.Framework.Desktop.Environment;
using Mythetech.Framework.Desktop.Hermes;
using Mythetech.Framework.Desktop.Photino;
using Mythetech.Framework.Desktop.Services;
using Mythetech.Framework.Desktop.Storage.LiteDb;
using Mythetech.Framework.Infrastructure;
using Mythetech.Framework.Infrastructure.Environment;
using Mythetech.Framework.Infrastructure.Files;
using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Framework.Infrastructure.Shell;

namespace Mythetech.Framework.Desktop;

public static class DesktopRegistrationExtensions
{
    public static IServiceCollection AddDesktopServices(this IServiceCollection services, DesktopHost host = DesktopHost.Photino)
    {
        switch (host)
        {
            case DesktopHost.Photino:
                services.AddPhotinoServices();
                break;
            case DesktopHost.Hermes:
                services.AddHermesServices();
                break;
            default:
                throw new ArgumentException("Invalid host type", nameof(host));
        }

        services.AddLinkOpenService();
        services.AddPluginStorage();
        services.AddDesktopAssetLoader();
        services.AddShowFileService();
        services.AddShellExecutor();
        services.AddSingleton<IPlatformDetector, DesktopPlatformDetector>();

        return services;
    }

    public static IServiceCollection AddDesktopAssetLoader(this IServiceCollection services)
    {
        var existing = services.FirstOrDefault(d => d.ServiceType == typeof(IPluginAssetLoader));
        if (existing != null)
        {
            services.Remove(existing);
        }

        services.AddScoped<IPluginAssetLoader, DesktopPluginAssetLoader>();

        return services;
    }

    public static IServiceCollection AddLinkOpenService(this IServiceCollection services)
    {
        services.AddTransient<ILinkOpenService, LinkOpenService>();

        return services;
    }

    public static IServiceCollection AddShowFileService(this IServiceCollection services)
    {
        services.AddTransient<IShowFileService, ShowFileService>();

        return services;
    }

    public static IServiceCollection AddShellExecutor(this IServiceCollection services)
    {
        services.AddSingleton<IShellExecutor, ShellExecutor>();

        return services;
    }

    public static IServiceCollection AddRuntimeEnvironment(this IServiceCollection services, bool? development = false, Version? version = null, string baseAddress = "app://")
    {
        return services.AddRuntimeEnvironment(development is true ? DesktopRuntimeEnvironment.Development(version, baseAddress) : DesktopRuntimeEnvironment.Production(version, baseAddress));
    }

    public static IServiceCollection AddRuntimeEnvironment(this IServiceCollection services, DesktopRuntimeEnvironment environment)
    {
        services.AddSingleton<IRuntimeEnvironment>(environment);
        return services;
    }

    public static IServiceCollection AddFileOperations(this IServiceCollection services)
    {
        services.AddSingleton<IFileOperations, SystemFileOperations>();

        services.AddSingleton<IFileReader>(sp => sp.GetRequiredService<IFileOperations>());
        services.AddSingleton<IFileWriter>(sp => sp.GetRequiredService<IFileOperations>());
        services.AddSingleton<IFileManager>(sp => sp.GetRequiredService<IFileOperations>());

        return services;
    }

    public static IServiceCollection AddDirectoryOperations(this IServiceCollection services)
    {
        services.AddSingleton<IDirectoryOperations, SystemDirectoryOperations>();

        return services;
    }

    public static IServiceCollection AddFileSystemOperations(this IServiceCollection services)
    {
        services.AddFileOperations();
        services.AddDirectoryOperations();

        return services;
    }
}
