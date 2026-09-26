using Bunit;
using Mythetech.Framework.WebAssembly.Services;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Files;

public class BrowserFileWriterTests : BunitContext
{
    [Fact(DisplayName = "DownloadAsync hands the file to the download module")]
    public async Task DownloadAsync_InvokesDownloadModule()
    {
        var module = JSInterop.SetupModule(BrowserFileWriter.ModulePath);
        module.SetupVoid("downloadFile", _ => true).SetVoidResult();
        var writer = new BrowserFileWriter(JSInterop.JSRuntime);
        byte[] data = [0x50, 0x4B, 0x03, 0x04];

        await writer.DownloadAsync("report.xlsx", "application/octet-stream", data);

        var invocation = module.VerifyInvoke("downloadFile");
        invocation.Arguments.ShouldBe(["report.xlsx", "application/octet-stream", data]);
    }
}
