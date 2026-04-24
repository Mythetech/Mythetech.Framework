using LiteDB;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Plugins;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Mythetech.Framework.Desktop.Storage.LiteDb;

public class LiteDbPluginStateProvider : IPluginStateProvider, IDisposable
{
    private readonly Lazy<ILiteDatabase?> _database;
    private readonly ILogger<LiteDbPluginStateProvider>? _logger;
    private const string CollectionName = "plugin_state";
    private const string DocumentId = "disabled_plugins";

    public LiteDbPluginStateProvider(string databasePath, ILogger<LiteDbPluginStateProvider>? logger = null)
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
                _logger?.LogError(ex, "Failed to initialize plugin state storage at {DatabasePath}. Plugin state persistence will be unavailable.", databasePath);
                return null;
            }
        });
    }

    public LiteDbPluginStateProvider(ILiteDatabase database, ILogger<LiteDbPluginStateProvider>? logger = null)
    {
        _logger = logger;
        _database = new Lazy<ILiteDatabase?>(() => database);
    }

    private ILiteCollection<PluginStateEntry>? GetCollection()
    {
        var db = _database.Value;
        return db?.GetCollection<PluginStateEntry>(CollectionName);
    }

    /// <inheritdoc />
    public Task<IReadOnlySet<string>> LoadDisabledPluginsAsync()
    {
        var collection = GetCollection();
        if (collection == null)
        {
            return Task.FromResult<IReadOnlySet<string>>(new HashSet<string>());
        }

        try
        {
            var entry = collection.FindById(DocumentId);
            if (entry?.DisabledPluginsJson == null)
            {
                return Task.FromResult<IReadOnlySet<string>>(new HashSet<string>());
            }

            var plugins = JsonSerializer.Deserialize<HashSet<string>>(entry.DisabledPluginsJson);
            _logger?.LogDebug("Loaded {Count} disabled plugins from state provider", plugins?.Count ?? 0);
            return Task.FromResult<IReadOnlySet<string>>(plugins ?? new HashSet<string>());
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load disabled plugins");
            return Task.FromResult<IReadOnlySet<string>>(new HashSet<string>());
        }
    }

    /// <inheritdoc />
    public Task SaveDisabledPluginsAsync(IReadOnlySet<string> disabledPlugins)
    {
        var collection = GetCollection();
        if (collection == null)
        {
            _logger?.LogDebug("Plugin state storage unavailable, skipping save");
            return Task.CompletedTask;
        }

        try
        {
            var json = JsonSerializer.Serialize(disabledPlugins);
            var entry = new PluginStateEntry
            {
                Id = DocumentId,
                DisabledPluginsJson = json,
                LastModified = DateTime.UtcNow
            };
            collection.Upsert(entry);
            _logger?.LogDebug("Saved {Count} disabled plugins to state provider", disabledPlugins.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save disabled plugins");
        }

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

internal class PluginStateEntry
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    public string DisabledPluginsJson { get; set; } = string.Empty;

    public DateTime LastModified { get; set; }
}
