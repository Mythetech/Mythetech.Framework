using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Plugins;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Mythetech.Framework.Desktop.Storage.Sqlite;

public class SqlitePluginStorageFactory : IPluginStorageFactory, IDisposable
{
    private readonly Lazy<string?> _connectionString;
    private readonly ILogger<SqlitePluginStorageFactory>? _logger;

    public SqlitePluginStorageFactory(string databasePath, ILogger<SqlitePluginStorageFactory>? logger = null)
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
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "PRAGMA journal_mode=WAL";
                cmd.ExecuteNonQuery();

                return connStr;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize plugin storage at {DatabasePath}. Plugin storage will be unavailable.", databasePath);
                return null;
            }
        });
    }

    /// <inheritdoc />
    public IPluginStorage? CreateForPlugin(string pluginId)
    {
        var connStr = _connectionString.Value;
        if (connStr == null) return null;
        return new SqlitePluginStorage(connStr, pluginId);
    }

    /// <inheritdoc />
    public Task<string> ExportPluginDataAsync(string pluginId)
    {
        var connStr = _connectionString.Value;
        if (connStr == null) return Task.FromResult("{}");

        var tableName = $"plugin_{pluginId.Replace(".", "_")}";
        var data = new Dictionary<string, string>();

        using var connection = new SqliteConnection(connStr);
        connection.Open();

        if (!TableExists(connection, tableName))
            return Task.FromResult(JsonSerializer.Serialize(data));

        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT key, json_value FROM [{tableName}]";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            data[reader.GetString(0)] = reader.GetString(1);
        }

        return Task.FromResult(JsonSerializer.Serialize(data));
    }

    /// <inheritdoc />
    public Task ImportPluginDataAsync(string pluginId, string jsonData)
    {
        var connStr = _connectionString.Value;
        if (connStr == null) return Task.CompletedTask;

        var imported = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonData);
        if (imported == null) return Task.CompletedTask;

        var tableName = $"plugin_{pluginId.Replace(".", "_")}";
        using var connection = new SqliteConnection(connStr);
        connection.Open();

        using var createCmd = connection.CreateCommand();
        createCmd.CommandText = $"CREATE TABLE IF NOT EXISTS [{tableName}] (key TEXT PRIMARY KEY, json_value TEXT NOT NULL)";
        createCmd.ExecuteNonQuery();

        foreach (var (key, value) in imported)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"INSERT OR REPLACE INTO [{tableName}] (key, json_value) VALUES (@key, @json)";
            cmd.Parameters.AddWithValue("@key", key);
            cmd.Parameters.AddWithValue("@json", value);
            cmd.ExecuteNonQuery();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeletePluginDataAsync(string pluginId)
    {
        var connStr = _connectionString.Value;
        if (connStr == null) return Task.CompletedTask;

        var tableName = $"plugin_{pluginId.Replace(".", "_")}";
        using var connection = new SqliteConnection(connStr);
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"DROP TABLE IF EXISTS [{tableName}]";
        cmd.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    private static bool TableExists(SqliteConnection connection, string tableName)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(1) FROM sqlite_master WHERE type='table' AND name=@name";
        cmd.Parameters.AddWithValue("@name", tableName);
        return (long)cmd.ExecuteScalar()! > 0;
    }
}
