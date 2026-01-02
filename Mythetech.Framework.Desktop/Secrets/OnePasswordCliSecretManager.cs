using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Secrets;

namespace Mythetech.Framework.Desktop.Secrets;

/// <summary>
/// 1Password CLI implementation of ISecretManager
/// </summary>
public class OnePasswordCliSecretManager : ISecretManager, ISecretSearcher
{
    private readonly ILogger<OnePasswordCliSecretManager> _logger;

    public OnePasswordCliSecretManager(ILogger<OnePasswordCliSecretManager> logger)
    {
        _logger = logger;
    }

    private const string OpCommand = "op";

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
        catch (InvalidOperationException ex) when (ex.Message.Contains("not signed in"))
        {
            return SecretOperationResult<IEnumerable<Secret>>.Fail(
                "1Password CLI is not signed in. Please run 'op signin' first.",
                SecretOperationErrorKind.AccessDenied);
        }
        catch (Exception ex) when (ex.Message.Contains("command not found") || ex.Message.Contains("not recognized"))
        {
            return SecretOperationResult<IEnumerable<Secret>>.Fail(
                "1Password CLI (op) is not installed or not in PATH.",
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
        catch (InvalidOperationException ex) when (ex.Message.Contains("isn't an item"))
        {
            return SecretOperationResult<Secret>.Fail(
                $"Secret '{key}' not found.",
                SecretOperationErrorKind.NotFound);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not signed in"))
        {
            return SecretOperationResult<Secret>.Fail(
                "1Password CLI is not signed in. Please run 'op signin' first.",
                SecretOperationErrorKind.AccessDenied);
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
                "1Password CLI is not signed in. Please run 'op signin' first.",
                SecretOperationErrorKind.AccessDenied);
        }
        catch (Exception ex) when (ex.Message.Contains("command not found") || ex.Message.Contains("not recognized"))
        {
            return SecretOperationResult.Fail(
                "1Password CLI (op) is not installed or not in PATH.",
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
    
    private async Task<string> ExecuteOpCommandAsync(string[] arguments, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = OpCommand,
            Arguments = string.Join(" ", arguments.Select(arg => $"\"{arg}\"")),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        using var process = new Process { StartInfo = startInfo };
        
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();
        
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                outputBuilder.AppendLine(e.Data);
            }
        };
        
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                errorBuilder.AppendLine(e.Data);
            }
        };
        
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        
        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
            catch
            {
                // Ignore errors when killing the process
            }
            throw;
        }
        
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"1Password CLI command failed with exit code {process.ExitCode}: {errorBuilder}");
        }
        
        return outputBuilder.ToString().Trim();
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

