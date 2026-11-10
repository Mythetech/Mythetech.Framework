using Microsoft.JSInterop;
using Mythetech.Framework.Infrastructure.Plugins;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Plugins;

public class PluginModuleInteropTests
{
    private const string ImportIdentifier = "import";

    private readonly IJSRuntime _jsRuntime = Substitute.For<IJSRuntime>();
    private readonly IJSObjectReference _module = Substitute.For<IJSObjectReference>();

    public PluginModuleInteropTests()
    {
        _module.InvokeAsync<IJSObjectReference>("importFromSource", Arg.Any<object?[]?>())
            .Returns(_ => ValueTask.FromResult(Substitute.For<IJSObjectReference>()));
    }

    [Fact(DisplayName = "ImportFromSourceAsync shares one import of the interop module between concurrent first calls")]
    public async Task ImportFromSourceAsync_ConcurrentFirstCalls_ImportOnce()
    {
        var import = new TaskCompletionSource<IJSObjectReference>();
        _jsRuntime.InvokeAsync<IJSObjectReference>(ImportIdentifier, Arg.Any<object?[]?>())
            .Returns(_ => new ValueTask<IJSObjectReference>(import.Task));
        var interop = new PluginModuleInterop(_jsRuntime);

        var first = interop.ImportFromSourceAsync("export const a = 1;");
        var second = interop.ImportFromSourceAsync("export const b = 2;");
        import.SetResult(_module);
        await Task.WhenAll(first, second);

        await _jsRuntime.Received(1).InvokeAsync<IJSObjectReference>(ImportIdentifier, Arg.Any<object?[]?>());
        await _module.Received(2).InvokeAsync<IJSObjectReference>("importFromSource", Arg.Any<object?[]?>());
    }

    [Fact(DisplayName = "ImportFromSourceAsync imports the interop module again after an import fails")]
    public async Task ImportFromSourceAsync_FailedImport_RetriesNextCall()
    {
        _jsRuntime.InvokeAsync<IJSObjectReference>(ImportIdentifier, Arg.Any<object?[]?>())
            .Returns(
                _ => ValueTask.FromException<IJSObjectReference>(new JSException("import failed")),
                _ => ValueTask.FromResult(_module));
        var interop = new PluginModuleInterop(_jsRuntime);

        await Should.ThrowAsync<JSException>(() => interop.ImportFromSourceAsync("export const a = 1;"));
        var imported = await interop.ImportFromSourceAsync("export const a = 1;");

        imported.ShouldNotBeNull();
        await _jsRuntime.Received(2).InvokeAsync<IJSObjectReference>(ImportIdentifier, Arg.Any<object?[]?>());
    }

    [Fact(DisplayName = "DisposeAsync disposes the imported interop module")]
    public async Task DisposeAsync_AfterImport_DisposesModule()
    {
        _jsRuntime.InvokeAsync<IJSObjectReference>(ImportIdentifier, Arg.Any<object?[]?>())
            .Returns(_ => ValueTask.FromResult(_module));
        var interop = new PluginModuleInterop(_jsRuntime);
        await interop.ImportFromSourceAsync("export const a = 1;");

        await interop.DisposeAsync();

        await _module.Received(1).DisposeAsync();
    }

    [Fact(DisplayName = "DisposeAsync after a failed import completes without disposing anything")]
    public async Task DisposeAsync_AfterFailedImport_DoesNotThrow()
    {
        _jsRuntime.InvokeAsync<IJSObjectReference>(ImportIdentifier, Arg.Any<object?[]?>())
            .Returns(_ => ValueTask.FromException<IJSObjectReference>(new JSException("import failed")));
        var interop = new PluginModuleInterop(_jsRuntime);
        await Should.ThrowAsync<JSException>(() => interop.ImportFromSourceAsync("export const a = 1;"));

        await Should.NotThrowAsync(async () => await interop.DisposeAsync());
    }

    [Fact(DisplayName = "DisposeAsync ignores a WebView that's already gone")]
    public async Task DisposeAsync_Disconnected_DoesNotThrow()
    {
        _jsRuntime.InvokeAsync<IJSObjectReference>(ImportIdentifier, Arg.Any<object?[]?>())
            .Returns(_ => ValueTask.FromResult(_module));
        _module.DisposeAsync().Returns(_ => ValueTask.FromException(new JSDisconnectedException("gone")));
        var interop = new PluginModuleInterop(_jsRuntime);
        await interop.ImportFromSourceAsync("export const a = 1;");

        await Should.NotThrowAsync(async () => await interop.DisposeAsync());
    }
}
