using System.Text.RegularExpressions;
using Shouldly;

namespace Mythetech.Framework.Test.Components.Input;

public partial class InputFieldStylesTests
{
    [GeneratedRegex(@"border-radius:\s*([^;]+);")]
    private static partial Regex BorderRadius();

    [Theory(DisplayName = "An input field's corners follow the theme's border radius")]
    [InlineData("MtTextField")]
    [InlineData("MtNumericField")]
    [InlineData("MtSelect")]
    public void Corners_follow_the_theme(string component)
    {
        var css = File.ReadAllText(Path.Combine(SourceTree.Root, "Mythetech.Framework", "Components", "Input", $"{component}.razor.css"));

        var radii = BorderRadius().Matches(css).Select(match => match.Groups[1].Value.Trim()).ToList();

        radii.ShouldNotBeEmpty();
        radii.ShouldAllBe(radius => radius == "inherit" || radius.Contains("var(--mud-default-borderradius)"));
    }
}
