using System.Text.RegularExpressions;
using Shouldly;

namespace Mythetech.Framework.Test.Components.Settings;

public partial class DynamicSettingControlStylesTests
{
    [GeneratedRegex(@"[Cc]lass=""(mf-[a-z-]+)""")]
    private static partial Regex EditorClass();

    [GeneratedRegex(@"([^{}]+)\{")]
    private static partial Regex Selector();

    [Fact]
    public void Rules_for_classes_an_editor_renders_reach_through_the_editor_component()
    {
        var settingsDirectory = Path.Combine(FindRepoRoot(), "Mythetech.Framework", "Components", "Settings");
        var editorClasses = Directory.EnumerateFiles(Path.Combine(settingsDirectory, "Editors"), "*.razor")
            .SelectMany(file => EditorClass().Matches(File.ReadAllText(file)).Select(m => m.Groups[1].Value))
            .ToHashSet();
        var css = File.ReadAllText(Path.Combine(settingsDirectory, "DynamicSettingControl.razor.css"));

        var unreachable = Selector().Matches(css)
            .Select(m => m.Groups[1].Value.Trim())
            .Where(selector => editorClasses.Any(c => Regex.IsMatch(selector, $@"\.{c}\b")) && !selector.Contains("::deep"))
            .ToList();

        editorClasses.ShouldNotBeEmpty();
        unreachable.ShouldBeEmpty("scoped rules never match elements an editor component renders unless they go through ::deep");
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Mythetech.Framework.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not find the repository root.");
    }
}
