using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Mythetech.Framework.Infrastructure.Plugins;
using NSubstitute;
using NSubstitute.ClearExtensions;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Plugins;

public class JsPluginAssetLoaderTests
{
    private const string Href = "_content/SamplePlugin/sample.css";
    private const string Src = "_content/SamplePlugin/sample.js";

    private readonly IJSRuntime _js = Substitute.For<IJSRuntime>();
    private readonly JsPluginAssetLoader _loader;

    public JsPluginAssetLoaderTests()
    {
        _loader = new JsPluginAssetLoader(_js);
    }

    private void InjectionThrows(Exception ex) =>
        _js.InvokeAsync<IJSVoidResult>(Arg.Any<string>(), Arg.Any<object?[]?>()).ThrowsAsyncForAnyArgs(ex);

    private int InjectionCalls() =>
        _js.ReceivedCalls().Count(c => c.GetMethodInfo().Name == nameof(IJSRuntime.InvokeAsync));

    [Fact(DisplayName = "LoadStylesheetAsync records a stylesheet once it is injected")]
    public async Task LoadStylesheetAsync_RecordsStylesheet_WhenInjected()
    {
        await _loader.LoadStylesheetAsync(Href);

        _loader.IsLoaded(Href).ShouldBeTrue();
    }

    [Fact(DisplayName = "LoadStylesheetAsync doesn't record a stylesheet lost to a disconnection, so a later call retries it")]
    public async Task LoadStylesheetAsync_RetriesStylesheet_LostToDisconnection()
    {
        InjectionThrows(new JSDisconnectedException("circuit gone"));

        await _loader.LoadStylesheetAsync(Href);
        _loader.IsLoaded(Href).ShouldBeFalse();

        _js.ClearSubstitute();
        await _loader.LoadStylesheetAsync(Href);

        _loader.IsLoaded(Href).ShouldBeTrue();
        InjectionCalls().ShouldBe(1);
    }

    [Fact(DisplayName = "LoadScriptAsync doesn't record a script lost to a disconnection")]
    public async Task LoadScriptAsync_DoesNotRecordScript_LostToDisconnection()
    {
        InjectionThrows(new ObjectDisposedException("WebView"));

        await _loader.LoadScriptAsync(Src);

        _loader.IsLoaded(Src).ShouldBeFalse();
    }

    [Fact(DisplayName = "LoadScriptAsync rethrows a genuine JS error and doesn't record the script")]
    public async Task LoadScriptAsync_RethrowsJsError_AndDoesNotRecordScript()
    {
        InjectionThrows(new JSException("script failed to load"));

        await Should.ThrowAsync<JSException>(async () => await _loader.LoadScriptAsync(Src));

        _loader.IsLoaded(Src).ShouldBeFalse();
    }

    [Fact(DisplayName = "UnloadStylesheetAsync forgets the stylesheet even when the runtime is gone")]
    public async Task UnloadStylesheetAsync_ForgetsStylesheet_WhenRuntimeIsGone()
    {
        await _loader.LoadStylesheetAsync(Href);
        InjectionThrows(new JSDisconnectedException("circuit gone"));

        await _loader.UnloadStylesheetAsync(Href);

        _loader.IsLoaded(Href).ShouldBeFalse();
    }
}
