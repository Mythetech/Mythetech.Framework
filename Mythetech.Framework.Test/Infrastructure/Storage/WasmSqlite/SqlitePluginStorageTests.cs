using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Framework.WebAssembly.Storage.Sqlite;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Storage.WasmSqlite;

/// <summary>
/// Tests for WebAssembly SQLite plugin storage.
/// These tests require a browser host with SqliteWasmBlazor initialized (OPFS + Web Worker).
/// They validate the same contract as the Desktop SQLite tests but against the Wasm ADO.NET provider.
/// Run via SqliteWasmBlazor.TestHost or a Blazor WebAssembly test runner.
/// </summary>
[Trait("Category", "WasmIntegration")]
public class SqlitePluginStorageTests
{
    private const string TestConnectionString = "Data Source=plugin_test.db";

    [Fact(Skip = "Requires browser host with SqliteWasmBlazor initialized")]
    public async Task Storage_PluginA_NotVisibleTo_PluginB()
    {
        var storageA = new SqlitePluginStorage(TestConnectionString, "plugin.a");
        await storageA.EnsureTableAsync();
        var storageB = new SqlitePluginStorage(TestConnectionString, "plugin.b");
        await storageB.EnsureTableAsync();

        await storageA.SetAsync("secret", "A's secret data");

        var resultA = await storageA.GetAsync<string>("secret");
        resultA.ShouldBe("A's secret data");

        var resultB = await storageB.GetAsync<string>("secret");
        resultB.ShouldBeNull();
    }

    [Fact(Skip = "Requires browser host with SqliteWasmBlazor initialized")]
    public async Task Storage_SameKeyNames_NoConflict()
    {
        var storageA = new SqlitePluginStorage(TestConnectionString, "plugin.a");
        await storageA.EnsureTableAsync();
        var storageB = new SqlitePluginStorage(TestConnectionString, "plugin.b");
        await storageB.EnsureTableAsync();

        await storageA.SetAsync("config", new TestConfig { Name = "Config A" });
        await storageB.SetAsync("config", new TestConfig { Name = "Config B" });

        var configA = await storageA.GetAsync<TestConfig>("config");
        var configB = await storageB.GetAsync<TestConfig>("config");

        configA!.Name.ShouldBe("Config A");
        configB!.Name.ShouldBe("Config B");
    }

    [Fact(Skip = "Requires browser host with SqliteWasmBlazor initialized")]
    public async Task Storage_ClearPlugin_DoesNotAffectOthers()
    {
        var storageA = new SqlitePluginStorage(TestConnectionString, "plugin.a");
        await storageA.EnsureTableAsync();
        var storageB = new SqlitePluginStorage(TestConnectionString, "plugin.b");
        await storageB.EnsureTableAsync();

        await storageA.SetAsync("data", "A's data");
        await storageB.SetAsync("data", "B's data");

        await storageA.ClearAsync();

        var resultA = await storageA.GetAsync<string>("data");
        var resultB = await storageB.GetAsync<string>("data");

        resultA.ShouldBeNull();
        resultB.ShouldBe("B's data");
    }

    [Fact(Skip = "Requires browser host with SqliteWasmBlazor initialized")]
    public async Task Storage_GetKeys_OnlyReturnsOwnKeys()
    {
        var storageA = new SqlitePluginStorage(TestConnectionString, "plugin.a");
        await storageA.EnsureTableAsync();
        var storageB = new SqlitePluginStorage(TestConnectionString, "plugin.b");
        await storageB.EnsureTableAsync();

        await storageA.SetAsync("note:1", "Note 1");
        await storageA.SetAsync("note:2", "Note 2");
        await storageB.SetAsync("note:3", "Note 3");

        var keysA = (await storageA.GetKeysAsync()).ToList();
        var keysB = (await storageB.GetKeysAsync()).ToList();

        keysA.ShouldContain("note:1");
        keysA.ShouldContain("note:2");
        keysA.ShouldNotContain("note:3");

        keysB.ShouldContain("note:3");
        keysB.ShouldNotContain("note:1");
    }

    [Fact(Skip = "Requires browser host with SqliteWasmBlazor initialized")]
    public async Task Storage_GetKeys_WithPrefix_FiltersCorrectly()
    {
        var storage = new SqlitePluginStorage(TestConnectionString, "plugin.test");
        await storage.EnsureTableAsync();

        await storage.SetAsync("note:1", "Note 1");
        await storage.SetAsync("note:2", "Note 2");
        await storage.SetAsync("config:theme", "dark");

        var noteKeys = (await storage.GetKeysAsync("note:")).ToList();
        noteKeys.Count.ShouldBe(2);
        noteKeys.ShouldContain("note:1");
        noteKeys.ShouldContain("note:2");
    }

