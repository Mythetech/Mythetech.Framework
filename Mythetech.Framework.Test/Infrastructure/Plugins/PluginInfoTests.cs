using System.Reflection;
using Mythetech.Framework.Infrastructure.Plugins;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Plugins;

public class PluginInfoTests
{
    #region ParseVersion Tests

    [Fact(DisplayName = "ParseVersion parses valid version string")]
    public void ParseVersion_ValidString_ReturnsVersion()
    {
        // Act
        var result = PluginInfo.ParseVersion("1.0.0");

        // Assert
        result.ShouldNotBeNull();
        result.Major.ShouldBe(1);
        result.Minor.ShouldBe(0);
        result.Build.ShouldBe(0);
    }

    [Fact(DisplayName = "ParseVersion parses version with build and revision")]
    public void ParseVersion_WithBuildAndRevision_ReturnsVersion()
    {
        // Act
        var result = PluginInfo.ParseVersion("2.5.3.10");

        // Assert
        result.ShouldNotBeNull();
        result.Major.ShouldBe(2);
        result.Minor.ShouldBe(5);
        result.Build.ShouldBe(3);
        result.Revision.ShouldBe(10);
    }

    [Fact(DisplayName = "ParseVersion returns null for null string")]
    public void ParseVersion_NullString_ReturnsNull()
    {
        // Act
        var result = PluginInfo.ParseVersion(null!);

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "ParseVersion returns null for empty string")]
    public void ParseVersion_EmptyString_ReturnsNull()
    {
        // Act
        var result = PluginInfo.ParseVersion(string.Empty);

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "ParseVersion returns null for whitespace string")]
    public void ParseVersion_WhitespaceString_ReturnsNull()
    {
        // Act
        var result = PluginInfo.ParseVersion("   ");

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "ParseVersion returns null for invalid version string")]
    public void ParseVersion_InvalidString_ReturnsNull()
    {
        // Act
        var result = PluginInfo.ParseVersion("not-a-version");

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region CompareVersion Tests

    [Fact(DisplayName = "CompareVersion returns positive when this version is newer")]
    public void CompareVersion_ThisNewer_ReturnsPositive()
    {
        // Arrange
        var plugin = CreatePluginInfo(new Version(2, 0, 0));
        var otherVersion = new Version(1, 0, 0);

        // Act
        var result = plugin.CompareVersion(otherVersion);

        // Assert
        result.ShouldBeGreaterThan(0);
    }

    [Fact(DisplayName = "CompareVersion returns zero when versions are the same")]
    public void CompareVersion_SameVersion_ReturnsZero()
    {
        // Arrange
        var version = new Version(1, 0, 0);
        var plugin = CreatePluginInfo(version);
        var otherVersion = new Version(1, 0, 0);

        // Act
        var result = plugin.CompareVersion(otherVersion);

        // Assert
        result.ShouldBe(0);
    }

    [Fact(DisplayName = "CompareVersion returns negative when this version is older")]
    public void CompareVersion_ThisOlder_ReturnsNegative()
    {
        // Arrange
        var plugin = CreatePluginInfo(new Version(1, 0, 0));
        var otherVersion = new Version(2, 0, 0);

        // Act
        var result = plugin.CompareVersion(otherVersion);

        // Assert
        result.ShouldBeLessThan(0);
    }

    [Fact(DisplayName = "CompareVersion compares minor version")]
    public void CompareVersion_ComparesMinorVersion()
    {
        // Arrange
        var plugin = CreatePluginInfo(new Version(1, 2, 0));
        var otherVersion = new Version(1, 1, 0);

        // Act
        var result = plugin.CompareVersion(otherVersion);

        // Assert
        result.ShouldBeGreaterThan(0);
    }

    [Fact(DisplayName = "CompareVersion compares build version")]
    public void CompareVersion_ComparesBuildVersion()
    {
        // Arrange
        var plugin = CreatePluginInfo(new Version(1, 0, 2));
        var otherVersion = new Version(1, 0, 1);

        // Act
        var result = plugin.CompareVersion(otherVersion);

        // Assert
        result.ShouldBeGreaterThan(0);
    }

    [Fact(DisplayName = "CompareVersion throws ArgumentNullException for null version")]
    public void CompareVersion_NullVersion_ThrowsArgumentNullException()
    {
        // Arrange
        var plugin = CreatePluginInfo(new Version(1, 0, 0));

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => plugin.CompareVersion(null!));
    }

    #endregion

    #region IsNewerThan Tests

    [Fact(DisplayName = "IsNewerThan returns true when this version is newer")]
    public void IsNewerThan_ThisNewer_ReturnsTrue()
    {
        // Arrange
        var plugin = CreatePluginInfo(new Version(2, 0, 0));
        var otherVersion = new Version(1, 0, 0);

        // Act
        var result = plugin.IsNewerThan(otherVersion);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact(DisplayName = "IsNewerThan returns false when versions are the same")]
    public void IsNewerThan_SameVersion_ReturnsFalse()
    {
        // Arrange
        var version = new Version(1, 0, 0);
        var plugin = CreatePluginInfo(version);
        var otherVersion = new Version(1, 0, 0);

        // Act
        var result = plugin.IsNewerThan(otherVersion);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact(DisplayName = "IsNewerThan returns false when this version is older")]
    public void IsNewerThan_ThisOlder_ReturnsFalse()
    {
        // Arrange
        var plugin = CreatePluginInfo(new Version(1, 0, 0));
        var otherVersion = new Version(2, 0, 0);

        // Act
        var result = plugin.IsNewerThan(otherVersion);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsSameVersion Tests

    [Fact(DisplayName = "IsSameVersion returns true when versions are the same")]
    public void IsSameVersion_SameVersion_ReturnsTrue()
    {
        // Arrange
        var version = new Version(1, 0, 0);
        var plugin = CreatePluginInfo(version);
        var otherVersion = new Version(1, 0, 0);

        // Act
        var result = plugin.IsSameVersion(otherVersion);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact(DisplayName = "IsSameVersion returns false when this version is newer")]
    public void IsSameVersion_ThisNewer_ReturnsFalse()
    {
        // Arrange
        var plugin = CreatePluginInfo(new Version(2, 0, 0));
        var otherVersion = new Version(1, 0, 0);

        // Act
        var result = plugin.IsSameVersion(otherVersion);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact(DisplayName = "IsSameVersion returns false when this version is older")]
    public void IsSameVersion_ThisOlder_ReturnsFalse()
    {
        // Arrange
        var plugin = CreatePluginInfo(new Version(1, 0, 0));
        var otherVersion = new Version(2, 0, 0);

        // Act
        var result = plugin.IsSameVersion(otherVersion);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsOlderThan Tests

    [Fact(DisplayName = "IsOlderThan returns true when this version is older")]
    public void IsOlderThan_ThisOlder_ReturnsTrue()
    {
        // Arrange
        var plugin = CreatePluginInfo(new Version(1, 0, 0));
        var otherVersion = new Version(2, 0, 0);

        // Act
        var result = plugin.IsOlderThan(otherVersion);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact(DisplayName = "IsOlderThan returns false when versions are the same")]
    public void IsOlderThan_SameVersion_ReturnsFalse()
    {
        // Arrange
        var version = new Version(1, 0, 0);
        var plugin = CreatePluginInfo(version);
        var otherVersion = new Version(1, 0, 0);

        // Act
        var result = plugin.IsOlderThan(otherVersion);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact(DisplayName = "IsOlderThan returns false when this version is newer")]
    public void IsOlderThan_ThisNewer_ReturnsFalse()
    {
        // Arrange
        var plugin = CreatePluginInfo(new Version(2, 0, 0));
        var otherVersion = new Version(1, 0, 0);

        // Act
        var result = plugin.IsOlderThan(otherVersion);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region Integration Tests

    [Fact(DisplayName = "Version comparison methods work together correctly for upgrade scenario")]
    public void VersionComparison_UpgradeScenario_WorksCorrectly()
    {
        // Arrange
        var installedPlugin = CreatePluginInfo(new Version(1, 0, 0));
        var registryVersion = new Version(2, 0, 0);

        // Act & Assert
        installedPlugin.IsOlderThan(registryVersion).ShouldBeTrue();
        installedPlugin.IsNewerThan(registryVersion).ShouldBeFalse();
        installedPlugin.IsSameVersion(registryVersion).ShouldBeFalse();
    }

    [Fact(DisplayName = "Version comparison methods work together correctly for downgrade scenario")]
    public void VersionComparison_DowngradeScenario_WorksCorrectly()
    {
        // Arrange
        var installedPlugin = CreatePluginInfo(new Version(2, 0, 0));
        var registryVersion = new Version(1, 0, 0);

        // Act & Assert
        installedPlugin.IsOlderThan(registryVersion).ShouldBeFalse();
        installedPlugin.IsNewerThan(registryVersion).ShouldBeTrue();
        installedPlugin.IsSameVersion(registryVersion).ShouldBeFalse();
    }

    [Fact(DisplayName = "Version comparison methods work together correctly for identical scenario")]
    public void VersionComparison_IdenticalScenario_WorksCorrectly()
    {
        // Arrange
        var version = new Version(1, 0, 0);
        var installedPlugin = CreatePluginInfo(version);
        var registryVersion = new Version(1, 0, 0);

        // Act & Assert
        installedPlugin.IsOlderThan(registryVersion).ShouldBeFalse();
        installedPlugin.IsNewerThan(registryVersion).ShouldBeFalse();
        installedPlugin.IsSameVersion(registryVersion).ShouldBeTrue();
    }

    [Fact(DisplayName = "ParseVersion and version comparison work together")]
    public void ParseVersion_AndComparison_WorkTogether()
    {
        // Arrange
        var installedPlugin = CreatePluginInfo(new Version(1, 0, 0));
        var registryVersionString = "2.0.0";
        var registryVersion = PluginInfo.ParseVersion(registryVersionString);

        // Act & Assert
        registryVersion.ShouldNotBeNull();
        installedPlugin.IsOlderThan(registryVersion).ShouldBeTrue();
    }

    #endregion

    #region Helper Methods

    private static PluginInfo CreatePluginInfo(Version version)
    {
        var manifest = Substitute.For<IPluginManifest>();
        manifest.Id.Returns("test.plugin");
        manifest.Name.Returns("Test Plugin");
        manifest.Version.Returns(version);
        manifest.Developer.Returns("Test Developer");
        manifest.Description.Returns("Test Description");

        return new PluginInfo
        {
            Manifest = manifest,
            Assembly = typeof(PluginInfoTests).Assembly,
            IsEnabled = true
        };
    }

    #endregion
}

