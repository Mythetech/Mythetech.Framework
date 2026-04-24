using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Plugins;
using SqliteWasmBlazor;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Mythetech.Framework.WebAssembly.Storage.Sqlite;

public class SqlitePluginStateProvider : IPluginStateProvider
{
    private readonly string _connectionString;
    private readonly ILogger<SqlitePluginStateProvider>? _logger;
    private bool _initialized;
    private const string DocumentId = "disabled_plugins";

    public SqlitePluginStateProvider(string databaseName, ILogger<SqlitePluginStateProvider>? logger = null)
    {
        _connectionString = $"Data Source={databaseName}";
        _logger = logger;
    }

    private async Task EnsureInitializedAsync()
    {
        if (_initialized) return;

        try
        {
            await using var connection = new SqliteWasmConnection(_connectionString);
            await connection.OpenAsync();

            await using var createCmd = connection.CreateCommand();
            createCmd.CommandText = "CREATE TABLE IF NOT EXISTS plugin_state (id TEXT PRIMARY KEY, disabled_plugins_json TEXT NOT NULL, last_modified TEXT NOT NULL)";
            await createCmd.ExecuteNonQueryAsync();

            _initialized = true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize plugin state storage. Plugin state persistence will be unavailable.");
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlySet<string>> LoadDisabledPluginsAsync()
    {
        await EnsureInitializedAsync();
        if (!_initialized) return new HashSet<string>();

        try
        {
            await using var connection = new SqliteWasmConnection(_connectionString);
            await connection.OpenAsync();
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT disabled_plugins_json FROM plugin_state WHERE id = $id";
            cmd.Parameters.Add(new SqliteWasmParameter("$id", DocumentId));

            var result = await cmd.ExecuteScalarAsync();
            var json = result as string;
            if (json == null)
            {
                return new HashSet<string>();
            }

            var plugins = JsonSerializer.Deserialize<HashSet<string>>(json);
            _logger?.LogDebug("Loaded {Count} disabled plugins from state provider", plugins?.Count ?? 0);
            return plugins ?? new HashSet<string>();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load disabled plugins");
            return new HashSet<string>();
        }
    }

    /// <inheritdoc />
    public async Task SaveDisabledPluginsAsync(IReadOnlySet<string> disabledPlugins)
    {
        await EnsureInitializedAsync();
        if (!_initialized)
        {
            _logger?.LogDebug("Plugin state storage unavailable, skipping save");
            return;
        }

        try
        {
            var json = JsonSerializer.Serialize(disabledPlugins);
            await using var connection = new SqliteWasmConnection(_connectionString);
            await connection.OpenAsync();
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT OR REPLACE INTO plugin_state (id, disabled_plugins_json, last_modified) VALUES ($id, $json, $modified)";
            cmd.Parameters.Add(new SqliteWasmParameter("$id", DocumentId));
            cmd.Parameters.Add(new SqliteWasmParameter("$json", json));
            cmd.Parameters.Add(new SqliteWasmParameter("$modified", DateTime.UtcNow.ToString("O")));
            await cmd.ExecuteNonQueryAsync();
            _logger?.LogDebug("Saved {Count} disabled plugins to state provider", disabledPlugins.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save disabled plugins");
        }
    }
}
