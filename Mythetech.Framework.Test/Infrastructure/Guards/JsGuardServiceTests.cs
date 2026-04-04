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
            .InvokeAsync<bool>("waitForJsGuard", Arg.Is<object[]>(a => (string)a[0] == "monaco"))
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
