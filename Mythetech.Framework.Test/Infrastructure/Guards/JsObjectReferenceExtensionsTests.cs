using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Mythetech.Framework.Infrastructure.Guards;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Guards;

public class JsObjectReferenceExtensionsTests
{
    private readonly IJSObjectReference _module = Substitute.For<IJSObjectReference>();

    private void VoidCallThrows(Exception ex) =>
        _module.InvokeAsync<IJSVoidResult>(Arg.Any<string>(), Arg.Any<object?[]?>()).ThrowsAsyncForAnyArgs(ex);

    private void ValueCallThrows(Exception ex) =>
        _module.InvokeAsync<int>(Arg.Any<string>(), Arg.Any<object?[]?>()).ThrowsAsyncForAnyArgs(ex);

    public static TheoryData<Exception> Disconnections => new()
    {
        new JSDisconnectedException("circuit gone"),
        new ObjectDisposedException("WebView"),
    };

    [Fact(DisplayName = "Module TryInvokeVoidAsync reports success when the call completes")]
    public async Task TryInvokeVoidAsync_ReportsSuccess_WhenCallCompletes()
    {
        var result = await _module.TryInvokeVoidAsync("noop");

        result.Status.ShouldBe(JsInvokeStatus.Success);
        result.Exception.ShouldBeNull();
    }

    [Theory(DisplayName = "Module TryInvokeVoidAsync reports Disconnected when the runtime is gone")]
    [MemberData(nameof(Disconnections))]
    public async Task TryInvokeVoidAsync_ReportsDisconnected_WhenRuntimeIsGone(Exception disconnection)
    {
        VoidCallThrows(disconnection);

        var result = await _module.TryInvokeVoidAsync("noop");

        result.Status.ShouldBe(JsInvokeStatus.Disconnected);
        result.Exception.ShouldBeNull();
    }

    [Fact(DisplayName = "Module TryInvokeVoidAsync reports Failed and carries the exception on a JS error")]
    public async Task TryInvokeVoidAsync_ReportsFailed_AndCarriesException_OnJsError()
    {
        var jsError = new JSException("ReferenceError: foo is not defined");
        VoidCallThrows(jsError);

        var result = await _module.TryInvokeVoidAsync("boom");

        result.Status.ShouldBe(JsInvokeStatus.Failed);
        result.Exception.ShouldBeSameAs(jsError);
    }

    [Fact(DisplayName = "Module TryInvokeVoidAsync lets unrelated exceptions propagate")]
    public async Task TryInvokeVoidAsync_LetsUnrelatedExceptionsPropagate()
    {
        VoidCallThrows(new InvalidOperationException("not a JS problem"));

        await Should.ThrowAsync<InvalidOperationException>(
            async () => await _module.TryInvokeVoidAsync("noop"));
    }

    [Fact(DisplayName = "Module TryInvokeAsync returns the value on success")]
    public async Task TryInvokeAsync_ReturnsValue_OnSuccess()
    {
        _module.InvokeAsync<int>(Arg.Any<string>(), Arg.Any<object?[]?>())
            .ReturnsForAnyArgs(ValueTask.FromResult(42));

        var result = await _module.TryInvokeAsync<int>("answer");

        result.Status.ShouldBe(JsInvokeStatus.Success);
        result.Value.ShouldBe(42);
    }

    [Fact(DisplayName = "Module TryInvokeAsync yields the default value when the runtime is gone")]
    public async Task TryInvokeAsync_YieldsDefaultValue_WhenRuntimeIsGone()
    {
        ValueCallThrows(new JSDisconnectedException("circuit gone"));

        var result = await _module.TryInvokeAsync<int>("answer");

        result.Status.ShouldBe(JsInvokeStatus.Disconnected);
        result.Value.ShouldBe(0);
        result.Exception.ShouldBeNull();
    }

    [Fact(DisplayName = "Module TryInvokeAsync reports Failed and carries the exception on a JS error")]
    public async Task TryInvokeAsync_ReportsFailed_AndCarriesException_OnJsError()
    {
        var jsError = new JSException("TypeError");
        ValueCallThrows(jsError);

        var result = await _module.TryInvokeAsync<int>("answer");

        result.Status.ShouldBe(JsInvokeStatus.Failed);
        result.Exception.ShouldBeSameAs(jsError);
    }

    [Fact(DisplayName = "Module InvokeVoidSafeAsync swallows disconnection")]
    public async Task InvokeVoidSafeAsync_SwallowsDisconnection()
    {
        VoidCallThrows(new JSDisconnectedException("circuit gone"));

        await Should.NotThrowAsync(async () => await _module.InvokeVoidSafeAsync("noop"));
    }

    [Fact(DisplayName = "Module InvokeVoidSafeAsync rethrows genuine JS errors")]
    public async Task InvokeVoidSafeAsync_RethrowsGenuineJsErrors()
    {
        VoidCallThrows(new JSException("ReferenceError"));

        await Should.ThrowAsync<JSException>(async () => await _module.InvokeVoidSafeAsync("boom"));
    }

    [Fact(DisplayName = "Module InvokeSafeAsync yields the default value when the runtime is gone")]
    public async Task InvokeSafeAsync_YieldsDefaultValue_WhenRuntimeIsGone()
    {
        ValueCallThrows(new JSDisconnectedException("circuit gone"));

        var value = await _module.InvokeSafeAsync<int>("answer");

        value.ShouldBe(0);
    }

    [Fact(DisplayName = "Module InvokeSafeAsync rethrows genuine JS errors")]
    public async Task InvokeSafeAsync_RethrowsGenuineJsErrors()
    {
        ValueCallThrows(new JSException("TypeError"));

        await Should.ThrowAsync<JSException>(async () => await _module.InvokeSafeAsync<int>("answer"));
    }
}
