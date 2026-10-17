using LiteDB;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Settings;

namespace Mythetech.Framework.Desktop.Storage.LiteDb;

public class LiteDbSettingsStorage : ISettingsStorage, ISettingsStorageProbe, IDisposable
{
    private readonly Lazy<ILiteDatabase?> _database;
    private readonly ILogger<LiteDbSettingsStorage>? _logger;
    private Exception? _openError;
    private const string CollectionName = "settings";

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
                _openError = ex;
                _logger?.LogError(ex, "Failed to initialize settings storage at {DatabasePath}. Settings persistence will be unavailable.", databasePath);
                return null;
            }
        });
    }

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

    // Reads run on the thread pool since the first one opens the database; saves stay on the caller to keep their order.
    /// <inheritdoc />
    public Task<string?> LoadSettingsAsync(string settingsId) => Task.Run(() => LoadSettings(settingsId));

    /// <inheritdoc />
    public Task<Dictionary<string, string>> LoadAllSettingsAsync() => Task.Run(LoadAllSettings);

    Task ISettingsStorageProbe.ProbeAsync(CancellationToken cancellationToken) => Task.Run(Probe, cancellationToken);

    private string? LoadSettings(string settingsId)
    {
        var collection = GetCollection();
        if (collection == null)
        {
            return null;
        }

        try
        {
            return collection.FindById(settingsId)?.JsonData;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load settings for {SettingsId}", settingsId);
            return null;
        }
    }

    private Dictionary<string, string> LoadAllSettings()
    {
        var result = new Dictionary<string, string>();
        var collection = GetCollection();

        if (collection == null)
        {
            return result;
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

        return result;
    }

    private void Probe()
    {
        var collection = GetCollection()
            ?? throw new InvalidOperationException($"The settings store could not be opened: {_openError?.Message}", _openError);
        collection.Count();
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

internal class SettingsStorageEntry
{
    [BsonId]
    public string SettingsId { get; set; } = string.Empty;

    public string JsonData { get; set; } = string.Empty;

    public DateTime LastModified { get; set; }
}
