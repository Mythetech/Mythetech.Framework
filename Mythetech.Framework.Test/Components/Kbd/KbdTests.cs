using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Mythetech.Framework.Components.Kbd;
using NSubstitute;
using Shouldly;
using KbdComponent = Mythetech.Framework.Components.Kbd.MtKbd;

namespace Mythetech.Framework.Test.Components.Kbd;

public class KbdTests : TestContext
{
    public KbdTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IPlatformDetector>(new DefaultPlatformDetector());
    }

    [Fact(DisplayName = "Renders key text inside a <kbd> element")]
    public void Renders_key_text()
    {
        var cut = RenderComponent<KbdComponent>(p => p.Add(k => k.Key, "K"));

        cut.Find("kbd").TextContent.Trim().ShouldBe("K");
    }

    [Fact(DisplayName = "Platform Auto with non-macOS detector renders Ctrl as Ctrl")]
    public void Auto_non_mac_renders_ctrl_as_ctrl()
    {
        var cut = RenderComponent<KbdComponent>(p => p.Add(k => k.Key, "Ctrl"));

        cut.Find("kbd").TextContent.Trim().ShouldBe("Ctrl");
    }

    [Fact(DisplayName = "Platform Auto with macOS detector renders Ctrl as ⌃")]
    public void Auto_mac_renders_ctrl_as_symbol()
    {
        var detector = Substitute.For<IPlatformDetector>();
        detector.IsMacOS.Returns(true);
        Services.AddSingleton(detector);

        var cut = RenderComponent<KbdComponent>(p => p.Add(k => k.Key, "Ctrl"));

        cut.Find("kbd").TextContent.Trim().ShouldBe("\u2303");
    }

    [Fact(DisplayName = "Platform MacOS override renders Cmd as ⌘ regardless of detector")]
    public void MacOS_override_renders_symbols()
    {
        var cut = RenderComponent<KbdComponent>(p => p
            .Add(k => k.Key, "Cmd")
            .Add(k => k.Platform, KbdPlatform.MacOS));

        cut.Find("kbd").TextContent.Trim().ShouldBe("\u2318");
    }

    [Fact(DisplayName = "Platform Default override renders Cmd as Ctrl regardless of detector")]
    public void Default_override_renders_ctrl()
    {
        var detector = Substitute.For<IPlatformDetector>();
        detector.IsMacOS.Returns(true);
        Services.AddSingleton(detector);

        var cut = RenderComponent<KbdComponent>(p => p
            .Add(k => k.Key, "Cmd")
            .Add(k => k.Platform, KbdPlatform.Default));

        cut.Find("kbd").TextContent.Trim().ShouldBe("Ctrl");
    }

    [Fact(DisplayName = "PlatformAware false renders text as-is even with macOS detector")]
    public void PlatformAware_false_renders_as_is()
    {
        var detector = Substitute.For<IPlatformDetector>();
        detector.IsMacOS.Returns(true);
        Services.AddSingleton(detector);

        var cut = RenderComponent<KbdComponent>(p => p
            .Add(k => k.Key, "Ctrl")
            .Add(k => k.PlatformAware, false));

        cut.Find("kbd").TextContent.Trim().ShouldBe("Ctrl");
    }

    [Theory(DisplayName = "macOS symbol mapping")]
    [InlineData("Ctrl", "\u2303")]
    [InlineData("Alt", "\u2325")]
    [InlineData("Shift", "\u21E7")]
    [InlineData("Cmd", "\u2318")]
    [InlineData("Meta", "\u2318")]
    [InlineData("Enter", "\u23CE")]
    [InlineData("Esc", "\u238B")]
    [InlineData("Backspace", "\u232B")]
    [InlineData("Tab", "\u21E5")]
    public void MacOS_maps_modifier_keys(string input, string expected)
    {
        var cut = RenderComponent<KbdComponent>(p => p
            .Add(k => k.Key, input)
            .Add(k => k.Platform, KbdPlatform.MacOS));

        cut.Find("kbd").TextContent.Trim().ShouldBe(expected);
    }

    [Fact(DisplayName = "Non-macOS maps Cmd to Ctrl")]
    public void Non_mac_maps_cmd_to_ctrl()
    {
        var cut = RenderComponent<KbdComponent>(p => p
            .Add(k => k.Key, "Cmd")
            .Add(k => k.Platform, KbdPlatform.Default));

        cut.Find("kbd").TextContent.Trim().ShouldBe("Ctrl");
    }

    [Fact(DisplayName = "Unknown keys pass through unchanged")]
    public void Unknown_keys_pass_through()
    {
        var cut = RenderComponent<KbdComponent>(p => p
            .Add(k => k.Key, "F5")
            .Add(k => k.Platform, KbdPlatform.MacOS));

        cut.Find("kbd").TextContent.Trim().ShouldBe("F5");
    }
}
