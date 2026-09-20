using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Plugins;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Components.AppContextDrawer;

public class InitialPanelTests : BunitContext
{
    public InitialPanelTests()
    {
        Services.AddMudServices();
        Services.AddSingleton(Substitute.For<IMessageBus>());
        Services.AddSingleton(new PluginState());
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact(DisplayName = "An ActivePanelId supplied up front renders that panel's content")]
    public void InitialActivePanelId_RendersPanelContent()
    {
        var cut = Render<InitialPanelTestHost>(p => p.Add(x => x.ActivePanelId, "second"));

        cut.Markup.ShouldContain("second panel body");
        cut.Markup.ShouldContain("Second Panel");
        cut.Markup.ShouldNotContain("No Item Selected");
    }

    [Fact(DisplayName = "An unknown ActivePanelId leaves the empty state in place")]
    public void UnknownActivePanelId_ShowsEmptyState()
    {
        var cut = Render<InitialPanelTestHost>(p => p.Add(x => x.ActivePanelId, "missing"));

        cut.Markup.ShouldContain("No Item Selected");
    }

    [Fact(DisplayName = "No ActivePanelId leaves the empty state in place")]
    public void NoActivePanelId_ShowsEmptyState()
    {
        var cut = Render<InitialPanelTestHost>();

        cut.Markup.ShouldContain("No Item Selected");
    }
}
