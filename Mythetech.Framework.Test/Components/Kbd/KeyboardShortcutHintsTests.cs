using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Mythetech.Framework.Components.Kbd;
using Shouldly;

namespace Mythetech.Framework.Test.Components.Kbd;

public class KeyboardShortcutHintsTests : TestContext
{
    public KeyboardShortcutHintsTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IPlatformDetector>(new DefaultPlatformDetector());
    }

    [Fact(DisplayName = "Renders with the mf-shortcut-hints CSS class")]
    public void Renders_with_css_class()
    {
        var cut = RenderComponent<KeyboardShortcutHints>(p => p
            .AddChildContent<ShortcutHint>(hint => hint
                .AddChildContent("test")));

        cut.Find(".mf-shortcut-hints").ShouldNotBeNull();
    }

    [Fact(DisplayName = "Renders child ShortcutHint content")]
    public void Renders_child_content()
    {
        var cut = RenderComponent<KeyboardShortcutHints>(p => p
            .AddChildContent<ShortcutHint>(hint => hint
                .AddChildContent("navigate")));

        cut.Markup.ShouldContain("navigate");
    }
}
