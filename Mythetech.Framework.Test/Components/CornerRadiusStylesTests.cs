using System.Text.RegularExpressions;
using Shouldly;

namespace Mythetech.Framework.Test.Components;

public partial class CornerRadiusStylesTests
{
    [GeneratedRegex(@"border-radius:\s*([^;]+);")]
    private static partial Regex BorderRadius();

    [Theory(DisplayName = "A component's corners follow the theme's border radius")]
    [InlineData("Components/Input/MtTextField.razor.css")]
    [InlineData("Components/Input/MtNumericField.razor.css")]
    [InlineData("Components/Input/MtSelect.razor.css")]
    [InlineData("Components/Navigation/MtNavLink.razor.css")]
    [InlineData("Components/Kbd/MtKbd.razor.css")]
    [InlineData("Components/CommandPalette/CommandPalettePanel.razor.css")]
    [InlineData("Components/Settings/DynamicSettingControl.razor.css")]
    [InlineData("Components/Settings/SettingsNavLink.razor.css")]
    [InlineData("Components/Settings/SettingsSectionDisplay.razor.css")]
    public void Corners_follow_the_theme(string stylesheet) => ShouldFollowTheTheme(Read(stylesheet));

    [Theory(DisplayName = "A shared rule's corners follow the theme's border radius")]
    [InlineData(".mf-dynamic-setting-control")]
    [InlineData(".mf-settings-content::-webkit-scrollbar-thumb")]
    public void Shared_rule_corners_follow_the_theme(string selector)
    {
        var rule = Regex.Match(Read("wwwroot/mythetech.css"), $@"(?m)^{Regex.Escape(selector)}\s*\{{([^}}]*)\}}");

        rule.Success.ShouldBeTrue();
        ShouldFollowTheTheme(rule.Groups[1].Value);
    }

    private static void ShouldFollowTheTheme(string css)
    {
        var radii = BorderRadius().Matches(css).Select(match => match.Groups[1].Value.Trim()).ToList();

        radii.ShouldNotBeEmpty();
        radii.ShouldAllBe(radius => radius == "inherit" || radius.Contains("var(--mud-default-borderradius)"));
    }

    private static string Read(string path) => File.ReadAllText(Path.Combine(SourceTree.Root, "Mythetech.Framework", path));
}
