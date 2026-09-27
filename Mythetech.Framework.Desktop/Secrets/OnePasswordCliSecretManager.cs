using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Secrets;

namespace Mythetech.Framework.Desktop.Secrets;

/// <summary>
/// 1Password CLI implementation of ISecretManager. Reads, lists and writes items through the "op" CLI.
/// </summary>
public class OnePasswordCliSecretManager : ISecretManager, ISecretSearcher, ISecretWriter
{
    private const string PasswordFieldId = "password";
    private const string NotFoundSignal = "isn't an item";
    private const string AccessDeniedMessage =
        "1Password CLI is not signed in or 1Password is locked. Unlock 1Password or run 'op signin', then try again.";
    private const string CliMissingMessage =
        "1Password CLI (op) was not found on PATH or in its usual install locations.";

    // op's wording when it is signed out, its session expired, or the 1Password app is locked or the unlock
    // prompt was dismissed or timed out. All of them mean the user has to unlock 1Password before reading or
    // writing works.
    private static readonly string[] AccessDeniedSignals =
    [
        "not signed in",
        "not currently signed in",
        "session expired",
        "is locked",
        "authorization prompt dismissed",
        "authorization timeout"
    ];

    private readonly ILogger<OnePasswordCliSecretManager> _logger;
    private readonly IOnePasswordCliRunner _runner;

    /// <summary>
    /// Creates a manager that runs the 1Password CLI ("op") from PATH.
    /// </summary>
    public OnePasswordCliSecretManager(ILogger<OnePasswordCliSecretManager> logger)
        : this(logger, new OnePasswordCliRunner())
    {
    }

    internal OnePasswordCliSecretManager(ILogger<OnePasswordCliSecretManager> logger, IOnePasswordCliRunner runner)
    {
        _logger = logger;
        _runner = runner;
    }

    /// <inheritdoc />
    public string Name => "1Password CLI";

    /// <inheritdoc />
    public async Task<SecretOperationResult<IEnumerable<Secret>>> ListSecretsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await ExecuteOpCommandAsync(["item", "list", "--format", "json"], cancellationToken);
            if (string.IsNullOrWhiteSpace(result))
            {
                return SecretOperationResult<IEnumerable<Secret>>.Ok([]);
            }

