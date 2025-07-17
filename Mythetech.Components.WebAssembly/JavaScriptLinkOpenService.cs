using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Mythetech.Components.Infrastructure;

namespace Mythetech.Components.WebAssembly;

public class JavaScriptLinkOpenService : ILinkOpenService
{
    private readonly IJSRuntime _jsRuntime;

    public JavaScriptLinkOpenService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }
    
    public async Task OpenLinkAsync(string url)
    {
        await _jsRuntime.InvokeVoidAsync("open", url, "_blank");      
    }
}

public static class JavaScriptLinkOpenServiceExtensions
{
    public static IServiceCollection AddLinkOpeningService(this IServiceCollection services)
    {
        services.AddTransient<ILinkOpenService, JavaScriptLinkOpenService>();
        
        return services;
    }
}