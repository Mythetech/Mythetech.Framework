using KristofferStrube.Blazor.FileSystemAccess;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Mythetech.Components.Infrastructure;

namespace Mythetech.Components.WebAssembly;

/// <summary>
/// WebAssembly implementation of link opening using JavaScript interop
/// </summary>
public class JavaScriptLinkOpenService : ILinkOpenService
{
    private readonly IJSRuntime _jsRuntime;

    /// <summary>
    /// Creates a new instance of the JavaScript link open service
    /// </summary>
    /// <param name="jsRuntime">The JavaScript runtime</param>
    public JavaScriptLinkOpenService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <inheritdoc />
    public async Task OpenLinkAsync(string url)
    {
        await _jsRuntime.InvokeVoidAsync("open", url, "_blank");      
    }
}

/// <summary>
/// WebAssembly service registration extensions
/// </summary>
public static class WebAssemblyServiceExtensions
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
    /// Registers all WebAssembly-specific services
    /// </summary>
    public static IServiceCollection AddWebAssemblyServices(this IServiceCollection services)
    {
        services.AddLinkOpeningService();
        services.AddFileOpenService();

        return services;
    }
}