            return SecretOperationResult<IEnumerable<Secret>>.Ok(ParseItemList(result));
        }
        catch (InvalidOperationException ex) when (IsAccessDenied(ex.Message))
        {
            return SecretOperationResult<IEnumerable<Secret>>.Fail(
                AccessDeniedMessage,
                SecretOperationErrorKind.AccessDenied);
        }
        catch (Exception ex) when (IsCliMissing(ex))
        {
            return SecretOperationResult<IEnumerable<Secret>>.Fail(
                CliMissingMessage,
                SecretOperationErrorKind.ConnectionFailed);
        }
        catch (Exception ex)
        {
            return SecretOperationResult<IEnumerable<Secret>>.Fail(
                $"Failed to list secrets: {ex.Message}",
                SecretOperationErrorKind.Unknown);
        }
    }

    /// <inheritdoc />
    public async Task<SecretOperationResult<Secret>> GetSecretAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return SecretOperationResult<Secret>.Fail(
                "Key cannot be null or empty.",
                SecretOperationErrorKind.InvalidKey);
        }

        try
        {
            var result = await ExecuteOpCommandAsync(["item", "get", key, "--format", "json"], cancellationToken);
            if (string.IsNullOrWhiteSpace(result))
            {
                return SecretOperationResult<Secret>.Fail(
                    $"Secret '{key}' not found.",
                    SecretOperationErrorKind.NotFound);
            }

            var secret = ParseItem(result);
            if (secret == null)
            {
                return SecretOperationResult<Secret>.Fail(
                    $"Secret '{key}' not found.",
                    SecretOperationErrorKind.NotFound);
            }

            return SecretOperationResult<Secret>.Ok(secret);
        }
        catch (InvalidOperationException ex) when (IsNotFound(ex.Message))
        {
            return SecretOperationResult<Secret>.Fail(
                $"Secret '{key}' not found.",
                SecretOperationErrorKind.NotFound);
        }
        catch (InvalidOperationException ex) when (IsAccessDenied(ex.Message))
        {
            return SecretOperationResult<Secret>.Fail(
                AccessDeniedMessage,
                SecretOperationErrorKind.AccessDenied);
        }
        catch (Exception ex) when (IsCliMissing(ex))
        {
            return SecretOperationResult<Secret>.Fail(
                CliMissingMessage,
                SecretOperationErrorKind.ConnectionFailed);
        }
        catch (Exception ex)
        {
            return SecretOperationResult<Secret>.Fail(
                $"Failed to get secret: {ex.Message}",
                SecretOperationErrorKind.Unknown);
        }
    }

    /// <inheritdoc />
    public async Task<SecretOperationResult> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await ExecuteOpCommandAsync(["account", "list"], cancellationToken);
            if (!string.IsNullOrWhiteSpace(result))
            {
                return SecretOperationResult.Ok();
            }
        }
        catch
        {
            // Fall through to try whoami
        }

        try
        {
            var result = await ExecuteOpCommandAsync(["whoami"], cancellationToken);
            if (!string.IsNullOrWhiteSpace(result))
            {
                return SecretOperationResult.Ok();
            }

            return SecretOperationResult.Fail(
                AccessDeniedMessage,
                SecretOperationErrorKind.AccessDenied);
        }
        catch (InvalidOperationException ex) when (IsAccessDenied(ex.Message))
        {
            return SecretOperationResult.Fail(
                AccessDeniedMessage,
                SecretOperationErrorKind.AccessDenied);
        }
        catch (Exception ex) when (IsCliMissing(ex))
        {
            return SecretOperationResult.Fail(
                CliMissingMessage,
                SecretOperationErrorKind.ConnectionFailed);
        }
        catch (Exception ex)
        {
            return SecretOperationResult.Fail(
                $"Failed to connect to 1Password: {ex.Message}",
                SecretOperationErrorKind.Unknown);
        }
    }

    /// <inheritdoc />
    public async Task<SecretOperationResult<IEnumerable<Secret>>> SearchSecretsAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return SecretOperationResult<IEnumerable<Secret>>.Fail(
                "Search term cannot be null or empty.",
                SecretOperationErrorKind.InvalidKey);
        }

        var listResult = await ListSecretsAsync(cancellationToken);
        if (!listResult.Success)
        {
            return listResult;
        }

        var term = searchTerm.ToLowerInvariant();
        var filtered = listResult.Value!.Where(s =>
            s.Key.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            (s.Name?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (s.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (s.Tags?.Any(t => t.Contains(term, StringComparison.OrdinalIgnoreCase)) ?? false)
        );

        return SecretOperationResult<IEnumerable<Secret>>.Ok(filtered);
    }
    
    /// <summary>
    /// Stores <paramref name="value"/> in the password field of the item titled <paramref name="key"/>, creating a
    /// Password item in the default vault when no item has that title. The value is piped to op as item JSON on
    /// stdin and never appears in op's arguments, which other processes on the machine can read.
    /// </summary>
    public Task<SecretOperationResult> SetSecretAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Task.FromResult(SecretOperationResult.Fail(
                "Key cannot be null or empty.",
                SecretOperationErrorKind.InvalidKey));
        }

        ArgumentNullException.ThrowIfNull(value);

        return RunWriteAsync("store", () => StorePasswordAsync(key, value, cancellationToken), cancellationToken);
    }

    /// <summary>
    /// Permanently deletes the item titled (or with the ID) <paramref name="key"/>.
    /// </summary>
    public Task<SecretOperationResult> DeleteSecretAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return Task.FromResult(SecretOperationResult.Fail(
                "Key cannot be null or empty.",
                SecretOperationErrorKind.InvalidKey));
        }

        return RunWriteAsync("delete", async () =>
        {
            var result = await _runner.RunAsync(["item", "delete", key], null, cancellationToken);
            return result.ExitCode == 0 ? SecretOperationResult.Ok() : MapWriteFailure(key, "delete", result);
        }, cancellationToken);
    }

    private async Task<SecretOperationResult> StorePasswordAsync(string key, string value, CancellationToken cancellationToken)
    {
        var existing = await _runner.RunAsync(["item", "get", key, "--format", "json"], null, cancellationToken);

        if (existing.ExitCode == 0)
        {
            return await UpdatePasswordAsync(key, existing.StandardOutput, value, cancellationToken);
        }

        if (!IsNotFound(existing.StandardError))
        {
            return MapWriteFailure(key, "store", existing);
        }

        var created = await _runner.RunAsync(["item", "create", "-"], BuildPasswordItemTemplate(key, value), cancellationToken);
        return created.ExitCode == 0 ? SecretOperationResult.Ok() : MapWriteFailure(key, "store", created);
    }

    /// <summary>
    /// Updates through op's documented template flow: the item's own JSON from 'op item get', with only the
    /// password value changed, piped to 'op item edit'. Other fields, notes and history stay as they were, and
    /// editing by ID keeps the item's identity, which recreating it would not.
    /// </summary>
    private async Task<SecretOperationResult> UpdatePasswordAsync(
        string key,
        string itemJson,
        string value,
        CancellationToken cancellationToken)
    {
        if (JsonNode.Parse(itemJson) is not JsonObject item
            || item["id"] is not JsonValue idNode
            || !idNode.TryGetValue<string>(out var id)
            || string.IsNullOrWhiteSpace(id))
        {
            return SecretOperationResult.Fail(
                $"Failed to store secret: 1Password CLI returned an unexpected item for '{key}'.",
                SecretOperationErrorKind.Unknown);
        }

        if (FindPasswordField(item) is not { } passwordField)
        {
            return SecretOperationResult.Fail(
                $"The 1Password item '{key}' has no password field, so it can't hold this secret.",
                SecretOperationErrorKind.InvalidKey);
        }

        passwordField["value"] = value;

        var edited = await _runner.RunAsync(["item", "edit", id], item.ToJsonString(), cancellationToken);
        return edited.ExitCode == 0 ? SecretOperationResult.Ok() : MapWriteFailure(key, "store", edited);
    }

    // Matches the field ExtractPasswordValue reads, so a stored value reads back through GetSecretAsync.
    private static JsonObject? FindPasswordField(JsonObject item) =>
        (item["fields"] as JsonArray)?
            .OfType<JsonObject>()
            .FirstOrDefault(field => field["id"] is JsonValue idNode
                                     && idNode.TryGetValue<string>(out var id)
                                     && id == PasswordFieldId);

    private static string BuildPasswordItemTemplate(string key, string value)
    {
        var template = new JsonObject
        {
            ["title"] = key,
            ["category"] = "PASSWORD",
            ["fields"] = new JsonArray
            {
                new JsonObject
                {
                    ["id"] = PasswordFieldId,
                    ["type"] = "CONCEALED",
                    ["purpose"] = "PASSWORD",
                    ["label"] = PasswordFieldId,
                    ["value"] = value
                }
            }
        };

        return template.ToJsonString();
    }

    private static async Task<SecretOperationResult> RunWriteAsync(
        string action,
        Func<Task<SecretOperationResult>> write,
        CancellationToken cancellationToken)
    {
        try
        {
            return await write();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (IsCliMissing(ex))
        {
            return SecretOperationResult.Fail(
                CliMissingMessage,
                SecretOperationErrorKind.ConnectionFailed);
        }
        catch (Exception ex)
        {
            return SecretOperationResult.Fail(
                $"Failed to {action} secret: {ex.Message}",
                SecretOperationErrorKind.Unknown);
        }
    }

    private static SecretOperationResult MapWriteFailure(string key, string action, OnePasswordCliResult result)
    {
        var error = result.StandardError.Trim();

        if (IsAccessDenied(error))
        {
            return SecretOperationResult.Fail(
                AccessDeniedMessage,
                SecretOperationErrorKind.AccessDenied);
        }

        if (IsNotFound(error))
        {
            return SecretOperationResult.Fail(
                $"Secret '{key}' not found.",
                SecretOperationErrorKind.NotFound);
        }

        return SecretOperationResult.Fail(
            $"Failed to {action} secret: 1Password CLI command failed with exit code {result.ExitCode}: {error}",
            SecretOperationErrorKind.Unknown);
    }

    private static bool IsAccessDenied(string error) =>
        AccessDeniedSignals.Any(signal => error.Contains(signal, StringComparison.OrdinalIgnoreCase));

    private static bool IsNotFound(string error) =>
        error.Contains(NotFoundSignal, StringComparison.OrdinalIgnoreCase);

    // Process.Start throws Win32Exception when op cannot be found or started. The message checks cover a
    // shell's own "command not found" wording.
    private static bool IsCliMissing(Exception ex) =>
        ex is Win32Exception
        || ex.Message.Contains("command not found")
        || ex.Message.Contains("not recognized");

    private async Task<string> ExecuteOpCommandAsync(string[] arguments, CancellationToken cancellationToken)
    {
        var result = await _runner.RunAsync(arguments, null, cancellationToken);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"1Password CLI command failed with exit code {result.ExitCode}: {result.StandardError}");
        }

        return result.StandardOutput.Trim();
    }

    private IEnumerable<Secret> ParseItemList(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var items = new List<Secret>();
            
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    var secret = ParseItemElement(element);
                    if (secret != null)
                    {
                        items.Add(secret);
                    }
                }
            }
            
            return items;
        }
        catch
        {
            return [];
        }
    }
    
    private Secret? ParseItem(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return ParseItemElement(doc.RootElement);
        }
        catch
        {
            return null;
        }
    }
    
    private Secret? ParseItemElement(JsonElement element)
    {
        try
        {
            var id = element.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
            var title = element.TryGetProperty("title", out var titleProp) ? titleProp.GetString() : null;

            // 1Password item list returns different structure than item get
            // List: { id, title, category, tags, ... }
            // Get: { id, title, fields, ... }
            var hasFields = element.TryGetProperty("fields", out var fieldsProp);

            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            var name = title ?? id;

            // Get category directly from element (item list format)
            var category = element.TryGetProperty("category", out var categoryProp) ? categoryProp.GetString() : null;

            // Get tags directly from element (item list format)
            var tags = new List<string>();
            if (element.TryGetProperty("tags", out var tagsProp) && tagsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var tag in tagsProp.EnumerateArray())
                {
                    if (tag.ValueKind == JsonValueKind.String)
                    {
                        tags.Add(tag.GetString() ?? string.Empty);
                    }
                }
            }

            // Value is only available when fetching full item (not in list)
            string? value = null;
            if (hasFields && fieldsProp.ValueKind == JsonValueKind.Array)
            {
                value = ExtractPasswordValue(fieldsProp);
                if (string.IsNullOrWhiteSpace(value))
                {
                    value = ExtractFirstFieldValue(fieldsProp);
                }
            }

            return new Secret
            {
                Key = id,
                Value = value ?? string.Empty,
                Name = name,
                Description = null,
                Tags = tags.Count > 0 ? tags.ToArray() : null,
                Category = category
            };
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Error parsing secret item");
            return null;
        }
    }
    
    private string? ExtractPasswordValue(JsonElement fields)
    {
        if (fields.ValueKind != JsonValueKind.Array) return null;
        
        foreach (var field in fields.EnumerateArray())
        {
            if (field.TryGetProperty("id", out var idProp) && 
                idProp.GetString() == "password" &&
                field.TryGetProperty("value", out var valueProp))
            {
                return valueProp.GetString();
            }
        }
        
        return null;
    }
    
    private string? ExtractFirstFieldValue(JsonElement fields)
    {
        if (fields.ValueKind != JsonValueKind.Array) return null;
        
        foreach (var field in fields.EnumerateArray())
        {
            if (field.TryGetProperty("value", out var valueProp))
            {
                var value = valueProp.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
        }
        
        return null;
    }
}

