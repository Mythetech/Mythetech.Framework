using Microsoft.JSInterop;
using Mythetech.Framework.Components.Kbd;

namespace Mythetech.Framework.WebAssembly.Components;

/// <summary>
/// WebAssembly platform detector using navigator.platform / navigator.userAgent
/// via synchronous JS interop. Result is cached after first call.
/// </summary>
public sealed class WebAssemblyPlatformDetector : IPlatformDetector
{
    private readonly IJSInProcessRuntime _jsRuntime;
    private bool? _isMacOS;

    public WebAssemblyPlatformDetector(IJSRuntime jsRuntime)
    {
        _jsRuntime = (IJSInProcessRuntime)jsRuntime;
    }

    public bool IsMacOS => _isMacOS ??= DetectMacOS();

    private bool DetectMacOS()
    {
        return _jsRuntime.Invoke<bool>(
            "eval",
            "navigator.platform.startsWith('Mac') || /Macintosh|Mac OS/.test(navigator.userAgent)");
    }
}
