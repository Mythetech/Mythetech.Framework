using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Plugins;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Mythetech.Framework.Desktop.Storage.Sqlite;

public class SqlitePluginStateProvider : IPluginStateProvider, IDisposable
{
    private readonly Lazy<string?> _connectionString;
    private readonly ILogger<SqlitePluginStateProvider>? _logger;
    private const string DocumentId = "disabled_plugins";

    public SqlitePluginStateProvider(string databasePath, ILogger<SqlitePluginStateProvider>? logger = null)
    {
        _logger = logger;
        _connectionString = new Lazy<string?>(() =>
        {
            try
            {
                var connStr = new SqliteConnectionStringBuilder
                {
                    DataSource = databasePath,
                    Mode = SqliteOpenMode.ReadWriteCreate
                }.ToString();

                using var connection = new SqliteConnection(connStr);
                connection.Open();

                using var walCmd = connection.CreateCommand();
                walCmd.CommandText = "PRAGMA journal_mode=WAL";
                walCmd.ExecuteNonQuery();

                using var createCmd = connection.CreateCommand();
                createCmd.CommandText = "CREATE TABLE IF NOT EXISTS plugin_state (id TEXT PRIMARY KEY, disabled_plugins_json TEXT NOT NULL, last_modified TEXT NOT NULL)";
                createCmd.ExecuteNonQuery();

                return connStr;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize plugin state storage at {DatabasePath}. Plugin state persistence will be unavailable.", databasePath);
                return null;
            }
        });
    }

    /// <inheritdoc />
    public Task<IReadOnlySet<string>> LoadDisabledPluginsAsync()
    {
        var connStr = _connectionString.Value;
        if (connStr == null)
        {
            return Task.FromResult<IReadOnlySet<string>>(new HashSet<string>());
        }

        try
        {
            using var connection = new SqliteConnection(connStr);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT disabled_plugins_json FROM plugin_state WHERE id = @id";
            cmd.Parameters.AddWithValue("@id", DocumentId);

            var json = cmd.ExecuteScalar() as string;
            if (json == null)
            {
                return Task.FromResult<IReadOnlySet<string>>(new HashSet<string>());
            }

            var plugins = JsonSerializer.Deserialize<HashSet<string>>(json);
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
        var connStr = _connectionString.Value;
        if (connStr == null)
        {
            _logger?.LogDebug("Plugin state storage unavailable, skipping save");
            return Task.CompletedTask;
        }

        try
        {
            var json = JsonSerializer.Serialize(disabledPlugins);
            using var connection = new SqliteConnection(connStr);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT OR REPLACE INTO plugin_state (id, disabled_plugins_json, last_modified) VALUES (@id, @json, @modified)";
            cmd.Parameters.AddWithValue("@id", DocumentId);
            cmd.Parameters.AddWithValue("@json", json);
            cmd.Parameters.AddWithValue("@modified", DateTime.UtcNow.ToString("O"));
            cmd.ExecuteNonQuery();
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
        GC.SuppressFinalize(this);
    }
}
