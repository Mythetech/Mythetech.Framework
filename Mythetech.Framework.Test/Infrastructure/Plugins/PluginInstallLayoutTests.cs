using Microsoft.Extensions.Logging.Abstractions;
using Mythetech.Framework.Infrastructure.Plugins.Installation;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Plugins;

public class PluginInstallLayoutTests : IDisposable
{
    private readonly string _pluginsDirectory = Path.Combine(Path.GetTempPath(), $"plugin_install_layout_test_{Guid.NewGuid():N}");

    public PluginInstallLayoutTests()
    {
        Directory.CreateDirectory(_pluginsDirectory);
    }

    [Fact(DisplayName = "PrepareForLoad deletes marked folders at both depths and never returns them")]
    public void PrepareForLoad_DeletesMarkedFolders()
    {
        var markedKey = CreateFolderWithDll("Removed");
        PluginInstallLayout.MarkForRemoval(markedKey);
        var keptInstall = PluginInstallLayout.CreateInstallFolder(_pluginsDirectory, "FailedUpgrade");
        CompleteInstall(keptInstall);
        var markedInstall = PluginInstallLayout.CreateInstallFolder(_pluginsDirectory, "FailedUpgrade");
        WriteDll(markedInstall);
        PluginInstallLayout.MarkForRemoval(markedInstall);

        var folders = PluginInstallLayout.PrepareForLoad(_pluginsDirectory, NullLogger.Instance);

        Directory.Exists(markedKey).ShouldBeFalse();
        Directory.Exists(markedInstall).ShouldBeFalse();
        folders.ShouldBe([keptInstall]);
    }

    [Fact(DisplayName = "PrepareForLoad keeps only the newest install folder of a plugin")]
    public void PrepareForLoad_KeepsOnlyNewestInstallFolder()
    {
        var older = PluginInstallLayout.CreateInstallFolder(_pluginsDirectory, "Notes");
        CompleteInstall(older);
        var newer = PluginInstallLayout.CreateInstallFolder(_pluginsDirectory, "Notes");
        CompleteInstall(newer);

        var folders = PluginInstallLayout.PrepareForLoad(_pluginsDirectory, NullLogger.Instance);

        folders.ShouldBe([newer]);
        Directory.Exists(older).ShouldBeFalse();
        Directory.Exists(newer).ShouldBeTrue();
    }

    [Fact(DisplayName = "PrepareForLoad returns a legacy plugin folder that holds DLLs directly")]
    public void PrepareForLoad_LegacyFolderWithDlls_IsReturned()
    {
        var legacy = CreateFolderWithDll("Notes");
        Directory.CreateDirectory(Path.Combine(_pluginsDirectory, "Empty"));

        var folders = PluginInstallLayout.PrepareForLoad(_pluginsDirectory, NullLogger.Instance);

        folders.ShouldBe([legacy]);
    }

    [Fact(DisplayName = "PrepareForLoad returns the plugins directory when it holds loose DLLs")]
    public void PrepareForLoad_LooseDllsInRoot_ReturnsRoot()
    {
        WriteDll(_pluginsDirectory);

        var folders = PluginInstallLayout.PrepareForLoad(_pluginsDirectory, NullLogger.Instance);

        folders.ShouldBe([_pluginsDirectory]);
    }

    [Fact(DisplayName = "PrepareForLoad cleans a marked legacy folder but keeps the new install inside it")]
    public void PrepareForLoad_MarkedLegacyFolderWithNewInstall_KeepsNewInstall()
    {
        var legacy = CreateFolderWithDll("Notes");
        var legacyAssets = Path.Combine(legacy, "wwwroot");
        Directory.CreateDirectory(legacyAssets);
        File.WriteAllText(Path.Combine(legacyAssets, "notes.css"), string.Empty);
        var install = PluginInstallLayout.CreateInstallFolder(_pluginsDirectory, "Notes");
        CompleteInstall(install);
        PluginInstallLayout.MarkForRemoval(legacy);

        var folders = PluginInstallLayout.PrepareForLoad(_pluginsDirectory, NullLogger.Instance);

        folders.ShouldBe([install]);
        Directory.Exists(install).ShouldBeTrue();
        Directory.GetFiles(legacy).ShouldBeEmpty();
        Directory.GetDirectories(legacy).ShouldBe([install]);
    }

