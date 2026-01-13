using LiteDB;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Plugins;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Mythetech.Framework.Desktop;

/// <summary>
/// LiteDB-based plugin state provider for Desktop applications.
/// Persists which plugins are disabled across application restarts.
/// </summary>
public class LiteDbPluginStateProvider : IPluginStateProvider, IDisposable
{
    private readonly Lazy<ILiteDatabase?> _database;
    private readonly ILogger<LiteDbPluginStateProvider>? _logger;
    private const string CollectionName = "plugin_state";
    private const string DocumentId = "disabled_plugins";

    /// <summary>
    /// Creates a new LiteDB plugin state provider.
    /// Uses lazy initialization to defer database creation until first use.
    /// </summary>
    /// <param name="databasePath">Path to the LiteDB file</param>
    /// <param name="logger">Optional logger for error reporting</param>
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

    /// <summary>
    /// Creates a new LiteDB plugin state provider with an existing database.
    /// </summary>
    /// <param name="database">An existing LiteDB database instance</param>
    /// <param name="logger">Optional logger for error reporting</param>
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

/// <summary>
/// Entry stored in LiteDB for plugin state persistence.
/// </summary>
internal class PluginStateEntry
{
    /// <summary>
    /// The document ID.
    /// </summary>
    [BsonId]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The JSON-serialized set of disabled plugin IDs.
    /// </summary>
    public string DisabledPluginsJson { get; set; } = string.Empty;

    /// <summary>
    /// When the state was last modified.
    /// </summary>
    public DateTime LastModified { get; set; }
}
