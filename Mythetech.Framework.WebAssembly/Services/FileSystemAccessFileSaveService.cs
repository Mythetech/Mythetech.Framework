using System.Text;
using KristofferStrube.Blazor.FileSystem;
using KristofferStrube.Blazor.FileSystemAccess;
using Microsoft.JSInterop;
using Mythetech.Framework.Infrastructure.Files;
using Mythetech.Framework.WebAssembly.Exceptions;

namespace Mythetech.Framework.WebAssembly.Services;

/// <summary>
/// WebAssembly implementation of file save service using the File System Access API.
/// Browsers without the API, such as Firefox and Safari, get a regular download instead.
/// </summary>
public class FileSystemAccessFileSaveService : IFileSaveService
{
    // Chromium rejects picker accept extensions longer than 16 characters including the leading dot.
    private const int MaxPickerExtensionLength = 15;

    private readonly IFileSystemAccessService _fileSystemAccess;
    private readonly IBrowserFileWriter _fileWriter;

    /// <summary>
    /// Creates a new instance of the file system access file save service
    /// </summary>
    /// <param name="fileSystemAccess">The file system access service</param>
    /// <param name="jsRuntime">The JavaScript runtime, used to trigger downloads when the File System Access API is unavailable</param>
    public FileSystemAccessFileSaveService(IFileSystemAccessService fileSystemAccess, IJSRuntime jsRuntime)
        : this(fileSystemAccess, new BrowserFileWriter(jsRuntime))
    {
    }

    internal FileSystemAccessFileSaveService(IFileSystemAccessService fileSystemAccess, IBrowserFileWriter fileWriter)
    {
        _fileSystemAccess = fileSystemAccess;
        _fileWriter = fileWriter;
    }

    /// <inheritdoc />
    public async Task<bool> SaveFileAsync(string fileName, string data)
    {
        return await SaveBytesAsync(fileName, Encoding.UTF8.GetBytes(data));
    }

    /// <inheritdoc />
    public async Task<string?> PromptFileSaveAsync(string fileName, string extension = "txt")
    {
        try
        {
            var fileHandle = await _fileSystemAccess.ShowSaveFilePickerAsync(CreatePickerOptions(fileName, extension));

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

    private async Task<bool> SaveBytesAsync(string fileName, byte[] data)
    {
        var extension = GetPickerExtension(fileName);
        FileSystemFileHandle? fileHandle;

        try
        {
            fileHandle = await _fileSystemAccess.ShowSaveFilePickerAsync(CreatePickerOptions(fileName, extension));
        }
        catch (JSException ex) when (IsUserCancellation(ex))
        {
            return false;
        }
        catch (JSException)
        {
            // Besides browsers that lack showSaveFilePicker, Chromium also refuses the picker once the click's
            // user activation has expired (slow exports), and a download still gets the file to the user.
            await _fileWriter.DownloadAsync(fileName, GetMimeTypeFromExtension(extension), data);
            return true;
        }

        if (fileHandle is null)
            return false;

        await _fileWriter.WriteAsync(fileHandle, data);
        return true;
    }

    private static SaveFilePickerOptionsStartInWellKnownDirectory CreatePickerOptions(string fileName, string? extension)
    {
        return new SaveFilePickerOptionsStartInWellKnownDirectory
        {
            StartIn = WellKnownDirectory.Downloads,
            SuggestedName = fileName,
            Types = extension is null
                ? null
                :
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
    }

    private static string? GetPickerExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName).TrimStart('.');

        // A picker type the browser rejects fails the whole picker call, so an odd extension gets no type filter.
        var isValid = extension.Length is > 0 and <= MaxPickerExtensionLength
            && extension.All(c => char.IsAsciiLetterOrDigit(c) || c == '+');

        return isValid ? extension.ToLowerInvariant() : null;
    }

    private static bool IsUserCancellation(JSException ex)
    {
        return ex.Message.Contains("AbortError") || ex.Message.Contains("The user aborted a request");
    }

    private static string GetMimeTypeFromExtension(string? extension)
    {
        return extension?.ToLowerInvariant() switch
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
            "tsv" => "text/tab-separated-values",
            "md" => "text/markdown",
            "sql" => "application/sql",
            "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "xls" => "application/vnd.ms-excel",
            _ => "application/octet-stream"
        };
    }
}