    [Fact(DisplayName = "CreateInstallFolder called twice for one key returns distinct folders in creation order")]
    public void CreateInstallFolder_TwiceForSameKey_ReturnsDistinctFolders()
    {
        var first = PluginInstallLayout.CreateInstallFolder(_pluginsDirectory, "Notes");
        var second = PluginInstallLayout.CreateInstallFolder(_pluginsDirectory, "Notes");

        second.ShouldNotBe(first);
        Directory.Exists(first).ShouldBeTrue();
        Directory.Exists(second).ShouldBeTrue();
        Path.GetDirectoryName(first).ShouldBe(Path.Combine(_pluginsDirectory, "Notes"));
        Path.GetFileName(first).ShouldStartWith(PluginInstallLayout.InstallFolderPrefix);
        string.CompareOrdinal(Path.GetFileName(second), Path.GetFileName(first)).ShouldBeGreaterThan(0);
    }

    [Fact(DisplayName = "CreateInstallFolder returns a folder marked for removal until its install completes")]
    public void CreateInstallFolder_NewFolder_IsMarkedUntilInstallCompletes()
    {
        var install = PluginInstallLayout.CreateInstallFolder(_pluginsDirectory, "Notes");
        WriteDll(install);

        File.Exists(Path.Combine(install, PluginInstallLayout.RemovedMarkerFileName)).ShouldBeTrue();
        var folders = PluginInstallLayout.PrepareForLoad(_pluginsDirectory, NullLogger.Instance);

        folders.ShouldBeEmpty();
        Directory.Exists(install).ShouldBeFalse();
    }

    [Fact(DisplayName = "PrepareForLoad deletes a newer unmarked install folder without a DLL and keeps the install before it")]
    public void PrepareForLoad_NewerEmptyUnmarkedInstall_KeepsPreviousInstall()
    {
        var installA = PluginInstallLayout.CreateInstallFolder(_pluginsDirectory, "Notes");
        CompleteInstall(installA);
        var installB = PluginInstallLayout.CreateInstallFolder(_pluginsDirectory, "Notes");
        File.Delete(Path.Combine(installB, PluginInstallLayout.RemovedMarkerFileName));

        var folders = PluginInstallLayout.PrepareForLoad(_pluginsDirectory, NullLogger.Instance);

        folders.ShouldBe([installA]);
        Directory.Exists(installA).ShouldBeTrue();
        Directory.Exists(installB).ShouldBeFalse();
    }

    [Fact(DisplayName = "PrepareForLoad returns nothing for a plugin whose only install folder has no DLL")]
    public void PrepareForLoad_OnlyInstallHasNoDll_ReturnsNothing()
    {
        var install = PluginInstallLayout.CreateInstallFolder(_pluginsDirectory, "Notes");
        File.Delete(Path.Combine(install, PluginInstallLayout.RemovedMarkerFileName));

        var folders = PluginInstallLayout.PrepareForLoad(_pluginsDirectory, NullLogger.Instance);

        folders.ShouldBeEmpty();
        Directory.Exists(install).ShouldBeFalse();
    }

    [Theory(DisplayName = "SanitizeFolderName falls back to 'plugin' for a name that is empty or only dots and whitespace")]
    [InlineData("..")]
    [InlineData(".")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" . . ")]
    public void SanitizeFolderName_NoUsableName_ReturnsFallback(string name)
    {
        PluginInstallLayout.SanitizeFolderName(name).ShouldBe("plugin");
    }

    [Fact(DisplayName = "SanitizeFolderName trims leading and trailing dots and whitespace")]
    public void SanitizeFolderName_TrimsDotsAndWhitespace()
    {
        PluginInstallLayout.SanitizeFolderName(" .Notes. ").ShouldBe("Notes");
    }

    [Fact(DisplayName = "CreateInstallFolder with the key '..' creates a folder inside the plugins directory")]
    public void CreateInstallFolder_ParentKey_StaysInsidePluginsDirectory()
    {
        var folder = PluginInstallLayout.CreateInstallFolder(_pluginsDirectory, "..");

        PluginInstallLayout.IsInside(folder, _pluginsDirectory).ShouldBeTrue();
        Path.GetDirectoryName(folder).ShouldBe(Path.Combine(_pluginsDirectory, "plugin"));
    }

    private string CreateFolderWithDll(string name)
    {
        var folder = Path.Combine(_pluginsDirectory, name);
        Directory.CreateDirectory(folder);
        WriteDll(folder);
        return folder;
    }

    private static void WriteDll(string folder) => File.WriteAllText(Path.Combine(folder, "Plugin.dll"), string.Empty);

    // What the installer does once a plugin from the folder has loaded.
    private static void CompleteInstall(string folder)
    {
        WriteDll(folder);
        File.Delete(Path.Combine(folder, PluginInstallLayout.RemovedMarkerFileName));
    }

    public void Dispose()
    {
        try { Directory.Delete(_pluginsDirectory, recursive: true); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }
}
