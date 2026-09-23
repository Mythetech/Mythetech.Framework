using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Plugins;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Components.AppContextDrawer;

public class ContextPanelRouteMatchTests : BunitContext
{
    private readonly BunitNavigationManager _nav;

    public ContextPanelRouteMatchTests()
    {
        Services.AddMudServices();
        Services.AddSingleton(Substitute.For<IMessageBus>());
        Services.AddSingleton(new PluginState());
        JSInterop.Mode = JSRuntimeMode.Loose;

        _nav = Services.GetRequiredService<BunitNavigationManager>();
    }

    private static bool IsHighlighted(IRenderedComponent<RouteMatchTestHost> cut, string panelId) =>
        cut.Find($"[data-panel-id={panelId}] button").ClassList.Contains("context-item-active");

    [Fact(DisplayName = "An exact route match panel lights up on its own route")]
    public void ExactRouteMatch_OnExactRoute_IsHighlighted()
    {
        _nav.NavigateTo("Sessions");

        var cut = Render<RouteMatchTestHost>();

        IsHighlighted(cut, "sessions").ShouldBeTrue();
    }

    [Fact(DisplayName = "An exact route match panel stays dark on a route nested beneath it")]
    public void ExactRouteMatch_OnNestedRoute_IsNotHighlighted()
    {
        _nav.NavigateTo("Sessions/1");

        var cut = Render<RouteMatchTestHost>();

        IsHighlighted(cut, "sessions").ShouldBeFalse();
        IsHighlighted(cut, "session-1").ShouldBeTrue();
    }

    [Fact(DisplayName = "An exact route match panel ignores the query string")]
    public void ExactRouteMatch_WithQueryString_IsHighlighted()
    {
        _nav.NavigateTo("Sessions?create=true");

        var cut = Render<RouteMatchTestHost>();

        IsHighlighted(cut, "sessions").ShouldBeTrue();
    }

    [Fact(DisplayName = "A prefix match panel still lights up on routes nested beneath it")]
    public void PrefixMatch_OnNestedRoute_IsHighlighted()
    {
        _nav.NavigateTo("Sessions/1/agents/2");

        var cut = Render<RouteMatchTestHost>();

        IsHighlighted(cut, "session-1").ShouldBeTrue();
    }

    [Fact(DisplayName = "Double clicking an exact route match panel from a nested route navigates to its page")]
    public void ExactRouteMatch_DoubleClickFromNestedRoute_Navigates()
    {
        _nav.NavigateTo("Sessions/1");
        var cut = Render<RouteMatchTestHost>();

        cut.Find("[data-panel-id=sessions] button").DoubleClick();

        _nav.Uri.ShouldBe($"{_nav.BaseUri}Sessions");
    }

    [Fact(DisplayName = "Double clicking an exact route match panel from elsewhere navigates to its page")]
    public void ExactRouteMatch_DoubleClickFromElsewhere_Navigates()
    {
        _nav.NavigateTo("Dashboard");
        var cut = Render<RouteMatchTestHost>();

        cut.Find("[data-panel-id=sessions] button").DoubleClick();

        _nav.Uri.ShouldBe($"{_nav.BaseUri}Sessions");
    }
}
