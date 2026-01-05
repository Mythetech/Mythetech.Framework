using Mythetech.Framework.Infrastructure.Plugins;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Plugins;

public class RegistryPluginEntryTests
{
    #region SupportsPlatform Tests

    [Fact(DisplayName = "SupportsPlatform returns true when SupportedPlatforms is null")]
    public void SupportsPlatform_NullPlatforms_ReturnsTrue()
    {
        // Arrange
        var entry = new RegistryPluginEntry
        {
            Id = "test.plugin",
            Name = "Test Plugin",
            Version = "1.0.0",
            Uri = "https://example.com/plugin.zip",
            SupportedPlatforms = null
        };

        // Act & Assert
        entry.SupportsPlatform(Platform.Desktop).ShouldBeTrue();
        entry.SupportsPlatform(Platform.WebAssembly).ShouldBeTrue();
    }

    [Fact(DisplayName = "SupportsPlatform returns true when SupportedPlatforms is empty")]
    public void SupportsPlatform_EmptyPlatforms_ReturnsTrue()
    {
        // Arrange
        var entry = new RegistryPluginEntry
        {
            Id = "test.plugin",
            Name = "Test Plugin",
            Version = "1.0.0",
            Uri = "https://example.com/plugin.zip",
            SupportedPlatforms = []
        };

        // Act & Assert
        entry.SupportsPlatform(Platform.Desktop).ShouldBeTrue();
        entry.SupportsPlatform(Platform.WebAssembly).ShouldBeTrue();
    }

    [Fact(DisplayName = "SupportsPlatform returns true for Desktop when only Desktop is supported")]
    public void SupportsPlatform_DesktopOnly_ReturnsTrueForDesktop()
    {
        // Arrange
        var entry = new RegistryPluginEntry
        {
            Id = "test.plugin",
            Name = "Test Plugin",
            Version = "1.0.0",
            Uri = "https://example.com/plugin.zip",
            SupportedPlatforms = ["desktop"]
        };

        // Act & Assert
        entry.SupportsPlatform(Platform.Desktop).ShouldBeTrue();
        entry.SupportsPlatform(Platform.WebAssembly).ShouldBeFalse();
    }

    [Fact(DisplayName = "SupportsPlatform returns true for WebAssembly when only WebAssembly is supported")]
    public void SupportsPlatform_WebAssemblyOnly_ReturnsTrueForWebAssembly()
    {
        // Arrange
        var entry = new RegistryPluginEntry
        {
            Id = "test.plugin",
            Name = "Test Plugin",
            Version = "1.0.0",
            Uri = "https://example.com/plugin.zip",
            SupportedPlatforms = ["webassembly"]
        };

        // Act & Assert
        entry.SupportsPlatform(Platform.Desktop).ShouldBeFalse();
        entry.SupportsPlatform(Platform.WebAssembly).ShouldBeTrue();
    }

    [Fact(DisplayName = "SupportsPlatform returns true for both when both platforms are supported")]
    public void SupportsPlatform_BothPlatforms_ReturnsTrueForBoth()
    {
        // Arrange
        var entry = new RegistryPluginEntry
        {
            Id = "test.plugin",
            Name = "Test Plugin",
            Version = "1.0.0",
            Uri = "https://example.com/plugin.zip",
            SupportedPlatforms = ["desktop", "webassembly"]
        };

        // Act & Assert
        entry.SupportsPlatform(Platform.Desktop).ShouldBeTrue();
        entry.SupportsPlatform(Platform.WebAssembly).ShouldBeTrue();
    }

    [Fact(DisplayName = "SupportsPlatform handles case-insensitive platform names")]
    public void SupportsPlatform_CaseInsensitive_Works()
    {
        // Arrange
        var entry = new RegistryPluginEntry
        {
            Id = "test.plugin",
            Name = "Test Plugin",
            Version = "1.0.0",
            Uri = "https://example.com/plugin.zip",
            SupportedPlatforms = ["DESKTOP", "WebAssembly"]
        };

        // Act & Assert
        entry.SupportsPlatform(Platform.Desktop).ShouldBeTrue();
        entry.SupportsPlatform(Platform.WebAssembly).ShouldBeTrue();
    }

