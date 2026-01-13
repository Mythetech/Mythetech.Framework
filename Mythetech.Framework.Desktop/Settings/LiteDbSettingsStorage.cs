using LiteDB;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Settings;

namespace Mythetech.Framework.Desktop.Settings;

/// <summary>
/// LiteDB-based settings storage for Desktop applications.
/// Uses a separate database file from plugin storage to keep app settings
/// isolated from plugin data.
/// </summary>
public class LiteDbSettingsStorage : ISettingsStorage, IDisposable
{
    private readonly Lazy<ILiteDatabase?> _database;
    private readonly ILogger<LiteDbSettingsStorage>? _logger;
    private const string CollectionName = "settings";

    /// <summary>
    /// Creates a new LiteDB settings storage instance.
    /// Uses lazy initialization to defer database creation until first use.
    /// </summary>
    /// <param name="databasePath">Path to the LiteDB file</param>
    /// <param name="logger">Optional logger for error reporting</param>
    public LiteDbSettingsStorage(string databasePath, ILogger<LiteDbSettingsStorage>? logger = null)
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
                _logger?.LogError(ex, "Failed to initialize settings storage at {DatabasePath}. Settings persistence will be unavailable.", databasePath);
                return null;
            }
        });
    }

    /// <summary>
    /// Creates a new LiteDB settings storage instance with an existing database.
    /// </summary>
    /// <param name="database">An existing LiteDB database instance</param>
    /// <param name="logger">Optional logger for error reporting</param>
    public LiteDbSettingsStorage(ILiteDatabase database, ILogger<LiteDbSettingsStorage>? logger = null)
    {
        _logger = logger;
        _database = new Lazy<ILiteDatabase?>(() => database);
    }

    private ILiteCollection<SettingsStorageEntry>? GetCollection()
    {
        var db = _database.Value;
        return db?.GetCollection<SettingsStorageEntry>(CollectionName);
    }

    /// <inheritdoc />
    public Task SaveSettingsAsync(string settingsId, string jsonData)
    {
        var collection = GetCollection();
        if (collection == null)
        {
            _logger?.LogDebug("Settings storage unavailable, skipping save for {SettingsId}", settingsId);
            return Task.CompletedTask;
        }

        try
        {
            var entry = new SettingsStorageEntry
            {
                SettingsId = settingsId,
                JsonData = jsonData,
                LastModified = DateTime.UtcNow
            };
            collection.Upsert(entry);
            _logger?.LogDebug("Saved settings for {SettingsId}", settingsId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save settings for {SettingsId}", settingsId);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<string?> LoadSettingsAsync(string settingsId)
    {
        var collection = GetCollection();
        if (collection == null)
        {
            return Task.FromResult<string?>(null);
        }

        try
        {
            var entry = collection.FindById(settingsId);
            return Task.FromResult(entry?.JsonData);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load settings for {SettingsId}", settingsId);
            return Task.FromResult<string?>(null);
        }
    }

    /// <inheritdoc />
    public Task<Dictionary<string, string>> LoadAllSettingsAsync()
    {
        var result = new Dictionary<string, string>();
        var collection = GetCollection();

        if (collection == null)
        {
            return Task.FromResult(result);
        }

        try
        {
            foreach (var entry in collection.FindAll())
            {
                result[entry.SettingsId] = entry.JsonData;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load all settings");
        }

        return Task.FromResult(result);
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

/// <summary>
/// Entry stored in LiteDB for settings persistence.
/// </summary>
internal class SettingsStorageEntry
{
    /// <summary>
    /// The settings domain ID (used as the document ID).
    /// </summary>
    [BsonId]
    public string SettingsId { get; set; } = string.Empty;

    /// <summary>
    /// The JSON-serialized settings data.
    /// </summary>
    public string JsonData { get; set; } = string.Empty;

    /// <summary>
    /// When the settings were last modified.
    /// </summary>
    public DateTime LastModified { get; set; }
}
