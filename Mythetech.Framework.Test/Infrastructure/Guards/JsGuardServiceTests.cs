using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Mythetech.Framework.Infrastructure.Guards;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Guards;

public class JsGuardServiceTests
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<JsGuardService> _logger;
    private readonly JsGuardService _service;

    public JsGuardServiceTests()
    {
        _jsRuntime = Substitute.For<IJSRuntime>();
        _logger = Substitute.For<ILogger<JsGuardService>>();
        _service = new JsGuardService(_logger);
    }

    [Fact(DisplayName = "IsReady returns false for unknown guard")]
    public void IsReady_ReturnsFalse_ForUnknownGuard()
    {
        _service.IsReady("unknown").ShouldBeFalse();
    }

    [Fact(DisplayName = "WaitForReadyAsync returns true when JS resolves true")]
    public async Task WaitForReadyAsync_ReturnsTrue_WhenJsResolvesTrue()
    {
        _jsRuntime
            .InvokeAsync<bool>("waitForJsGuard", Arg.Any<object[]>())
            .ReturnsForAnyArgs(ValueTask.FromResult(true));

        var result = await _service.WaitForReadyAsync(_jsRuntime, "monaco");

        result.ShouldBeTrue();
        _service.IsReady("monaco").ShouldBeTrue();
    }

    [Fact(DisplayName = "WaitForReadyAsync returns false and logs warning on timeout")]
    public async Task WaitForReadyAsync_ReturnsFalse_AndLogsWarning_OnTimeout()
    {
        _jsRuntime
            .InvokeAsync<bool>("waitForJsGuard", Arg.Any<object[]>())
            .ReturnsForAnyArgs(ValueTask.FromResult(false));

        var result = await _service.WaitForReadyAsync(_jsRuntime, "monaco");

        result.ShouldBeFalse();
        _service.IsReady("monaco").ShouldBeFalse();
        _logger.ReceivedWithAnyArgs(1).LogWarning(default(string));
    }

    [Fact(DisplayName = "WaitForReadyAsync does not cache false so re-render retries naturally")]
    public async Task WaitForReadyAsync_DoesNotCacheFalse()
    {
        _jsRuntime
            .InvokeAsync<bool>("waitForJsGuard", Arg.Any<object[]>())
            .ReturnsForAnyArgs(ValueTask.FromResult(false), ValueTask.FromResult(true));

        var first = await _service.WaitForReadyAsync(_jsRuntime, "monaco");
        var second = await _service.WaitForReadyAsync(_jsRuntime, "monaco");

        first.ShouldBeFalse();
        second.ShouldBeTrue();
        _service.IsReady("monaco").ShouldBeTrue();
        _ = _jsRuntime.ReceivedWithAnyArgs(2).InvokeAsync<bool>(default!, default);
    }

    [Fact(DisplayName = "WaitForReadyAsync skips JS call if already ready")]
    public async Task WaitForReadyAsync_SkipsJsCall_IfAlreadyReady()
    {
        _jsRuntime
            .InvokeAsync<bool>("waitForJsGuard", Arg.Any<object[]>())
            .ReturnsForAnyArgs(ValueTask.FromResult(true));

        await _service.WaitForReadyAsync(_jsRuntime, "monaco");
        var result = await _service.WaitForReadyAsync(_jsRuntime, "monaco");

        result.ShouldBeTrue();
        // Should only have called JS once
        _ = _jsRuntime.ReceivedWithAnyArgs(1).InvokeAsync<bool>(default!, default);
    }

    [Fact(DisplayName = "WaitForReadyAsync passes default 15-second timeout to JS")]
    public async Task WaitForReadyAsync_PassesDefaultTimeoutToJs()
    {
        _jsRuntime
            .InvokeAsync<bool>("waitForJsGuard", Arg.Any<object[]>())
            .ReturnsForAnyArgs(ValueTask.FromResult(true));

        await _service.WaitForReadyAsync(_jsRuntime, "monaco");

        _ = _jsRuntime.Received(1).InvokeAsync<bool>(
            "waitForJsGuard",
            Arg.Is<object[]>(a => a.Length == 2 && (string)a[0] == "monaco" && (int)a[1] == 15000));
    }

    [Fact(DisplayName = "WaitForReadyAsync passes explicit timeout to JS as milliseconds")]
    public async Task WaitForReadyAsync_PassesExplicitTimeoutToJs()
    {
        _jsRuntime
            .InvokeAsync<bool>("waitForJsGuard", Arg.Any<object[]>())
            .ReturnsForAnyArgs(ValueTask.FromResult(true));

        await _service.WaitForReadyAsync(_jsRuntime, "monaco", TimeSpan.FromSeconds(30));

        _ = _jsRuntime.Received(1).InvokeAsync<bool>(
            "waitForJsGuard",
            Arg.Is<object[]>(a => a.Length == 2 && (string)a[0] == "monaco" && (int)a[1] == 30000));
    }

    [Fact(DisplayName = "ResetAsync clears ready state and invokes clearJsGuard")]
    public async Task ResetAsync_ClearsState_AndInvokesClearJsGuard()
    {
        _jsRuntime
            .InvokeAsync<bool>("waitForJsGuard", Arg.Any<object[]>())
            .ReturnsForAnyArgs(ValueTask.FromResult(true));

        await _service.WaitForReadyAsync(_jsRuntime, "monaco");
        _service.IsReady("monaco").ShouldBeTrue();

        await _service.ResetAsync(_jsRuntime, "monaco");

        _service.IsReady("monaco").ShouldBeFalse();
        _ = _jsRuntime.Received(1).InvokeAsync<Microsoft.JSInterop.Infrastructure.IJSVoidResult>(
            "clearJsGuard",
            Arg.Is<object[]>(a => a.Length == 1 && (string)a[0] == "monaco"));
    }

    [Fact(DisplayName = "Multiple guards track independently")]
    public async Task MultipleGuards_TrackIndependently()
    {
        _jsRuntime
            .InvokeAsync<bool>("waitForJsGuard", Arg.Any<object[]>())
            .ReturnsForAnyArgs(ValueTask.FromResult(true));

        await _service.WaitForReadyAsync(_jsRuntime, "monaco");

        _service.IsReady("monaco").ShouldBeTrue();
        _service.IsReady("easymde").ShouldBeFalse();
    }
}
