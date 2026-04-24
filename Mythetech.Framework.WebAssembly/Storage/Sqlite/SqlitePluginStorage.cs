using Mythetech.Framework.Infrastructure.Plugins;
using SqliteWasmBlazor;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Mythetech.Framework.WebAssembly.Storage.Sqlite;

public class SqlitePluginStorage : IPluginStorage
{
    private readonly string _connectionString;
    private readonly string _tableName;

    public SqlitePluginStorage(string connectionString, string pluginId)
    {
        _connectionString = connectionString;
        _tableName = $"plugin_{pluginId.Replace(".", "_")}";
    }

    public async Task EnsureTableAsync()
    {
        await using var connection = new SqliteWasmConnection(_connectionString);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"CREATE TABLE IF NOT EXISTS [{_tableName}] (key TEXT PRIMARY KEY, json_value TEXT NOT NULL)";
        await cmd.ExecuteNonQueryAsync();
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key)
    {
        await using var connection = new SqliteWasmConnection(_connectionString);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT json_value FROM [{_tableName}] WHERE key = $key";
        cmd.Parameters.Add(new SqliteWasmParameter("$key", key));

        var result = await cmd.ExecuteScalarAsync() as string;
        if (result == null)
            return default;

        return JsonSerializer.Deserialize<T>(result);
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value);
        await using var connection = new SqliteWasmConnection(_connectionString);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"INSERT OR REPLACE INTO [{_tableName}] (key, json_value) VALUES ($key, $json)";
        cmd.Parameters.Add(new SqliteWasmParameter("$key", key));
        cmd.Parameters.Add(new SqliteWasmParameter("$json", json));
        await cmd.ExecuteNonQueryAsync();
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string key)
    {
        await using var connection = new SqliteWasmConnection(_connectionString);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"DELETE FROM [{_tableName}] WHERE key = $key";
        cmd.Parameters.Add(new SqliteWasmParameter("$key", key));
        var rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string key)
    {
        await using var connection = new SqliteWasmConnection(_connectionString);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(1) FROM [{_tableName}] WHERE key = $key";
        cmd.Parameters.Add(new SqliteWasmParameter("$key", key));
        var result = await cmd.ExecuteScalarAsync();
        return result is long count && count > 0;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetKeysAsync(string? prefix = null)
    {
        await using var connection = new SqliteWasmConnection(_connectionString);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();

        if (prefix != null)
        {
            cmd.CommandText = $"SELECT key FROM [{_tableName}] WHERE key LIKE $prefix";
            cmd.Parameters.Add(new SqliteWasmParameter("$prefix", prefix + "%"));
        }
        else
        {
            cmd.CommandText = $"SELECT key FROM [{_tableName}]";
        }

        var keys = new List<string>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            keys.Add(reader.GetString(0));
        }

        return keys;
    }

    /// <inheritdoc />
    public async Task ClearAsync()
    {
        await using var connection = new SqliteWasmConnection(_connectionString);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"DELETE FROM [{_tableName}]";
        await cmd.ExecuteNonQueryAsync();
    }
}
