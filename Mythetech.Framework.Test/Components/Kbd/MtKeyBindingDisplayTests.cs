using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using MudBlazor.Utilities;
using Mythetech.Framework.Components.Kbd;
using Mythetech.Framework.Infrastructure.Keyboard;
using Shouldly;

namespace Mythetech.Framework.Test.Components.Kbd;

public class MtKeyBindingDisplayTests : BunitContext
{
    public MtKeyBindingDisplayTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IPlatformDetector>(new DefaultPlatformDetector());
    }

    [Fact(DisplayName = "Renders one kbd element per token")]
    public void Renders_kbd_per_token()
    {
        var cut = Render<MtKeyBindingDisplay>(parameters => parameters
            .Add(p => p.Binding, KeyBinding.CtrlOrCmd(JsKey.KeyK, shift: true)));

        cut.FindAll("kbd").Count.ShouldBe(3);
    }

    [Fact(DisplayName = "Renders Not set for an unbound action")]
    public void Renders_not_set_when_unbound()
    {
        var cut = Render<MtKeyBindingDisplay>(parameters => parameters
            .Add(p => p.Binding, null));

        cut.FindAll("kbd").ShouldBeEmpty();
        cut.Find(".mf-keybinding-unset").TextContent.Trim().ShouldBe("Not set");
    }
}
