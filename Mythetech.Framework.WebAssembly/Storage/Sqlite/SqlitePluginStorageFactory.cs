using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Plugins;
using SqliteWasmBlazor;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Mythetech.Framework.WebAssembly.Storage.Sqlite;

public class SqlitePluginStorageFactory : IPluginStorageFactory
{
    private readonly string _connectionString;
    private readonly ILogger<SqlitePluginStorageFactory>? _logger;
    private bool _initialized;

    public SqlitePluginStorageFactory(string databaseName, ILogger<SqlitePluginStorageFactory>? logger = null)
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
            _initialized = true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize plugin storage database. Plugin storage will be unavailable.");
        }
    }

    /// <inheritdoc />
    public IPluginStorage? CreateForPlugin(string pluginId)
    {
        if (!_initialized)
        {
            _logger?.LogDebug("Plugin storage not yet initialized, returning null");
            return null;
        }

        return new SqlitePluginStorage(_connectionString, pluginId);
    }

    public async Task<IPluginStorage?> CreateForPluginAsync(string pluginId)
    {
        await EnsureInitializedAsync();
        if (!_initialized) return null;

        var storage = new SqlitePluginStorage(_connectionString, pluginId);
        await storage.EnsureTableAsync();
        return storage;
    }

    /// <inheritdoc />
    public async Task<string> ExportPluginDataAsync(string pluginId)
    {
        await EnsureInitializedAsync();
        if (!_initialized) return "{}";

        var tableName = $"plugin_{pluginId.Replace(".", "_")}";
        var data = new Dictionary<string, string>();

        try
        {
            await using var connection = new SqliteWasmConnection(_connectionString);
            await connection.OpenAsync();

            if (!await TableExistsAsync(connection, tableName))
                return JsonSerializer.Serialize(data);

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT key, json_value FROM [{tableName}]";
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                data[reader.GetString(0)] = reader.GetString(1);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to export plugin data for {PluginId}", pluginId);
        }

        return JsonSerializer.Serialize(data);
    }

    /// <inheritdoc />
    public async Task ImportPluginDataAsync(string pluginId, string jsonData)
    {
        await EnsureInitializedAsync();
        if (!_initialized) return;

        var imported = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonData);
        if (imported == null) return;

        var tableName = $"plugin_{pluginId.Replace(".", "_")}";

        try
        {
            await using var connection = new SqliteWasmConnection(_connectionString);
            await connection.OpenAsync();

            await using var createCmd = connection.CreateCommand();
            createCmd.CommandText = $"CREATE TABLE IF NOT EXISTS [{tableName}] (key TEXT PRIMARY KEY, json_value TEXT NOT NULL)";
            await createCmd.ExecuteNonQueryAsync();

            foreach (var (key, value) in imported)
            {
                await using var cmd = connection.CreateCommand();
                cmd.CommandText = $"INSERT OR REPLACE INTO [{tableName}] (key, json_value) VALUES ($key, $json)";
                cmd.Parameters.Add(new SqliteWasmParameter("$key", key));
                cmd.Parameters.Add(new SqliteWasmParameter("$json", value));
                await cmd.ExecuteNonQueryAsync();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to import plugin data for {PluginId}", pluginId);
        }
    }

    /// <inheritdoc />
    public async Task DeletePluginDataAsync(string pluginId)
    {
        await EnsureInitializedAsync();
        if (!_initialized) return;

        var tableName = $"plugin_{pluginId.Replace(".", "_")}";

        try
        {
            await using var connection = new SqliteWasmConnection(_connectionString);
            await connection.OpenAsync();

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = $"DROP TABLE IF EXISTS [{tableName}]";
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to delete plugin data for {PluginId}", pluginId);
        }
    }

    private static async Task<bool> TableExistsAsync(SqliteWasmConnection connection, string tableName)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(1) FROM sqlite_master WHERE type='table' AND name=$name";
        cmd.Parameters.Add(new SqliteWasmParameter("$name", tableName));
        var result = await cmd.ExecuteScalarAsync();
        return result is long count && count > 0;
    }
}
