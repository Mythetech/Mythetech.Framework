using KristofferStrube.Blazor.FileSystemAccess;
using Microsoft.Extensions.DependencyInjection;
using Mythetech.Framework.Infrastructure;
using Mythetech.Framework.Infrastructure.Environment;
using Mythetech.Framework.Infrastructure.Files;
using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Framework.Infrastructure.Settings;
using Mythetech.Framework.Infrastructure.Shell;
using Mythetech.Framework.WebAssembly.Environment;
using Mythetech.Framework.WebAssembly.Plugins;
using Mythetech.Framework.WebAssembly.Settings;
using Mythetech.Framework.WebAssembly.Shell;

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

    /// <summary>
    /// Registers localStorage-based settings storage for WebAssembly.
    /// Uses a different key prefix from plugin storage (settings: vs plugin:).
    /// </summary>
    public static IServiceCollection AddWebAssemblySettingsStorage(this IServiceCollection services)
    {
        services.AddScoped<ISettingsStorage, LocalStorageSettingsStorage>();
        return services;
    }

    /// <summary>
    /// Registers localStorage-based plugin state storage for WebAssembly.
    /// This enables persisting plugin enabled/disabled states across sessions.
    /// </summary>
    public static IServiceCollection AddPluginStateProvider(this IServiceCollection services)
    {
        services.AddScoped<IPluginStateProvider, LocalStoragePluginStateProvider>();
        return services;
    }

    /// <summary>
    /// Registers file operations services for WebAssembly.
    /// These throw PlatformNotSupportedException - use IFileOpenService/IFileSaveService
    /// for dialog-based file access instead.
    /// </summary>
    public static IServiceCollection AddFileOperations(this IServiceCollection services)
    {
        services.AddSingleton<IFileOperations, UnsupportedFileOperations>();
        services.AddSingleton<IFileReader>(sp => sp.GetRequiredService<IFileOperations>());
        services.AddSingleton<IFileWriter>(sp => sp.GetRequiredService<IFileOperations>());
        services.AddSingleton<IFileManager>(sp => sp.GetRequiredService<IFileOperations>());

        return services;
    }

    /// <summary>
    /// Registers directory operations services for WebAssembly.
    /// These throw PlatformNotSupportedException as direct directory access
    /// is not available in browser environments.
    /// </summary>
    public static IServiceCollection AddDirectoryOperations(this IServiceCollection services)
    {
        services.AddSingleton<IDirectoryOperations, UnsupportedDirectoryOperations>();

        return services;
    }

    /// <summary>
    /// Registers both file and directory operations for WebAssembly.
    /// Note: These will throw PlatformNotSupportedException when used.
    /// </summary>
    public static IServiceCollection AddFileSystemOperations(this IServiceCollection services)
    {
        services.AddFileOperations();
        services.AddDirectoryOperations();

        return services;
    }

    /// <summary>
    /// Registers the WebAssembly shell execution services.
    /// </summary>
    /// <remarks>
    /// This provides a simulated shell environment for WebAssembly applications.
    /// Commands can be registered via <see cref="ICommandRegistry"/> in C# or
    /// via <c>mythetech.shell.registerCommand()</c> in JavaScript.
    /// <para>
    /// Built-in commands include: echo, env, help, clear, version.
    /// </para>
    /// <para>
    /// Important: Include the shell-executor.js script in your application:
    /// <code>&lt;script src="_content/Mythetech.Framework.WebAssembly/shell-executor.js"&gt;&lt;/script&gt;</code>
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration for shell options.</param>
    public static IServiceCollection AddShellExecution(
        this IServiceCollection services,
        Action<WasmShellOptions>? configure = null)
    {
        var options = new WasmShellOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<ICommandRegistry, CommandRegistry>();
        services.AddScoped<IShellExecutor, WasmShellExecutor>();

        return services;
    }
}