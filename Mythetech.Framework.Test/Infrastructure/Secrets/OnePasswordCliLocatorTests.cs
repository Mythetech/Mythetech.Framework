using System.Runtime.InteropServices;
using Mythetech.Framework.Desktop.Secrets;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Secrets;

/// <summary>
/// Checks where the 1Password CLI is looked for, using a supplied PATH and file list rather than the real
/// filesystem, so the results do not depend on what is installed on the test machine.
/// </summary>
public class OnePasswordCliLocatorTests
{
    // What a macOS app launched from Finder typically gets: no Homebrew, no /usr/local/bin.
    private const string FinderPath = "/usr/bin:/bin:/usr/sbin:/sbin";

    [Fact(DisplayName = "op found on PATH is used before any fallback location")]
    public void OpOnPath_IsUsedFirst()
    {
        var located = OnePasswordCliLocator.Locate(
            OSPlatform.OSX,
            "/usr/bin:/Users/me/bin",
            ExistsOnly("/Users/me/bin/op", "/opt/homebrew/bin/op"));

        located.ShouldBe("/Users/me/bin/op");
    }

    [Theory(DisplayName = "macOS falls back to the Homebrew and /usr/local/bin locations when PATH lacks op")]
    [InlineData("/opt/homebrew/bin/op")]
    [InlineData("/usr/local/bin/op")]
    public void MacOS_OpNotOnPath_UsesInstallLocation(string installed)
    {
        OnePasswordCliLocator.Locate(OSPlatform.OSX, FinderPath, ExistsOnly(installed)).ShouldBe(installed);
    }

    [Fact(DisplayName = "macOS prefers Apple silicon Homebrew over /usr/local/bin when both exist")]
    public void MacOS_BothInstallLocations_PrefersHomebrew()
    {
        OnePasswordCliLocator.Locate(OSPlatform.OSX, FinderPath, ExistsOnly("/usr/local/bin/op", "/opt/homebrew/bin/op"))
            .ShouldBe("/opt/homebrew/bin/op");
    }

    [Theory(DisplayName = "Linux falls back to /usr/bin and /usr/local/bin when PATH lacks op")]
    [InlineData("/usr/bin/op")]
    [InlineData("/usr/local/bin/op")]
    public void Linux_OpNotOnPath_UsesInstallLocation(string installed)
    {
        OnePasswordCliLocator.Locate(OSPlatform.Linux, "/bin", ExistsOnly(installed)).ShouldBe(installed);
    }

    [Fact(DisplayName = "Linux does not look in macOS install locations")]
    public void Linux_IgnoresMacOSLocations()
    {
        OnePasswordCliLocator.Locate(OSPlatform.Linux, "/bin", ExistsOnly("/opt/homebrew/bin/op")).ShouldBe("op");
    }

    [Fact(DisplayName = "Windows finds op.exe on PATH")]
    public void Windows_OpOnPath_IsUsed()
    {
        var located = OnePasswordCliLocator.Locate(
            OSPlatform.Windows,
            @"C:\Windows\system32;C:\Tools\",
            ExistsOnly(@"C:\Tools\op.exe"),
            @"C:\Program Files");

        located.ShouldBe(@"C:\Tools\op.exe");
    }

    [Fact(DisplayName = "Windows reads quoted PATH entries")]
    public void Windows_QuotedPathEntry_IsUsed()
    {
        var located = OnePasswordCliLocator.Locate(
            OSPlatform.Windows,
            "\"C:\\Program Files\\Tools\";C:\\Windows",
            ExistsOnly(@"C:\Program Files\Tools\op.exe"),
            @"C:\Program Files");

        located.ShouldBe(@"C:\Program Files\Tools\op.exe");
    }

    [Fact(DisplayName = "Windows falls back to the documented Program Files install folder")]
    public void Windows_OpNotOnPath_UsesProgramFilesInstall()
    {
        var located = OnePasswordCliLocator.Locate(
            OSPlatform.Windows,
            @"C:\Windows\system32",
            ExistsOnly(@"C:\Program Files\1Password CLI\op.exe"),
            @"C:\Program Files");

        located.ShouldBe(@"C:\Program Files\1Password CLI\op.exe");
    }

    [Fact(DisplayName = "Install locations are still checked when PATH is not set")]
    public void NoPathVariable_UsesInstallLocation()
    {
        OnePasswordCliLocator.Locate(OSPlatform.OSX, null, ExistsOnly("/opt/homebrew/bin/op")).ShouldBe("/opt/homebrew/bin/op");
    }

    [Fact(DisplayName = "Empty and relative PATH entries are never searched")]
    public void RelativePathEntries_AreIgnored()
    {
        OnePasswordCliLocator.Locate(OSPlatform.OSX, "::bin:./tools", ExistsOnly("bin/op", "./tools/op", "/op"))
            .ShouldBe("op");
    }

    [Theory(DisplayName = "When op is nowhere to be found the bare command is returned")]
    [InlineData("OSX")]
    [InlineData("LINUX")]
    [InlineData("WINDOWS")]
    public void NotInstalled_ReturnsBareCommand(string platform)
    {
        OnePasswordCliLocator.Locate(OSPlatform.Create(platform), FinderPath, _ => false, @"C:\Program Files")
            .ShouldBe("op");
    }

    private static Func<string, bool> ExistsOnly(params string[] files)
    {
        var existing = files.ToHashSet(StringComparer.Ordinal);
        return existing.Contains;
    }
}
