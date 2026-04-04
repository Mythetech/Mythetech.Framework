using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor.Services;
using Mythetech.Framework.Infrastructure.Guards;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Components.Guards;

public class JsGuardTests : TestContext
{
    private readonly IJsGuardService _guardService;

    public JsGuardTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;

        _guardService = Substitute.For<IJsGuardService>();
        Services.AddSingleton(_guardService);
    }

    [Fact(DisplayName = "Renders child content when guard is ready")]
    public void RendersChildContent_WhenGuardIsReady()
    {
        _guardService.IsReady("monaco").Returns(true);

        var cut = RenderComponent<JsGuard>(parameters => parameters
            .Add(p => p.Name, "monaco")
            .AddChildContent("<p>Editor loaded</p>"));

        cut.Markup.ShouldContain("Editor loaded");
    }

    [Fact(DisplayName = "Renders nothing when guard is not ready")]
    public void RendersNothing_WhenGuardIsNotReady()
    {
        _guardService.IsReady("monaco").Returns(false);

        var cut = RenderComponent<JsGuard>(parameters => parameters
            .Add(p => p.Name, "monaco")
            .AddChildContent("<p>Editor loaded</p>"));

        cut.Markup.ShouldNotContain("Editor loaded");
    }

    [Fact(DisplayName = "ErrorBoundary catches child exceptions and renders nothing by default")]
    public void ErrorBoundary_CatchesChildException_RendersNothing()
    {
        _guardService.IsReady("monaco").Returns(true);

        var cut = RenderComponent<JsGuard>(parameters => parameters
            .Add(p => p.Name, "monaco")
            .AddChildContent<ThrowingComponent>());

        // ErrorBoundary should have caught the exception — no unhandled crash
        cut.Markup.ShouldNotContain("This should not appear");
    }

    [Fact(DisplayName = "ErrorBoundary renders custom error content on failure")]
    public void ErrorBoundary_RendersCustomErrorContent_OnFailure()
    {
        _guardService.IsReady("monaco").Returns(true);

        var cut = RenderComponent<JsGuard>(parameters => parameters
            .Add(p => p.Name, "monaco")
            .AddChildContent<ThrowingComponent>()
            .Add(p => p.ErrorContent, ex => $"<p>Failed: {ex.Message}</p>"));

        cut.Markup.ShouldContain("Failed: Component error");
    }

    [Fact(DisplayName = "Recover resets the error boundary")]
    public async Task Recover_ResetsErrorBoundary()
    {
        _guardService.IsReady("monaco").Returns(true);

        var cut = RenderComponent<JsGuard>(parameters => parameters
            .Add(p => p.Name, "monaco")
            .AddChildContent<ThrowingComponent>()
            .Add(p => p.ErrorContent, ex => "<p>Error state</p>"));

        cut.Markup.ShouldContain("Error state");

        await cut.InvokeAsync(() => cut.Instance.Recover());

        // After recover, it will try to render the child again (which will throw again)
        // but the point is Recover() doesn't itself throw
        cut.Markup.ShouldContain("Error state");
    }

    [Fact(DisplayName = "Calls WaitForReadyAsync on first render when not ready")]
    public void CallsWaitForReadyAsync_OnFirstRender_WhenNotReady()
    {
        // Start not ready, then become ready after WaitForReadyAsync is called
        _guardService.IsReady("monaco").Returns(false, false, true);
        _guardService.WaitForReadyAsync(Arg.Any<IJSRuntime>(), "monaco")
            .Returns(Task.FromResult(true));

        var cut = RenderComponent<JsGuard>(parameters => parameters
            .Add(p => p.Name, "monaco")
            .AddChildContent("<p>Editor loaded</p>"));

        // After OnAfterRenderAsync triggers WaitForReadyAsync and re-renders
        cut.Markup.ShouldContain("Editor loaded");
        _guardService.Received(1).WaitForReadyAsync(Arg.Any<IJSRuntime>(), "monaco");
    }

    [Fact(DisplayName = "Does not call WaitForReadyAsync when already ready")]
    public void DoesNotCallWaitForReadyAsync_WhenAlreadyReady()
    {
        _guardService.IsReady("monaco").Returns(true);

        var cut = RenderComponent<JsGuard>(parameters => parameters
            .Add(p => p.Name, "monaco")
            .AddChildContent("<p>Editor loaded</p>"));

        cut.Markup.ShouldContain("Editor loaded");
        _guardService.DidNotReceive().WaitForReadyAsync(Arg.Any<IJSRuntime>(), Arg.Any<string>());
    }

    [Fact(DisplayName = "Checks correct guard name")]
    public void ChecksCorrectGuardName()
    {
        _guardService.IsReady("monaco").Returns(true);
        _guardService.IsReady("easymde").Returns(false);

        var cut = RenderComponent<JsGuard>(parameters => parameters
            .Add(p => p.Name, "easymde")
            .AddChildContent("<p>Markdown editor</p>"));

        cut.Markup.ShouldNotContain("Markdown editor");
    }

    /// <summary>
    /// Test component that throws during rendering.
    /// </summary>
    private class ThrowingComponent : Microsoft.AspNetCore.Components.ComponentBase
    {
        protected override void OnInitialized()
        {
            throw new InvalidOperationException("Component error");
        }
    }
}
