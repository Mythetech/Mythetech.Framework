using Mythetech.Framework.Desktop.Hermes;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Files;

public class HermesInteropFileSaveServiceTests : IDisposable
{
    private readonly string _directory;
    private readonly ISaveFileDialog _dialog = Substitute.For<ISaveFileDialog>();
    private readonly HermesInteropFileSaveService _service;

    public HermesInteropFileSaveServiceTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"file_save_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_directory);
        _service = new HermesInteropFileSaveService(_dialog);
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    [Fact(DisplayName = "SaveFileAsync writes the text to the chosen path")]
    public async Task SaveFileAsync_Text_WritesToChosenPath()
    {
        var path = Path.Combine(_directory, "export.csv");
        _dialog.ShowSaveFile(Arg.Any<string>(), Arg.Any<string?>()).Returns(path);

        var result = await _service.SaveFileAsync("export.csv", "id,name\n1,Zoë");

        result.ShouldBeTrue();
        (await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken)).ShouldBe("id,name\n1,Zoë");
    }

    [Fact(DisplayName = "SaveFileAsync writes binary data unchanged to the chosen path")]
    public async Task SaveFileAsync_Bytes_WritesToChosenPath()
    {
        var path = Path.Combine(_directory, "report.xlsx");
        _dialog.ShowSaveFile(Arg.Any<string>(), Arg.Any<string?>()).Returns(path);
        byte[] data = [0x50, 0x4B, 0x03, 0x04, 0x00, 0xFF];

        var result = await _service.SaveFileAsync("report.xlsx", data);

        result.ShouldBeTrue();
        (await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken)).ShouldBe(data);
    }

    [Fact(DisplayName = "SaveFileAsync returns false and writes nothing when the dialog is cancelled")]
    public async Task SaveFileAsync_Cancelled_ReturnsFalse()
    {
        _dialog.ShowSaveFile(Arg.Any<string>(), Arg.Any<string?>()).Returns((string?)null);

        var textResult = await _service.SaveFileAsync("export.csv", "id,name");
        var bytesResult = await _service.SaveFileAsync("report.xlsx", new byte[] { 0x50, 0x4B });

        textResult.ShouldBeFalse();
        bytesResult.ShouldBeFalse();
        Directory.EnumerateFileSystemEntries(_directory).ShouldBeEmpty();
    }

    [Theory(DisplayName = "SaveFileAsync filters the dialog by the file name's extension")]
    [InlineData("export.csv", "csv")]
    [InlineData("report.xlsx", "xlsx")]
    [InlineData("export", null)]
    public async Task SaveFileAsync_FiltersByFileExtension(string fileName, string? extension)
    {
        _dialog.ShowSaveFile(Arg.Any<string>(), Arg.Any<string?>()).Returns((string?)null);

        await _service.SaveFileAsync(fileName, "content");

        _dialog.Received(1).ShowSaveFile(fileName, extension);
    }

    [Fact(DisplayName = "PromptFileSaveAsync passes the requested extension to the dialog")]
    public async Task PromptFileSaveAsync_PassesExtension()
    {
        var path = Path.Combine(_directory, "query.sql");
        _dialog.ShowSaveFile("query", "sql").Returns(path);

        var location = await _service.PromptFileSaveAsync("query", "sql");

        location.ShouldBe(path);
    }
}
