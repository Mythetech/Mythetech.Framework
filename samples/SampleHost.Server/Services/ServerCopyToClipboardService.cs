using MudBlazor;
using Mythetech.Framework.Infrastructure;

namespace SampleHost.Server.Services;

public class ServerCopyToClipboardService : ICopyToClipboard
{
    private readonly IJsApiService _jsApiService;

    public ServerCopyToClipboardService(IJsApiService jsApiService)
    {
        _jsApiService = jsApiService;
    }

    public Task CopyToClipboardAsync(string text) => _jsApiService.CopyToClipboardAsync(text).AsTask();
}
