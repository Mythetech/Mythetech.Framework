using Bunit;
using KristofferStrube.Blazor.FileSystem;
using Microsoft.JSInterop;
using Mythetech.Framework.WebAssembly.Services;
using NSubstitute;
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

    [Fact(DisplayName = "WriteAsync writes the data and then closes the stream exactly once")]
    public async Task WriteAsync_WritesThenClosesOnce()
    {
        // Browsers reject closing a writable stream that is already closed.
        var writable = Substitute.For<IJSObjectReference>();
        var handleReference = Substitute.For<IJSObjectReference>();
        handleReference.InvokeAsync<IJSObjectReference>("createWritable", Arg.Any<object?[]?>())
            .Returns(new ValueTask<IJSObjectReference>(writable));
        var handle = FileSystemFileHandle.Create(Substitute.For<IJSRuntime>(), handleReference);
        var writer = new BrowserFileWriter(Substitute.For<IJSRuntime>());
        byte[] data = [0x50, 0x4B, 0x03, 0x04];

        await writer.WriteAsync(handle, data);

        var invocations = writable.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(IJSObjectReference.InvokeAsync))
            .Select(call => call.GetArguments())
            .ToList();
        invocations.Select(arguments => arguments[0]).ShouldBe(["write", "close"]);
        invocations[0][1].ShouldBe(new object[] { data });
    }
}