    [Fact(Skip = "Requires browser host with SqliteWasmBlazor initialized")]
    public async Task Storage_Set_CreatesEntry()
    {
        var storage = new SqlitePluginStorage(TestConnectionString, "plugin.test");
        await storage.EnsureTableAsync();

        await storage.SetAsync("key", "value");

        var exists = await storage.ExistsAsync("key");
        exists.ShouldBeTrue();
    }

    [Fact(Skip = "Requires browser host with SqliteWasmBlazor initialized")]
    public async Task Storage_Set_OverwritesExisting()
    {
        var storage = new SqlitePluginStorage(TestConnectionString, "plugin.test");
        await storage.EnsureTableAsync();
        await storage.SetAsync("key", "original");

        await storage.SetAsync("key", "updated");

        var result = await storage.GetAsync<string>("key");
        result.ShouldBe("updated");
    }

    [Fact(Skip = "Requires browser host with SqliteWasmBlazor initialized")]
    public async Task Storage_Delete_RemovesEntry()
    {
        var storage = new SqlitePluginStorage(TestConnectionString, "plugin.test");
        await storage.EnsureTableAsync();
        await storage.SetAsync("key", "value");

        var deleted = await storage.DeleteAsync("key");

        deleted.ShouldBeTrue();
        var exists = await storage.ExistsAsync("key");
        exists.ShouldBeFalse();
    }

    [Fact(Skip = "Requires browser host with SqliteWasmBlazor initialized")]
    public async Task Storage_Delete_NonExistent_ReturnsFalse()
    {
        var storage = new SqlitePluginStorage(TestConnectionString, "plugin.test");
        await storage.EnsureTableAsync();

        var deleted = await storage.DeleteAsync("nonexistent");
        deleted.ShouldBeFalse();
    }

    [Fact(Skip = "Requires browser host with SqliteWasmBlazor initialized")]
    public async Task Storage_Get_NonExistent_ReturnsDefault()
    {
        var storage = new SqlitePluginStorage(TestConnectionString, "plugin.test");
        await storage.EnsureTableAsync();

        var stringResult = await storage.GetAsync<string>("nonexistent");
        var intResult = await storage.GetAsync<int>("nonexistent");

        stringResult.ShouldBeNull();
        intResult.ShouldBe(0);
    }

    [Fact(Skip = "Requires browser host with SqliteWasmBlazor initialized")]
    public async Task Factory_Export_ExportsAllData()
    {
        var factory = new SqlitePluginStorageFactory("plugin_test.db");
        var storage = await factory.CreateForPluginAsync("plugin.export");
        await storage!.SetAsync("key1", "value1");
        await storage.SetAsync("key2", 42);

        var json = await factory.ExportPluginDataAsync("plugin.export");

        json.ShouldNotBeNullOrEmpty();
        json.ShouldContain("key1");
        json.ShouldContain("key2");
    }

    [Fact(Skip = "Requires browser host with SqliteWasmBlazor initialized")]
    public async Task Factory_Import_ImportsData()
    {
        var factory = new SqlitePluginStorageFactory("plugin_test.db");
        var json = """{"testKey":"{\"Value\":\"imported\"}"}""";

        await factory.ImportPluginDataAsync("plugin.import", json);

        var storage = await factory.CreateForPluginAsync("plugin.import");
        var exists = await storage!.ExistsAsync("testKey");
        exists.ShouldBeTrue();
    }

    [Fact(Skip = "Requires browser host with SqliteWasmBlazor initialized")]
    public async Task Factory_Delete_RemovesAllData()
    {
        var factory = new SqlitePluginStorageFactory("plugin_test.db");
        var storage = await factory.CreateForPluginAsync("plugin.delete");
        await storage!.SetAsync("key1", "value1");

        await factory.DeletePluginDataAsync("plugin.delete");

        var newStorage = await factory.CreateForPluginAsync("plugin.delete");
        var keys = await newStorage!.GetKeysAsync();
        keys.ShouldBeEmpty();
    }
}

public class TestConfig
{
    public string Name { get; set; } = string.Empty;
}
