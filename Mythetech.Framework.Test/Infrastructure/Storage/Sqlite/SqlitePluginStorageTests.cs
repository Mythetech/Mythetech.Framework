using Mythetech.Framework.Desktop.Storage.Sqlite;
using Mythetech.Framework.Infrastructure.Plugins;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Storage.Sqlite;

public class SqlitePluginStorageTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly SqlitePluginStorageFactory _factory;

    public SqlitePluginStorageTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"plugin_test_{Guid.NewGuid()}.sqlite");
        _factory = new SqlitePluginStorageFactory(_testDbPath);
    }

    #region Storage Isolation Tests

    [Fact(DisplayName = "Data set by Plugin A is not visible to Plugin B")]
    public async Task Storage_PluginA_NotVisibleTo_PluginB()
    {
        var storageA = _factory.CreateForPlugin("plugin.a")!;
        var storageB = _factory.CreateForPlugin("plugin.b")!;

        await storageA.SetAsync("secret", "A's secret data");

        var resultA = await storageA.GetAsync<string>("secret");
        resultA.ShouldBe("A's secret data");

        var resultB = await storageB.GetAsync<string>("secret");
        resultB.ShouldBeNull();
    }

    [Fact(DisplayName = "Plugins can use same key names without conflict")]
    public async Task Storage_SameKeyNames_NoConflict()
    {
        var storageA = _factory.CreateForPlugin("plugin.a")!;
        var storageB = _factory.CreateForPlugin("plugin.b")!;

        await storageA.SetAsync("config", new TestConfig { Name = "Config A" });
        await storageB.SetAsync("config", new TestConfig { Name = "Config B" });

        var configA = await storageA.GetAsync<TestConfig>("config");
        var configB = await storageB.GetAsync<TestConfig>("config");

        configA!.Name.ShouldBe("Config A");
        configB!.Name.ShouldBe("Config B");
    }

    [Fact(DisplayName = "Clearing one plugin's storage doesn't affect another")]
    public async Task Storage_ClearPlugin_DoesNotAffectOthers()
    {
        var storageA = _factory.CreateForPlugin("plugin.a")!;
        var storageB = _factory.CreateForPlugin("plugin.b")!;

        await storageA.SetAsync("data", "A's data");
        await storageB.SetAsync("data", "B's data");

        await storageA.ClearAsync();

        var resultA = await storageA.GetAsync<string>("data");
        var resultB = await storageB.GetAsync<string>("data");

        resultA.ShouldBeNull();
        resultB.ShouldBe("B's data");
    }

    [Fact(DisplayName = "GetKeysAsync only returns keys for this plugin")]
    public async Task Storage_GetKeys_OnlyReturnsOwnKeys()
    {
        var storageA = _factory.CreateForPlugin("plugin.a")!;
        var storageB = _factory.CreateForPlugin("plugin.b")!;

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
        keysB.ShouldNotContain("note:2");
    }

    [Fact(DisplayName = "GetKeysAsync with prefix filters correctly")]
    public async Task Storage_GetKeys_WithPrefix_FiltersCorrectly()
    {
        var storage = _factory.CreateForPlugin("plugin.test")!;

        await storage.SetAsync("note:1", "Note 1");
        await storage.SetAsync("note:2", "Note 2");
        await storage.SetAsync("config:theme", "dark");
        await storage.SetAsync("config:lang", "en");

        var noteKeys = (await storage.GetKeysAsync("note:")).ToList();
        var configKeys = (await storage.GetKeysAsync("config:")).ToList();

        noteKeys.Count.ShouldBe(2);
        noteKeys.ShouldContain("note:1");
        noteKeys.ShouldContain("note:2");

        configKeys.Count.ShouldBe(2);
        configKeys.ShouldContain("config:theme");
        configKeys.ShouldContain("config:lang");
    }

    #endregion

    #region CRUD Operations Tests

    [Fact(DisplayName = "SetAsync creates new entry")]
    public async Task Storage_Set_CreatesEntry()
    {
        var storage = _factory.CreateForPlugin("plugin.test")!;

        await storage.SetAsync("key", "value");

        var exists = await storage.ExistsAsync("key");
        exists.ShouldBeTrue();
    }

    [Fact(DisplayName = "SetAsync overwrites existing entry")]
    public async Task Storage_Set_OverwritesExisting()
    {
        var storage = _factory.CreateForPlugin("plugin.test")!;
        await storage.SetAsync("key", "original");

        await storage.SetAsync("key", "updated");

        var result = await storage.GetAsync<string>("key");
        result.ShouldBe("updated");
    }

    [Fact(DisplayName = "DeleteAsync removes entry and returns true")]
    public async Task Storage_Delete_RemovesEntry()
    {
        var storage = _factory.CreateForPlugin("plugin.test")!;
        await storage.SetAsync("key", "value");

        var deleted = await storage.DeleteAsync("key");

        deleted.ShouldBeTrue();
        var exists = await storage.ExistsAsync("key");
        exists.ShouldBeFalse();
    }

    [Fact(DisplayName = "DeleteAsync returns false for non-existent key")]
    public async Task Storage_Delete_NonExistent_ReturnsFalse()
    {
        var storage = _factory.CreateForPlugin("plugin.test")!;

        var deleted = await storage.DeleteAsync("nonexistent");

        deleted.ShouldBeFalse();
    }

    [Fact(DisplayName = "GetAsync returns default for non-existent key")]
    public async Task Storage_Get_NonExistent_ReturnsDefault()
    {
        var storage = _factory.CreateForPlugin("plugin.test")!;

        var stringResult = await storage.GetAsync<string>("nonexistent");
        var intResult = await storage.GetAsync<int>("nonexistent");

        stringResult.ShouldBeNull();
        intResult.ShouldBe(0);
    }

    [Fact(DisplayName = "Complex objects are serialized and deserialized correctly")]
    public async Task Storage_ComplexObjects_SerializeCorrectly()
    {
        var storage = _factory.CreateForPlugin("plugin.test")!;
        var note = new TestNote
        {
            Id = "note-123",
            Title = "Test Note",
            Content = "Some content",
            Tags = ["important", "work"],
            CreatedAt = new DateTime(2024, 1, 15, 10, 30, 0)
        };

        await storage.SetAsync("note:123", note);
        var result = await storage.GetAsync<TestNote>("note:123");

        result.ShouldNotBeNull();
        result.Id.ShouldBe("note-123");
        result.Title.ShouldBe("Test Note");
        result.Content.ShouldBe("Some content");
        result.Tags.ShouldContain("important");
        result.Tags.ShouldContain("work");
        result.CreatedAt.ShouldBe(new DateTime(2024, 1, 15, 10, 30, 0));
    }

    #endregion

    #region Export/Import Tests

    [Fact(DisplayName = "ExportPluginDataAsync exports all plugin data as JSON")]
    public async Task Factory_Export_ExportsAllData()
    {
        var storage = _factory.CreateForPlugin("plugin.export")!;
        await storage.SetAsync("key1", "value1");
        await storage.SetAsync("key2", 42);

        var json = await _factory.ExportPluginDataAsync("plugin.export");

        json.ShouldNotBeNullOrEmpty();
        json.ShouldContain("key1");
        json.ShouldContain("key2");
    }

    [Fact(DisplayName = "ImportPluginDataAsync imports data correctly")]
    public async Task Factory_Import_ImportsData()
    {
        var json = """{"testKey":"{\"Value\":\"imported\"}"}""";

        await _factory.ImportPluginDataAsync("plugin.import", json);

        var storage = _factory.CreateForPlugin("plugin.import")!;
        var exists = await storage.ExistsAsync("testKey");
        exists.ShouldBeTrue();
    }

    [Fact(DisplayName = "DeletePluginDataAsync removes all plugin data")]
    public async Task Factory_Delete_RemovesAllData()
    {
        var storage = _factory.CreateForPlugin("plugin.delete")!;
        await storage.SetAsync("key1", "value1");
        await storage.SetAsync("key2", "value2");

        await _factory.DeletePluginDataAsync("plugin.delete");

        var newStorage = _factory.CreateForPlugin("plugin.delete")!;
        var keys = await newStorage.GetKeysAsync();
        keys.ShouldBeEmpty();
    }

    [Fact(DisplayName = "DeletePluginDataAsync doesn't affect other plugins")]
    public async Task Factory_Delete_DoesNotAffectOthers()
    {
        var storageA = _factory.CreateForPlugin("plugin.a")!;
        var storageB = _factory.CreateForPlugin("plugin.b")!;
        await storageA.SetAsync("data", "A's data");
        await storageB.SetAsync("data", "B's data");

        await _factory.DeletePluginDataAsync("plugin.a");

        var newStorageA = _factory.CreateForPlugin("plugin.a")!;
        var keysA = await newStorageA.GetKeysAsync();
        var keysB = (await storageB.GetKeysAsync()).ToList();

        keysA.ShouldBeEmpty();
        keysB.ShouldContain("data");
    }

    #endregion

    public void Dispose()
    {
        _factory.Dispose();

        if (File.Exists(_testDbPath))
        {
            File.Delete(_testDbPath);
        }

        GC.SuppressFinalize(this);
    }
}

#region Test Models

public class TestConfig
{
    public string Name { get; set; } = string.Empty;
}

public class TestNote
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}

#endregion
