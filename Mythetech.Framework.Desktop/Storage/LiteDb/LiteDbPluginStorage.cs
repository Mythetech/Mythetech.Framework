using LiteDB;
using Mythetech.Framework.Infrastructure.Plugins;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Mythetech.Framework.Desktop.Storage.LiteDb;

public class LiteDbPluginStorage : IPluginStorage
{
    private readonly ILiteDatabase _database;
    private readonly string _collectionName;
    private ILiteCollection<PluginStorageEntry> Collection => _database.GetCollection<PluginStorageEntry>(_collectionName);

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

internal class PluginStorageEntry
{
    [BsonId]
    public string Key { get; set; } = string.Empty;

    public string JsonValue { get; set; } = string.Empty;
}
