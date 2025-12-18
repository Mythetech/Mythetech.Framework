using KristofferStrube.Blazor.FileSystemAccess;
using Microsoft.JSInterop;
using Mythetech.Components.Infrastructure;
using Mythetech.Components.WebAssembly.Exceptions;

namespace Mythetech.Components.WebAssembly;

/// <summary>
/// WebAssembly implementation of file save service using the File System Access API
/// </summary>
public class FileSystemAccessFileSaveService : IFileSaveService
{
    private readonly IFileSystemAccessService _fileSystemAccess;

    /// <summary>
    /// Creates a new instance of the file system access file save service
    /// </summary>
    /// <param name="fileSystemAccess">The file system access service</param>
    public FileSystemAccessFileSaveService(IFileSystemAccessService fileSystemAccess)
    {
        _fileSystemAccess = fileSystemAccess;
    }

    /// <inheritdoc />
    public async Task<bool> SaveFileAsync(string fileName, string data)
    {
        var location = await PromptFileSaveAsync(fileName);

        if (string.IsNullOrWhiteSpace(location))
            return false;

        return true;
    }

    /// <inheritdoc />
    public async Task<string?> PromptFileSaveAsync(string fileName, string extension = "txt")
    {
        try
        {
            var options = new SaveFilePickerOptionsStartInWellKnownDirectory
            {
                StartIn = WellKnownDirectory.Downloads,
                SuggestedName = fileName,
                Types =
                [
                    new FilePickerAcceptType
                    {
                        Description = $"{extension.ToUpperInvariant()} files",
                        Accept = new Dictionary<string, string[]>
                        {
                            { GetMimeTypeFromExtension(extension), [$".{extension}"] }
                        }
                    }
                ]
            };

            var fileHandle = await _fileSystemAccess.ShowSaveFilePickerAsync(options);

            if (fileHandle is null)
                return null;

            return await fileHandle.GetNameAsync();
        }
        catch (JSException ex) when (IsUserCancellation(ex))
        {
            return null;
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

    private static string GetMimeTypeFromExtension(string extension)
    {
        var ext = extension.ToLowerInvariant();
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
            _ => "application/octet-stream"
        };
    }
}

