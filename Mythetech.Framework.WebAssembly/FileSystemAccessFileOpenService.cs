using KristofferStrube.Blazor.FileSystemAccess;
using Microsoft.JSInterop;
using Mythetech.Framework.Infrastructure.Files;
using Mythetech.Framework.WebAssembly.Exceptions;

namespace Mythetech.Framework.WebAssembly;

/// <summary>
/// WebAssembly implementation of file open service using the File System Access API
/// </summary>
public class FileSystemAccessFileOpenService : IFileOpenService
{
    private readonly IFileSystemAccessService _fileSystemAccess;

    /// <summary>
    /// Creates a new instance of the file system access file open service
    /// </summary>
    /// <param name="fileSystemAccess">The file system access service</param>
    public FileSystemAccessFileOpenService(IFileSystemAccessService fileSystemAccess)
    {
        _fileSystemAccess = fileSystemAccess;
    }

    /// <inheritdoc />
    public async Task<string[]> OpenFileAsync(
        string title = "Choose file",
        string? defaultPath = null,
        bool multiSelect = false,
        FileFilter[]? filters = null)
    {
        try
        {
            var options = new OpenFilePickerOptionsStartInWellKnownDirectory
            {
                Multiple = multiSelect,
                StartIn = WellKnownDirectory.Downloads
            };

            if (filters is { Length: > 0 })
            {
                options.Types = filters.Select(f => new FilePickerAcceptType
                {
                    Description = f.Name,
                    Accept = new Dictionary<string, string[]>
                    {
                        { GetMimeTypeFromExtensions(f.Extensions), f.Extensions.Select(e => $".{e}").ToArray() }
                    }
                }).ToArray();
            }

            var fileHandles = await _fileSystemAccess.ShowOpenFilePickerAsync(options);

            if (fileHandles is null || fileHandles.Length == 0)
                return [];

            var names = new List<string>();
            foreach (var handle in fileHandles)
            {
                names.Add(await handle.GetNameAsync());
            }

            return names.ToArray();
        }
        catch (JSException ex) when (IsUserCancellation(ex))
        {
            return [];
        }
        catch (JSException ex)
        {
            throw new UnsupportedBrowserApiException("File System Access", ex);
        }
    }

    /// <inheritdoc />
    public async Task<string[]> OpenFolderAsync(
        string title = "Choose folder",
        string? defaultPath = null,
        bool multiSelect = false)
    {
        try
        {
            var options = new DirectoryPickerOptionsStartInWellKnownDirectory
            {
                StartIn = WellKnownDirectory.Downloads
            };

            var directoryHandle = await _fileSystemAccess.ShowDirectoryPickerAsync(options);

            if (directoryHandle is null)
                return [];

            return [await directoryHandle.GetNameAsync()];
        }
        catch (JSException ex) when (IsUserCancellation(ex))
        {
            return [];
        }
        catch (JSException ex)
        {
            throw new UnsupportedBrowserApiException("File System Access", ex);
        }
    }

    private static bool IsUserCancellation(JSException ex)
    {
        return ex.Message.Contains("AbortError") || ex.Message.Contains("The user aborted a request");
    }

    private static string GetMimeTypeFromExtensions(string[] extensions)
    {
        if (extensions.Length == 0)
            return "*/*";

        var ext = extensions[0].ToLowerInvariant();
        return ext switch
        {
            "txt" => "text/plain",
            "json" => "application/json",
            "xml" => "application/xml",
            "html" or "htm" => "text/html",
            "css" => "text/css",
            "js" => "application/javascript",
            "png" => "image/png",
            "jpg" or "jpeg" => "image/jpeg",
            "gif" => "image/gif",
            "svg" => "image/svg+xml",
            "pdf" => "application/pdf",
            "zip" => "application/zip",
            "csv" => "text/csv",
            _ => "*/*"
        };
    }
}