    #endregion

    #region ParsedPlatforms Tests

    [Fact(DisplayName = "ParsedPlatforms returns null when SupportedPlatforms is null")]
    public void ParsedPlatforms_NullPlatforms_ReturnsNull()
    {
        // Arrange
        var entry = new RegistryPluginEntry
        {
            Id = "test.plugin",
            Name = "Test Plugin",
            Version = "1.0.0",
            Uri = "https://example.com/plugin.zip",
            SupportedPlatforms = null
        };

        // Act & Assert
        entry.ParsedPlatforms.ShouldBeNull();
    }

    [Fact(DisplayName = "ParsedPlatforms returns null when SupportedPlatforms is empty")]
    public void ParsedPlatforms_EmptyPlatforms_ReturnsNull()
    {
        // Arrange
        var entry = new RegistryPluginEntry
        {
            Id = "test.plugin",
            Name = "Test Plugin",
            Version = "1.0.0",
            Uri = "https://example.com/plugin.zip",
            SupportedPlatforms = []
        };

        // Act & Assert
        entry.ParsedPlatforms.ShouldBeNull();
    }

    [Fact(DisplayName = "ParsedPlatforms parses valid platform strings")]
    public void ParsedPlatforms_ValidStrings_ParsesCorrectly()
    {
        // Arrange
        var entry = new RegistryPluginEntry
        {
            Id = "test.plugin",
            Name = "Test Plugin",
            Version = "1.0.0",
            Uri = "https://example.com/plugin.zip",
            SupportedPlatforms = ["desktop", "webassembly"]
        };

        // Act
        var result = entry.ParsedPlatforms;

        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBe(2);
        result.ShouldContain(Platform.Desktop);
        result.ShouldContain(Platform.WebAssembly);
    }

    [Fact(DisplayName = "ParsedPlatforms ignores invalid platform strings")]
    public void ParsedPlatforms_InvalidStrings_Ignored()
    {
        // Arrange
        var entry = new RegistryPluginEntry
        {
            Id = "test.plugin",
            Name = "Test Plugin",
            Version = "1.0.0",
            Uri = "https://example.com/plugin.zip",
            SupportedPlatforms = ["desktop", "invalid", "webassembly", "also-invalid"]
        };

        // Act
        var result = entry.ParsedPlatforms;

        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBe(2);
        result.ShouldContain(Platform.Desktop);
        result.ShouldContain(Platform.WebAssembly);
    }

    [Fact(DisplayName = "ParsedPlatforms returns null when all platform strings are invalid")]
    public void ParsedPlatforms_AllInvalid_ReturnsNull()
    {
        // Arrange
        var entry = new RegistryPluginEntry
        {
            Id = "test.plugin",
            Name = "Test Plugin",
            Version = "1.0.0",
            Uri = "https://example.com/plugin.zip",
            SupportedPlatforms = ["invalid", "also-invalid", "not-a-platform"]
        };

        // Act & Assert
        entry.ParsedPlatforms.ShouldBeNull();
    }

    #endregion

    #region Backward Compatibility Tests

    [Fact(DisplayName = "Plugin without SupportedPlatforms works on all platforms (backward compatibility)")]
    public void BackwardCompatibility_NoSupportedPlatforms_WorksOnAllPlatforms()
    {
        // Arrange - simulates an old registry entry without the new field
        var entry = new RegistryPluginEntry
        {
            Id = "legacy.plugin",
            Name = "Legacy Plugin",
            Version = "1.0.0",
            Uri = "https://example.com/legacy.zip"
            // SupportedPlatforms not set (defaults to null)
        };

        // Act & Assert
        entry.SupportsPlatform(Platform.Desktop).ShouldBeTrue();
        entry.SupportsPlatform(Platform.WebAssembly).ShouldBeTrue();
        entry.ParsedPlatforms.ShouldBeNull();
    }

    #endregion
}
