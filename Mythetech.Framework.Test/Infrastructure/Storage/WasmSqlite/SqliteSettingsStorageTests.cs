using Mythetech.Framework.WebAssembly.Storage.Sqlite;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Storage.WasmSqlite;

[Trait("Category", "WasmIntegration")]
public class SqliteSettingsStorageTests
{
    [Fact(Skip = "Requires browser host with SqliteWasmBlazor initialized")]
    public async Task SaveAndLoad_RoundTrips()
    {
        var storage = new SqliteSettingsStorage("settings_test.db");

        await storage.SaveSettingsAsync("theme", """{"dark": true}""");
        var result = await storage.LoadSettingsAsync("theme");

        result.ShouldBe("""{"dark": true}""");
    }

    [Fact(Skip = "Requires browser host with SqliteWasmBlazor initialized")]
    public async Task SaveAndLoad_OverwritesExisting()
    {
        var storage = new SqliteSettingsStorage("settings_test.db");

        await storage.SaveSettingsAsync("theme", """{"dark": false}""");
        await storage.SaveSettingsAsync("theme", """{"dark": true}""");

        var result = await storage.LoadSettingsAsync("theme");
        result.ShouldBe("""{"dark": true}""");
    }

    [Fact(Skip = "Requires browser host with SqliteWasmBlazor initialized")]
    public async Task Load_NonExistent_ReturnsNull()
    {
        var storage = new SqliteSettingsStorage("settings_test.db");

        var result = await storage.LoadSettingsAsync("nonexistent");
        result.ShouldBeNull();
    }

    [Fact(Skip = "Requires browser host with SqliteWasmBlazor initialized")]
    public async Task LoadAll_ReturnsAllSettings()
    {
        var storage = new SqliteSettingsStorage("settings_test.db");

        await storage.SaveSettingsAsync("setting1", """{"value": 1}""");
        await storage.SaveSettingsAsync("setting2", """{"value": 2}""");

        var all = await storage.LoadAllSettingsAsync();

        all.ShouldContainKey("setting1");
        all.ShouldContainKey("setting2");
    }

    [Fact(Skip = "Requires browser host with SqliteWasmBlazor initialized")]
    public async Task Settings_IsolatedAcrossDomains()
    {
        var storage = new SqliteSettingsStorage("settings_test.db");

        await storage.SaveSettingsAsync("domain1.setting", """{"v": 1}""");
        await storage.SaveSettingsAsync("domain2.setting", """{"v": 2}""");

        var result1 = await storage.LoadSettingsAsync("domain1.setting");
        var result2 = await storage.LoadSettingsAsync("domain2.setting");

        result1.ShouldBe("""{"v": 1}""");
        result2.ShouldBe("""{"v": 2}""");
    }
}
