using LiteDB;
using Mythetech.Framework.Infrastructure.Plugins;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Mythetech.Framework.Desktop;

/// <summary>
/// LiteDB-based plugin storage for Desktop applications.
/// Each plugin gets its own collection within a shared database.
/// </summary>
public class LiteDbPluginStorage : IPluginStorage
{
    private readonly ILiteDatabase _database;
    private readonly string _collectionName;
    private ILiteCollection<PluginStorageEntry> Collection => _database.GetCollection<PluginStorageEntry>(_collectionName);

    /// <summary>
    /// Constructor
    /// </summary>
    public LiteDbPluginStorage(ILiteDatabase database, string pluginId)
    {
        _database = database;
        _collectionName = $"plugin_{pluginId.Replace(".", "_")}";
    }

    /// <inheritdoc />
    public Task<T?> GetAsync<T>(string key)
    {
        var entry = Collection.FindById(key);
        if (entry == null)
            return Task.FromResult<T?>(default);
        
        var result = JsonSerializer.Deserialize<T>(entry.JsonValue);
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task SetAsync<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value);
        var entry = new PluginStorageEntry { Key = key, JsonValue = json };
        Collection.Upsert(entry);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(string key)
    {
        var deleted = Collection.Delete(key);
        return Task.FromResult(deleted);
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string key)
    {
        var exists = Collection.FindById(key) != null;
        return Task.FromResult(exists);
    }

    /// <inheritdoc />
    public Task<IEnumerable<string>> GetKeysAsync(string? prefix = null)
    {
        IEnumerable<string> keys = Collection.FindAll().Select(e => e.Key);
        
        if (prefix != null)
        {
            keys = keys.Where(k => k.StartsWith(prefix));
        }
        
        return Task.FromResult(keys.ToList().AsEnumerable());
    }

    /// <inheritdoc />
    public Task ClearAsync()
    {
        Collection.DeleteAll();
        return Task.CompletedTask;
    }
}

/// <summary>
/// Entry stored in LiteDB
/// </summary>
internal class PluginStorageEntry
{
    /// <summary>
    /// The storage key (used as the document ID)
    /// </summary>
    [BsonId]
    public string Key { get; set; } = string.Empty;
    
    /// <summary>
    /// The JSON-serialized value
    /// </summary>
    public string JsonValue { get; set; } = string.Empty;
}

/// <summary>
/// Factory for creating LiteDB-based plugin storage instances
/// </summary>
public class LiteDbPluginStorageFactory : IPluginStorageFactory, IDisposable
{
    private readonly ILiteDatabase _database;

    /// <summary>
    /// Constructor - creates or opens the plugin database
    /// </summary>
    /// <param name="databasePath">Path to the LiteDB file</param>
    public LiteDbPluginStorageFactory(string databasePath)
    {
        _database = new LiteDatabase(databasePath);
    }

    /// <summary>
    /// Constructor with existing database instance
    /// </summary>
    public LiteDbPluginStorageFactory(ILiteDatabase database)
    {
        _database = database;
    }

    /// <inheritdoc />
    public IPluginStorage CreateForPlugin(string pluginId)
    {
        return new LiteDbPluginStorage(_database, pluginId);
    }

    /// <inheritdoc />
    public Task<string> ExportPluginDataAsync(string pluginId)
    {
        var storage = CreateForPlugin(pluginId) as LiteDbPluginStorage;
        var collectionName = $"plugin_{pluginId.Replace(".", "_")}";
        var collection = _database.GetCollection<PluginStorageEntry>(collectionName);
        
        var data = collection.FindAll()
            .ToDictionary(e => e.Key, e => e.JsonValue);
        
        return Task.FromResult(JsonSerializer.Serialize(data));
    }

    /// <inheritdoc />
    public Task ImportPluginDataAsync(string pluginId, string jsonData)
    {
        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonData);
        if (data == null) return Task.CompletedTask;
        
        var collectionName = $"plugin_{pluginId.Replace(".", "_")}";
        var collection = _database.GetCollection<PluginStorageEntry>(collectionName);
        
        foreach (var (key, value) in data)
        {
            collection.Upsert(new PluginStorageEntry { Key = key, JsonValue = value });
        }
        
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeletePluginDataAsync(string pluginId)
    {
        var collectionName = $"plugin_{pluginId.Replace(".", "_")}";
        _database.DropCollection(collectionName);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _database.Dispose();
        GC.SuppressFinalize(this);
    }
}

