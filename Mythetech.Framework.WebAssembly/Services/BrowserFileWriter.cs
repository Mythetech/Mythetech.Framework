using KristofferStrube.Blazor.FileSystem;
using Microsoft.JSInterop;

namespace Mythetech.Framework.WebAssembly.Services;

/// <summary>
/// Writes file contents in the browser, either into a File System Access file handle or as a download
/// </summary>
internal interface IBrowserFileWriter
{
    /// <summary>
    /// Writes the data into the file behind the handle, replacing its contents
    /// </summary>
    Task WriteAsync(FileSystemFileHandle fileHandle, byte[] data);

    /// <summary>
    /// Saves the data through a regular browser download
    /// </summary>
    Task DownloadAsync(string fileName, string mimeType, byte[] data);
}

/// <summary>
/// JavaScript interop implementation of <see cref="IBrowserFileWriter"/>
/// </summary>
internal sealed class BrowserFileWriter : IBrowserFileWriter
{
    internal const string ModulePath = "./_content/Mythetech.Framework.WebAssembly/file-download.js";

    private readonly IJSRuntime _jsRuntime;

    public BrowserFileWriter(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task WriteAsync(FileSystemFileHandle fileHandle, byte[] data)
    {
        await using var writable = await fileHandle.CreateWritableAsync();
        await writable.WriteAsync(data);

        // The browser writes to a swap file and only replaces the real file on close; disposing just releases the JS reference.
        await writable.CloseAsync();
    }

    public async Task DownloadAsync(string fileName, string mimeType, byte[] data)
    {
        await using var module = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath);
        await module.InvokeVoidAsync("downloadFile", fileName, mimeType, data);
    }
}
