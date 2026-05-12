using MudBlazor;
using Mythetech.Framework.Infrastructure;

namespace Mythetech.Framework.WebAssembly.Services;

/// <summary>
/// WebAssembly clipboard implementation that delegates to MudBlazor's <see cref="IJsApiService"/>.
/// </summary>
public class JsCopyToClipboardService : ICopyToClipboard
{
    private readonly IJsApiService _jsApiService;

    public JsCopyToClipboardService(IJsApiService jsApiService)
    {
        _jsApiService = jsApiService;
    }

    public Task CopyToClipboardAsync(string text) => _jsApiService.CopyToClipboardAsync(text).AsTask();
}
