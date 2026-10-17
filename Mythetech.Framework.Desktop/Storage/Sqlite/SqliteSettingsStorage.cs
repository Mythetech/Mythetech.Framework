using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Settings;

namespace Mythetech.Framework.Desktop.Storage.Sqlite;

public class SqliteSettingsStorage : ISettingsStorage, ISettingsStorageProbe, IDisposable
{
    private readonly Lazy<string?> _connectionString;
    private readonly ILogger<SqliteSettingsStorage>? _logger;
    private Exception? _openError;

    public SqliteSettingsStorage(string databasePath, ILogger<SqliteSettingsStorage>? logger = null)
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
                createCmd.CommandText = "CREATE TABLE IF NOT EXISTS settings (settings_id TEXT PRIMARY KEY, json_data TEXT NOT NULL, last_modified TEXT NOT NULL)";
                createCmd.ExecuteNonQuery();

                return connStr;
            }
            catch (Exception ex)
            {
                _openError = ex;
                _logger?.LogError(ex, "Failed to initialize settings storage at {DatabasePath}. Settings persistence will be unavailable.", databasePath);
                return null;
            }
        });
    }

    /// <inheritdoc />
    public Task SaveSettingsAsync(string settingsId, string jsonData)
    {
        var connStr = _connectionString.Value;
        if (connStr == null)
        {
            _logger?.LogDebug("Settings storage unavailable, skipping save for {SettingsId}", settingsId);
            return Task.CompletedTask;
        }

        try
        {
            using var connection = new SqliteConnection(connStr);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT OR REPLACE INTO settings (settings_id, json_data, last_modified) VALUES (@id, @json, @modified)";
            cmd.Parameters.AddWithValue("@id", settingsId);
            cmd.Parameters.AddWithValue("@json", jsonData);
            cmd.Parameters.AddWithValue("@modified", DateTime.UtcNow.ToString("O"));
            cmd.ExecuteNonQuery();
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
        var connStr = _connectionString.Value;
        if (connStr == null)
        {
            return null;
        }

        try
        {
            using var connection = new SqliteConnection(connStr);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT json_data FROM settings WHERE settings_id = @id";
            cmd.Parameters.AddWithValue("@id", settingsId);
            var result = cmd.ExecuteScalar() as string;
            return result;
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
        var connStr = _connectionString.Value;

        if (connStr == null)
        {
            return result;
        }

        try
        {
            using var connection = new SqliteConnection(connStr);
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT settings_id, json_data FROM settings";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
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

    private void Probe()
    {
        var connectionString = _connectionString.Value
            ?? throw new InvalidOperationException($"The settings store could not be opened: {_openError?.Message}", _openError);
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM settings";
        command.ExecuteScalar();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
