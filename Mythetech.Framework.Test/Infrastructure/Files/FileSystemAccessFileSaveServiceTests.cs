using System.Text;
using KristofferStrube.Blazor.FileSystem;
using KristofferStrube.Blazor.FileSystemAccess;
using Microsoft.JSInterop;
using Mythetech.Framework.WebAssembly.Exceptions;
using Mythetech.Framework.WebAssembly.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Files;

public class FileSystemAccessFileSaveServiceTests
{
    private const string UserCancelledMessage = "AbortError: The user aborted a request.";
    private const string PickerMissingMessage = "Could not find 'showSaveFilePicker' ('showSaveFilePicker' was undefined).";

    private readonly IFileSystemAccessService _fileSystemAccess = Substitute.For<IFileSystemAccessService>();
    private readonly IBrowserFileWriter _fileWriter = Substitute.For<IBrowserFileWriter>();
    private readonly FileSystemAccessFileSaveService _service;

    public FileSystemAccessFileSaveServiceTests()
    {
        _service = new FileSystemAccessFileSaveService(_fileSystemAccess, _fileWriter);
    }

    [Fact(DisplayName = "SaveFileAsync writes the text as UTF-8 to the picked file")]
    public async Task SaveFileAsync_Text_WritesContentToPickedFile()
    {
        var handle = CreateHandle();
        PickerReturns(handle);

        var result = await _service.SaveFileAsync("export.csv", "id,name\n1,Zoë");

        result.ShouldBeTrue();
        await _fileWriter.Received(1).WriteAsync(
            handle,
            Arg.Is<byte[]>(bytes => bytes.SequenceEqual(Encoding.UTF8.GetBytes("id,name\n1,Zoë"))));
    }

    [Fact(DisplayName = "SaveFileAsync returns false and writes nothing when the user cancels")]
    public async Task SaveFileAsync_UserCancels_ReturnsFalse()
    {
        PickerThrows(UserCancelledMessage);

        var result = await _service.SaveFileAsync("export.csv", "id,name");

        result.ShouldBeFalse();
        await _fileWriter.DidNotReceiveWithAnyArgs().WriteAsync(default!, default!);
        await _fileWriter.DidNotReceiveWithAnyArgs().DownloadAsync(default!, default!, default!);
    }

    [Fact(DisplayName = "SaveFileAsync downloads the file when the File System Access API is unavailable")]
    public async Task SaveFileAsync_ApiUnavailable_FallsBackToDownload()
    {
        PickerThrows(PickerMissingMessage);

        var result = await _service.SaveFileAsync("export.csv", "id,name");

        result.ShouldBeTrue();
        await _fileWriter.Received(1).DownloadAsync(
            "export.csv",
            "text/csv",
            Arg.Is<byte[]>(bytes => bytes.SequenceEqual(Encoding.UTF8.GetBytes("id,name"))));
        await _fileWriter.DidNotReceiveWithAnyArgs().WriteAsync(default!, default!);
    }

    [Fact(DisplayName = "SaveFileAsync propagates a failed write instead of reporting success")]
    public async Task SaveFileAsync_WriteFails_Throws()
    {
        PickerReturns(CreateHandle());
        _fileWriter.WriteAsync(Arg.Any<FileSystemFileHandle>(), Arg.Any<byte[]>())
            .ThrowsAsync(new JSException("NotAllowedError: The request is not allowed."));

        await Should.ThrowAsync<JSException>(() => _service.SaveFileAsync("export.csv", "id,name"));
    }

    [Theory(DisplayName = "SaveFileAsync offers a picker type matching the file extension")]
    [InlineData("export.csv", "text/csv", ".csv")]
    [InlineData("results.json", "application/json", ".json")]
    [InlineData("query.sql", "application/sql", ".sql")]
    [InlineData("report.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ".xlsx")]
    [InlineData("NOTES.TXT", "text/plain", ".txt")]
    [InlineData("archive.custom", "application/octet-stream", ".custom")]
    public async Task SaveFileAsync_PickerTypeMatchesExtension(string fileName, string mimeType, string acceptedExtension)
    {
        var options = CapturePickerOptions();

        await _service.SaveFileAsync(fileName, "content");

        options().ShouldNotBeNull();
        options()!.SuggestedName.ShouldBe(fileName);
        var type = options()!.Types.ShouldHaveSingleItem();
        type.Accept.ShouldNotBeNull();
        type.Accept.ShouldContainKey(mimeType);
        type.Accept[mimeType].ShouldBe([acceptedExtension]);
    }

    [Theory(DisplayName = "SaveFileAsync offers no picker type when the file name has no usable extension")]
    [InlineData("export")]
    [InlineData("export.")]
    [InlineData("export.tar-gz")]
    public async Task SaveFileAsync_NoUsableExtension_NoPickerType(string fileName)
    {
        var options = CapturePickerOptions();

        await _service.SaveFileAsync(fileName, "content");

        options().ShouldNotBeNull();
        options()!.Types.ShouldBeNull();
    }

    [Fact(DisplayName = "PromptFileSaveAsync throws UnsupportedBrowserApiException when the API is unavailable")]
    public async Task PromptFileSaveAsync_ApiUnavailable_Throws()
    {
        PickerThrows(PickerMissingMessage);

        await Should.ThrowAsync<UnsupportedBrowserApiException>(() => _service.PromptFileSaveAsync("export", "csv"));
    }

    private static FileSystemFileHandle CreateHandle()
    {
        return FileSystemFileHandle.Create(Substitute.For<IJSRuntime>(), Substitute.For<IJSObjectReference>());
    }

    private void PickerReturns(FileSystemFileHandle handle)
    {
        _fileSystemAccess.ShowSaveFilePickerAsync(Arg.Any<SaveFilePickerOptionsStartInWellKnownDirectory?>())
            .Returns(handle);
    }

    private void PickerThrows(string message)
    {
        _fileSystemAccess.ShowSaveFilePickerAsync(Arg.Any<SaveFilePickerOptionsStartInWellKnownDirectory?>())
            .ThrowsAsync(new JSException(message));
    }

    private Func<SaveFilePickerOptionsStartInWellKnownDirectory?> CapturePickerOptions()
    {
        SaveFilePickerOptionsStartInWellKnownDirectory? captured = null;
        _fileSystemAccess.ShowSaveFilePickerAsync(Arg.Do<SaveFilePickerOptionsStartInWellKnownDirectory?>(o => captured = o))
            .Returns(CreateHandle());
        return () => captured;
    }
}
