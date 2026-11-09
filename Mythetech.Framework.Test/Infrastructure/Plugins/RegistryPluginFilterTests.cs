using System.Reflection;
using Mythetech.Framework.Infrastructure.Plugins;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Plugins;

public class RegistryPluginFilterTests
{
    private static RegistryPluginEntry Entry(string version = "2.0.0") => new()
    {
        Id = "a", Name = "A", Version = version, Uri = "u"
    };

    private static PluginInfo Installed(string version)
    {
        var manifest = Substitute.For<IPluginManifest>();
        manifest.Id.Returns("a");
        manifest.Version.Returns(Version.Parse(version));
        return new PluginInfo { Manifest = manifest, Assembly = Assembly.GetExecutingAssembly() };
    }

    [Fact(DisplayName = "ShouldList returns false for a deprecated entry")]
    public void ShouldList_Deprecated_ReturnsFalse()
    {
        var entry = Entry();
        entry.IsDeprecated = true;

        RegistryPluginFilter.ShouldList(entry, null, false, true).ShouldBeFalse();
    }

    [Fact(DisplayName = "ShouldList returns false for a dev plugin in production")]
    public void ShouldList_DevPluginInProduction_ReturnsFalse()
    {
        var entry = Entry();
        entry.IsDevPlugin = true;

        RegistryPluginFilter.ShouldList(entry, null, true, true).ShouldBeFalse();
        RegistryPluginFilter.ShouldList(entry, null, false, true).ShouldBeTrue();
    }

    [Fact(DisplayName = "ShouldList hides a preview plugin unless previews are shown")]
    public void ShouldList_PreviewHiddenUnlessShown()
    {
        var entry = Entry();
        entry.IsPreview = true;

        RegistryPluginFilter.ShouldList(entry, null, false, false).ShouldBeFalse();
        RegistryPluginFilter.ShouldList(entry, null, false, true).ShouldBeTrue();
    }

    [Fact(DisplayName = "ShouldList returns true when the plugin is not installed")]
    public void ShouldList_NotInstalled_ReturnsTrue()
    {
        RegistryPluginFilter.ShouldList(Entry(), null, false, false).ShouldBeTrue();
    }

    [Fact(DisplayName = "ShouldList returns true when the installed version is older")]
    public void ShouldList_InstalledOlder_ReturnsTrue()
    {
        RegistryPluginFilter.ShouldList(Entry("2.0.0"), Installed("1.0.0"), false, false).ShouldBeTrue();
    }

    [Fact(DisplayName = "ShouldList returns false when the installed version is the same")]
    public void ShouldList_InstalledSameVersion_ReturnsFalse()
    {
        RegistryPluginFilter.ShouldList(Entry("1.0.0"), Installed("1.0.0"), false, false).ShouldBeFalse();
    }

    [Fact(DisplayName = "ShouldList returns false for an unparseable version while installed")]
    public void ShouldList_UnparseableVersionWhileInstalled_ReturnsFalse()
    {
        RegistryPluginFilter.ShouldList(Entry("not-a-version"), Installed("1.0.0"), false, false).ShouldBeFalse();
    }
}
