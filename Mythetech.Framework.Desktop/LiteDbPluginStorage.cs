using LiteDB;
using Microsoft.Extensions.Logging;
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
/// Factory for creating LiteDB-based plugin storage instances.
/// Uses lazy initialization to defer database creation until first use,
/// allowing graceful handling of permission errors.
/// </summary>
public class LiteDbPluginStorageFactory : IPluginStorageFactory, IDisposable
{
    private readonly Lazy<ILiteDatabase?> _database;
    private readonly ILogger<LiteDbPluginStorageFactory>? _logger;

    /// <summary>
    /// Constructor - creates or opens the plugin database lazily
    /// </summary>
    /// <param name="databasePath">Path to the LiteDB file</param>
    /// <param name="logger">Optional logger for error reporting</param>
    public LiteDbPluginStorageFactory(string databasePath, ILogger<LiteDbPluginStorageFactory>? logger = null)
    {
        _logger = logger;
        _database = new Lazy<ILiteDatabase?>(() =>
        {
            try
            {
                return new LiteDatabase(databasePath);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize plugin storage at {DatabasePath}. Plugin storage will be unavailable.", databasePath);
                return null;
            }
        });
    }

    /// <summary>
    /// Constructor with existing database instance
    /// </summary>
    /// <param name="database">An existing LiteDB database instance</param>
    /// <param name="logger">Optional logger for error reporting</param>
    public LiteDbPluginStorageFactory(ILiteDatabase database, ILogger<LiteDbPluginStorageFactory>? logger = null)
    {
        _logger = logger;
        _database = new Lazy<ILiteDatabase?>(() => database);
    }

    /// <inheritdoc />
    public IPluginStorage? CreateForPlugin(string pluginId)
    {
        var db = _database.Value;
        if (db == null) return null;
        return new LiteDbPluginStorage(db, pluginId);
    }

    /// <inheritdoc />
    public Task<string> ExportPluginDataAsync(string pluginId)
    {
        var db = _database.Value;
        if (db == null) return Task.FromResult("{}");

        var collectionName = $"plugin_{pluginId.Replace(".", "_")}";
        var collection = db.GetCollection<PluginStorageEntry>(collectionName);

        var data = collection.FindAll()
            .ToDictionary(e => e.Key, e => e.JsonValue);

        return Task.FromResult(JsonSerializer.Serialize(data));
    }

    /// <inheritdoc />
    public Task ImportPluginDataAsync(string pluginId, string jsonData)
    {
        var db = _database.Value;
        if (db == null) return Task.CompletedTask;

        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonData);
        if (data == null) return Task.CompletedTask;

        var collectionName = $"plugin_{pluginId.Replace(".", "_")}";
        var collection = db.GetCollection<PluginStorageEntry>(collectionName);

        foreach (var (key, value) in data)
        {
            collection.Upsert(new PluginStorageEntry { Key = key, JsonValue = value });
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeletePluginDataAsync(string pluginId)
    {
        var db = _database.Value;
        if (db == null) return Task.CompletedTask;

        var collectionName = $"plugin_{pluginId.Replace(".", "_")}";
        db.DropCollection(collectionName);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_database.IsValueCreated && _database.Value != null)
        {
            _database.Value.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}

