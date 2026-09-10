using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Mythetech.Framework.Infrastructure.Guards;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Guards;

public class JsRuntimeExtensionsTests
{
    private readonly IJSRuntime _js = Substitute.For<IJSRuntime>();

    private void VoidCallThrows(Exception ex) =>
        _js.InvokeAsync<IJSVoidResult>(Arg.Any<string>(), Arg.Any<object?[]?>()).ThrowsAsyncForAnyArgs(ex);

    private void ValueCallThrows(Exception ex) =>
        _js.InvokeAsync<int>(Arg.Any<string>(), Arg.Any<object?[]?>()).ThrowsAsyncForAnyArgs(ex);

    [Fact(DisplayName = "TryInvokeVoidAsync reports success when the call completes")]
    public async Task TryInvokeVoidAsync_ReportsSuccess_WhenCallCompletes()
    {
        var result = await _js.TryInvokeVoidAsync("noop");

        result.Status.ShouldBe(JsInvokeStatus.Success);
        result.Success.ShouldBeTrue();
        result.Exception.ShouldBeNull();
    }

    [Fact(DisplayName = "TryInvokeVoidAsync reports Disconnected when the runtime is gone")]
    public async Task TryInvokeVoidAsync_ReportsDisconnected_WhenRuntimeIsGone()
    {
        VoidCallThrows(new JSDisconnectedException("circuit gone"));

        var result = await _js.TryInvokeVoidAsync("noop");

        result.Status.ShouldBe(JsInvokeStatus.Disconnected);
        result.Success.ShouldBeFalse();
        result.Exception.ShouldBeNull();
    }

    [Fact(DisplayName = "TryInvokeVoidAsync reports Disconnected when the runtime was disposed")]
    public async Task TryInvokeVoidAsync_ReportsDisconnected_WhenRuntimeWasDisposed()
    {
        VoidCallThrows(new ObjectDisposedException("WebView"));

        var result = await _js.TryInvokeVoidAsync("noop");

        result.Status.ShouldBe(JsInvokeStatus.Disconnected);
    }

    [Fact(DisplayName = "TryInvokeVoidAsync reports Failed and carries the exception on a JS error")]
    public async Task TryInvokeVoidAsync_ReportsFailed_AndCarriesException_OnJsError()
    {
        var jsError = new JSException("ReferenceError: foo is not defined");
        VoidCallThrows(jsError);

        var result = await _js.TryInvokeVoidAsync("boom");

        result.Status.ShouldBe(JsInvokeStatus.Failed);
        result.Success.ShouldBeFalse();
        result.Exception.ShouldBeSameAs(jsError);
    }

    [Fact(DisplayName = "TryInvokeVoidAsync lets unrelated exceptions propagate")]
    public async Task TryInvokeVoidAsync_LetsUnrelatedExceptionsPropagate()
    {
        VoidCallThrows(new InvalidOperationException("not a JS problem"));

        await Should.ThrowAsync<InvalidOperationException>(
            async () => await _js.TryInvokeVoidAsync("noop"));
    }

    [Fact(DisplayName = "TryInvokeAsync returns the value on success")]
    public async Task TryInvokeAsync_ReturnsValue_OnSuccess()
    {
        _js.InvokeAsync<int>(Arg.Any<string>(), Arg.Any<object?[]?>())
            .ReturnsForAnyArgs(ValueTask.FromResult(42));

        var result = await _js.TryInvokeAsync<int>("answer");

        result.Status.ShouldBe(JsInvokeStatus.Success);
        result.Value.ShouldBe(42);
    }

    [Fact(DisplayName = "TryInvokeAsync yields the default value when the runtime is gone")]
    public async Task TryInvokeAsync_YieldsDefaultValue_WhenRuntimeIsGone()
    {
        ValueCallThrows(new JSDisconnectedException("circuit gone"));

        var result = await _js.TryInvokeAsync<int>("answer");

        result.Status.ShouldBe(JsInvokeStatus.Disconnected);
        result.Value.ShouldBe(0);
        result.Exception.ShouldBeNull();
    }

    [Fact(DisplayName = "TryInvokeAsync reports Failed and carries the exception on a JS error")]
    public async Task TryInvokeAsync_ReportsFailed_AndCarriesException_OnJsError()
    {
        var jsError = new JSException("TypeError");
        ValueCallThrows(jsError);

        var result = await _js.TryInvokeAsync<int>("answer");

        result.Status.ShouldBe(JsInvokeStatus.Failed);
        result.Exception.ShouldBeSameAs(jsError);
    }

    [Fact(DisplayName = "InvokeVoidSafeAsync swallows disconnection")]
    public async Task InvokeVoidSafeAsync_SwallowsDisconnection()
    {
        VoidCallThrows(new JSDisconnectedException("circuit gone"));

        await Should.NotThrowAsync(async () => await _js.InvokeVoidSafeAsync("noop"));
    }

    [Fact(DisplayName = "InvokeVoidSafeAsync rethrows genuine JS errors")]
    public async Task InvokeVoidSafeAsync_RethrowsGenuineJsErrors()
    {
        VoidCallThrows(new JSException("ReferenceError"));

        await Should.ThrowAsync<JSException>(async () => await _js.InvokeVoidSafeAsync("boom"));
    }

    [Fact(DisplayName = "InvokeSafeAsync yields the default value when the runtime is gone")]
    public async Task InvokeSafeAsync_YieldsDefaultValue_WhenRuntimeIsGone()
    {
        ValueCallThrows(new JSDisconnectedException("circuit gone"));

        var value = await _js.InvokeSafeAsync<int>("answer");

        value.ShouldBe(0);
    }

    [Fact(DisplayName = "InvokeSafeAsync rethrows genuine JS errors")]
    public async Task InvokeSafeAsync_RethrowsGenuineJsErrors()
    {
        ValueCallThrows(new JSException("TypeError"));

        await Should.ThrowAsync<JSException>(async () => await _js.InvokeSafeAsync<int>("answer"));
    }
}
