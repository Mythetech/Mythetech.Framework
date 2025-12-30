using KristofferStrube.Blazor.FileSystemAccess;
using Microsoft.Extensions.DependencyInjection;
using Mythetech.Framework.Infrastructure;
using Mythetech.Framework.Infrastructure.Plugins;

namespace Mythetech.Framework.WebAssembly;

/// <summary>
/// WebAssembly service registration extensions
/// </summary>
public static class WebAssemblyRegistrationExtensions
{
    /// <summary>
    /// Registers the link opening service for WebAssembly
    /// </summary>
    public static IServiceCollection AddLinkOpeningService(this IServiceCollection services)
    {
        services.AddTransient<ILinkOpenService, JavaScriptLinkOpenService>();
        
        return services;
    }

    /// <summary>
    /// Registers the file open service for WebAssembly using the File System Access API
    /// </summary>
    public static IServiceCollection AddFileOpenService(this IServiceCollection services)
    {
        services.AddFileSystemAccessServiceInProcess();
        services.AddTransient<IFileOpenService, FileSystemAccessFileOpenService>();

        return services;
    }

    /// <summary>
    /// Registers the file save service for WebAssembly using the File System Access API
    /// </summary>
    public static IServiceCollection AddFileSaveService(this IServiceCollection services)
    {
        //services.AddFileSystemAccessServiceInProcess();
        services.AddTransient<IFileSystemAccessServiceInProcess, FileSystemAccessServiceInProcess>().AddTransient(sp => (IFileSystemAccessService) sp.GetRequiredService<IFileSystemAccessServiceInProcess>());
        services.AddTransient<IFileSaveService, FileSystemAccessFileSaveService>();

        return services;
    }

    /// <summary>
    /// Registers the plugin storage factory for WebAssembly using localStorage
    /// </summary>
    public static IServiceCollection AddPluginStorage(this IServiceCollection services)
    {
        services.AddSingleton<IPluginStorageFactory, LocalStoragePluginStorageFactory>();
        
        return services;
    }

    /// <summary>
    /// Registers all WebAssembly-specific services
    /// </summary>
    public static IServiceCollection AddWebAssemblyServices(this IServiceCollection services)
    {
        services.AddLinkOpeningService();
        services.AddFileOpenService();
        services.AddFileSaveService();
        services.AddPluginStorage();

        return services;
    }
}