using Bunit;
using MudBlazor;
using MudBlazor.Services;
using Mythetech.Framework.Components.Buttons;
using Shouldly;

namespace Mythetech.Framework.Test.Components.Buttons;

public class IconButtonTooltipTests : BunitContext
{
    public IconButtonTooltipTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Render<MudPopoverProvider>();
    }

    [Fact(DisplayName = "An icon button's tooltip is primary unless told otherwise")]
    public void Tooltip_DefaultsToPrimary()
    {
        var cut = Render<MtIconButton>(p => p
            .Add(x => x.Icon, "home")
            .Add(x => x.Tooltip, "Home"));

        cut.FindComponent<MudTooltip>().Instance.Color.ShouldBe(Color.Primary);
    }

    [Fact(DisplayName = "An icon button passes a supplied tooltip colour to its tooltip")]
    public void Tooltip_UsesSuppliedColour()
    {
        var cut = Render<MtIconButton>(p => p
            .Add(x => x.Icon, "home")
            .Add(x => x.Tooltip, "Home")
            .Add(x => x.TooltipColor, Color.Default));

        cut.FindComponent<MudTooltip>().Instance.Color.ShouldBe(Color.Default);
    }
}
