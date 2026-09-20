using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Plugins;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Components.AppContextDrawer;

public class CloseIconTests : BunitContext
{
    public CloseIconTests()
    {
        Services.AddMudServices();
        Services.AddSingleton(Substitute.For<IMessageBus>());
        Services.AddSingleton(new PluginState());
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact(DisplayName = "The panel close button defaults to the framework close icon")]
    public void CloseButton_DefaultsToCloseIcon()
    {
        var cut = Render<CloseIconTestHost>();

        cut.Find("[data-panel-id=plugin] button").Click();

        CloseButtonIconText(cut).ShouldBe("close");
    }

    [Fact(DisplayName = "The panel close button honours an overridden CloseIcon")]
    public void CloseButton_UsesSuppliedIcon()
    {
        var cut = Render<CloseIconTestHost>(p =>
            p.Add(x => x.CloseIcon, MythetechFrameworkIcons.ChevronLeft));

        cut.Find("[data-panel-id=plugin] button").Click();

        CloseButtonIconText(cut).ShouldBe("chevron_left");
    }

    private static string CloseButtonIconText(IRenderedComponent<CloseIconTestHost> cut) =>
        cut.Find(".app-context-content button .material-symbols-rounded").TextContent.Trim();
}
