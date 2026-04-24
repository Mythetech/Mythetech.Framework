using Mythetech.Framework.Desktop.Storage.Sqlite;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Storage.Sqlite;

public class SqliteSettingsStorageTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly SqliteSettingsStorage _storage;

    public SqliteSettingsStorageTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"settings_test_{Guid.NewGuid()}.sqlite");
        _storage = new SqliteSettingsStorage(_testDbPath);
    }

    [Fact(DisplayName = "SaveSettingsAsync creates new settings entry")]
    public async Task Save_CreatesEntry()
    {
        await _storage.SaveSettingsAsync("test.settings", """{"theme":"dark"}""");

        var result = await _storage.LoadSettingsAsync("test.settings");
        result.ShouldBe("""{"theme":"dark"}""");
    }

    [Fact(DisplayName = "SaveSettingsAsync overwrites existing settings")]
    public async Task Save_OverwritesExisting()
    {
        await _storage.SaveSettingsAsync("test.settings", """{"theme":"dark"}""");
        await _storage.SaveSettingsAsync("test.settings", """{"theme":"light"}""");

        var result = await _storage.LoadSettingsAsync("test.settings");
        result.ShouldBe("""{"theme":"light"}""");
    }

    [Fact(DisplayName = "LoadSettingsAsync returns null for non-existent settings")]
    public async Task Load_NonExistent_ReturnsNull()
    {
        var result = await _storage.LoadSettingsAsync("nonexistent");
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "LoadAllSettingsAsync returns all settings")]
    public async Task LoadAll_ReturnsAllSettings()
    {
        await _storage.SaveSettingsAsync("app.theme", """{"dark":true}""");
        await _storage.SaveSettingsAsync("app.locale", """{"lang":"en"}""");
        await _storage.SaveSettingsAsync("plugin.config", """{"enabled":true}""");

        var all = await _storage.LoadAllSettingsAsync();

        all.Count.ShouldBe(3);
        all["app.theme"].ShouldBe("""{"dark":true}""");
        all["app.locale"].ShouldBe("""{"lang":"en"}""");
        all["plugin.config"].ShouldBe("""{"enabled":true}""");
    }

    [Fact(DisplayName = "LoadAllSettingsAsync returns empty dictionary when no settings exist")]
    public async Task LoadAll_Empty_ReturnsEmptyDictionary()
    {
        var all = await _storage.LoadAllSettingsAsync();
        all.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Multiple settings domains are isolated")]
    public async Task MultipleSettings_AreIsolated()
    {
        await _storage.SaveSettingsAsync("domain.a", """{"value":"A"}""");
        await _storage.SaveSettingsAsync("domain.b", """{"value":"B"}""");

        var resultA = await _storage.LoadSettingsAsync("domain.a");
        var resultB = await _storage.LoadSettingsAsync("domain.b");

        resultA.ShouldBe("""{"value":"A"}""");
        resultB.ShouldBe("""{"value":"B"}""");
    }

    public void Dispose()
    {
        _storage.Dispose();

        if (File.Exists(_testDbPath))
        {
            File.Delete(_testDbPath);
        }

        GC.SuppressFinalize(this);
    }
}
