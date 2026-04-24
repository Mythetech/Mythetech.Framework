using LiteDB;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Plugins;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Mythetech.Framework.Desktop.Storage.LiteDb;

public class LiteDbPluginStorageFactory : IPluginStorageFactory, IDisposable
{
    private readonly Lazy<ILiteDatabase?> _database;
    private readonly ILogger<LiteDbPluginStorageFactory>? _logger;

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
