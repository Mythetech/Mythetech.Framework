using KristofferStrube.Blazor.FileSystemAccess;
using Microsoft.Extensions.DependencyInjection;
using Mythetech.Framework.Infrastructure;
using Mythetech.Framework.Infrastructure.Environment;
using Mythetech.Framework.Infrastructure.Files;
using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Framework.WebAssembly.Environment;

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
    /// Registers the show file service for WebAssembly (no-op with logging)
    /// </summary>
    public static IServiceCollection AddShowFileService(this IServiceCollection services)
    {
        services.AddTransient<IShowFileService, ShowFileService>();

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
        services.AddShowFileService();

        return services;
    }

    /// <summary>
    /// Registers the runtime environment for WebAssembly.
    /// Uses IWebAssemblyHostEnvironment for environment detection and NavigationManager for base URL.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="version">Optional version override. If not specified, uses the entry assembly version.</param>
    public static IServiceCollection AddRuntimeEnvironment(this IServiceCollection services, Version? version = null)
    {
        services.AddScoped<IRuntimeEnvironment>(sp =>
        {
            var hostEnvironment = sp.GetRequiredService<Microsoft.AspNetCore.Components.WebAssembly.Hosting.IWebAssemblyHostEnvironment>();
            var navigationManager = sp.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
            return new WebAssemblyRuntimeEnvironment(hostEnvironment, navigationManager, version);
        });

        return services;
    }
}