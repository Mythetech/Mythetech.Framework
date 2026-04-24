using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Settings;
using SqliteWasmBlazor;

namespace Mythetech.Framework.WebAssembly.Storage.Sqlite;

public class SqliteSettingsStorage : ISettingsStorage
{
    private readonly string _connectionString;
    private readonly ILogger<SqliteSettingsStorage>? _logger;
    private bool _initialized;

    public SqliteSettingsStorage(string databaseName, ILogger<SqliteSettingsStorage>? logger = null)
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
            createCmd.CommandText = "CREATE TABLE IF NOT EXISTS settings (settings_id TEXT PRIMARY KEY, json_data TEXT NOT NULL, last_modified TEXT NOT NULL)";
            await createCmd.ExecuteNonQueryAsync();

            _initialized = true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize settings storage. Settings persistence will be unavailable.");
        }
    }

    /// <inheritdoc />
    public async Task SaveSettingsAsync(string settingsId, string jsonData)
    {
        await EnsureInitializedAsync();
        if (!_initialized)
        {
            _logger?.LogDebug("Settings storage unavailable, skipping save for {SettingsId}", settingsId);
            return;
        }

        try
        {
            await using var connection = new SqliteWasmConnection(_connectionString);
            await connection.OpenAsync();
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT OR REPLACE INTO settings (settings_id, json_data, last_modified) VALUES ($id, $json, $modified)";
            cmd.Parameters.Add(new SqliteWasmParameter("$id", settingsId));
            cmd.Parameters.Add(new SqliteWasmParameter("$json", jsonData));
            cmd.Parameters.Add(new SqliteWasmParameter("$modified", DateTime.UtcNow.ToString("O")));
            await cmd.ExecuteNonQueryAsync();
            _logger?.LogDebug("Saved settings for {SettingsId}", settingsId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save settings for {SettingsId}", settingsId);
        }
    }

    /// <inheritdoc />
    public async Task<string?> LoadSettingsAsync(string settingsId)
    {
        await EnsureInitializedAsync();
        if (!_initialized) return null;

        try
        {
            await using var connection = new SqliteWasmConnection(_connectionString);
            await connection.OpenAsync();
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT json_data FROM settings WHERE settings_id = $id";
            cmd.Parameters.Add(new SqliteWasmParameter("$id", settingsId));
            var result = await cmd.ExecuteScalarAsync();
            return result as string;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load settings for {SettingsId}", settingsId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, string>> LoadAllSettingsAsync()
    {
        var result = new Dictionary<string, string>();

        await EnsureInitializedAsync();
        if (!_initialized) return result;

        try
        {
            await using var connection = new SqliteWasmConnection(_connectionString);
            await connection.OpenAsync();
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT settings_id, json_data FROM settings";
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result[reader.GetString(0)] = reader.GetString(1);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load all settings");
        }

        return result;
    }
}
