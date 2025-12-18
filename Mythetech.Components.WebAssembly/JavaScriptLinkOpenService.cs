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