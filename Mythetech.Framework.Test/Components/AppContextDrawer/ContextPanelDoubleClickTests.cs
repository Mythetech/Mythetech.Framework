using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Plugins;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Components.AppContextDrawer;

public class ContextPanelDoubleClickTests : BunitContext
{
    private readonly BunitNavigationManager _nav;

    public ContextPanelDoubleClickTests()
    {
        Services.AddMudServices();
        Services.AddSingleton(Substitute.For<IMessageBus>());
        Services.AddSingleton(new PluginState());
        JSInterop.Mode = JSRuntimeMode.Loose;

        _nav = Services.GetRequiredService<BunitNavigationManager>();
    }

    [Fact(DisplayName = "Double clicking a panel icon navigates to its route")]
    public void DoubleClick_NavigatesToRoute()
    {
        var cut = Render<DoubleClickTestHost>();

        cut.Find("[data-panel-id=messaging] button").DoubleClick();

        _nav.Uri.ShouldBe($"{_nav.BaseUri}Messaging");
    }

    [Fact(DisplayName = "Double clicking a panel icon closes the drawer")]
    public void DoubleClick_ClosesDrawer()
    {
        var cut = Render<DoubleClickTestHost>();

        cut.Find("[data-panel-id=messaging] button").DoubleClick();

        cut.Instance.IsOpen.ShouldBeFalse();
        cut.Instance.ActivePanelId.ShouldBeNull();
    }

    [Fact(DisplayName = "Double clicking a panel icon with no route does not navigate")]
    public void DoubleClick_WithoutRoutePrefix_DoesNotNavigate()
    {
        var cut = Render<DoubleClickTestHost>();
        var before = _nav.Uri;

        cut.Find("[data-panel-id=plugin] button").DoubleClick();

        _nav.Uri.ShouldBe(before);
    }

    [Fact(DisplayName = "Double clicking a panel icon with no route leaves the drawer state alone")]
    public void DoubleClick_WithoutRoutePrefix_LeavesDrawerStateAlone()
    {
        var cut = Render<DoubleClickTestHost>();

        cut.Find("[data-panel-id=plugin] button").Click();
        cut.Find("[data-panel-id=plugin] button").DoubleClick();

        cut.Instance.IsOpen.ShouldBeTrue();
        cut.Instance.ActivePanelId.ShouldBe("plugin");
    }

    [Fact(DisplayName = "Double clicking the panel for the page you are already on is a no-op")]
    public void DoubleClick_WhenAlreadyOnRoute_DoesNothing()
    {
        _nav.NavigateTo("Messaging");
        var cut = Render<DoubleClickTestHost>();

        cut.Find("[data-panel-id=messaging] button").Click();
        cut.Find("[data-panel-id=messaging] button").DoubleClick();

        _nav.History.Count.ShouldBe(1);
        cut.Instance.IsOpen.ShouldBeTrue();
        cut.Instance.ActivePanelId.ShouldBe("messaging");
    }

    [Fact(DisplayName = "Double clicking a panel navigates up when on a nested route")]
    public void DoubleClick_WhenOnNestedRoute_NavigatesToIndex()
    {
        _nav.NavigateTo("Messaging/detail-1");
        var cut = Render<DoubleClickTestHost>();

        cut.Find("[data-panel-id=messaging] button").DoubleClick();

        _nav.Uri.ShouldBe($"{_nav.BaseUri}Messaging");
    }

    [Fact(DisplayName = "Single clicking a panel icon does not navigate")]
    public void SingleClick_DoesNotNavigate()
    {
        var cut = Render<DoubleClickTestHost>();
        var before = _nav.Uri;

        cut.Find("[data-panel-id=messaging] button").Click();

        _nav.Uri.ShouldBe(before);
        cut.Instance.IsOpen.ShouldBeTrue();
    }
}
