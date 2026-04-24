using Microsoft.Data.Sqlite;
using Mythetech.Framework.Infrastructure.Plugins;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Mythetech.Framework.Desktop.Storage.Sqlite;

public class SqlitePluginStorage : IPluginStorage
{
    private readonly string _connectionString;
    private readonly string _tableName;

    public SqlitePluginStorage(string connectionString, string pluginId)
    {
        _connectionString = connectionString;
        _tableName = $"plugin_{pluginId.Replace(".", "_")}";
        EnsureTable();
    }

    private void EnsureTable()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"CREATE TABLE IF NOT EXISTS [{_tableName}] (key TEXT PRIMARY KEY, json_value TEXT NOT NULL)";
        cmd.ExecuteNonQuery();
    }

    /// <inheritdoc />
    public Task<T?> GetAsync<T>(string key)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT json_value FROM [{_tableName}] WHERE key = @key";
        cmd.Parameters.AddWithValue("@key", key);

        var result = cmd.ExecuteScalar() as string;
        if (result == null)
            return Task.FromResult<T?>(default);

        return Task.FromResult(JsonSerializer.Deserialize<T>(result));
    }

    /// <inheritdoc />
    public Task SetAsync<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value);
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"INSERT OR REPLACE INTO [{_tableName}] (key, json_value) VALUES (@key, @json)";
        cmd.Parameters.AddWithValue("@key", key);
        cmd.Parameters.AddWithValue("@json", json);
        cmd.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(string key)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"DELETE FROM [{_tableName}] WHERE key = @key";
        cmd.Parameters.AddWithValue("@key", key);
        var rows = cmd.ExecuteNonQuery();
        return Task.FromResult(rows > 0);
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string key)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(1) FROM [{_tableName}] WHERE key = @key";
        cmd.Parameters.AddWithValue("@key", key);
        var count = (long)cmd.ExecuteScalar()!;
        return Task.FromResult(count > 0);
    }

    /// <inheritdoc />
    public Task<IEnumerable<string>> GetKeysAsync(string? prefix = null)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var cmd = connection.CreateCommand();

        if (prefix != null)
        {
            cmd.CommandText = $"SELECT key FROM [{_tableName}] WHERE key LIKE @prefix";
            cmd.Parameters.AddWithValue("@prefix", prefix + "%");
        }
        else
        {
            cmd.CommandText = $"SELECT key FROM [{_tableName}]";
        }

        var keys = new List<string>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            keys.Add(reader.GetString(0));
        }

        return Task.FromResult<IEnumerable<string>>(keys);
    }

    /// <inheritdoc />
    public Task ClearAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"DELETE FROM [{_tableName}]";
        cmd.ExecuteNonQuery();
        return Task.CompletedTask;
    }
}
