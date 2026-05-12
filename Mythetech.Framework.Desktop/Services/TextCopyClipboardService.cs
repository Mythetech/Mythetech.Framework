using Mythetech.Framework.Infrastructure;
using TextCopy;

namespace Mythetech.Framework.Desktop.Services;

/// <summary>
/// Desktop clipboard implementation using the cross-platform TextCopy library.
/// Avoids WebView clipboard JS interop which can be unreliable in Blazor Hybrid hosts.
/// </summary>
public class TextCopyClipboardService : ICopyToClipboard
{
    public Task CopyToClipboardAsync(string text) => ClipboardService.SetTextAsync(text);
}
