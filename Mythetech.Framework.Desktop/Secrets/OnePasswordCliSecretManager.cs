using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Mythetech.Framework.Infrastructure.Secrets;

namespace Mythetech.Framework.Desktop.Secrets;

/// <summary>
/// 1Password CLI implementation of ISecretManager
/// </summary>
public class OnePasswordCliSecretManager : ISecretManager
{
    private const string OpCommand = "op";
    
    /// <inheritdoc />
    public async Task<IEnumerable<Secret>> ListSecretsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await ExecuteOpCommandAsync(["item", "list", "--format", "json"], cancellationToken);
            if (string.IsNullOrWhiteSpace(result))
            {
                return [];
            }
            
            return ParseItemList(result);
        }
        catch
        {
            return [];
        }
    }
    
    /// <inheritdoc />
    public async Task<Secret?> GetSecretAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        
        try
        {
            var result = await ExecuteOpCommandAsync(["item", "get", key, "--format", "json"], cancellationToken);
            if (string.IsNullOrWhiteSpace(result))
            {
                return null;
            }
            
            return ParseItem(result);
        }
        catch
        {
            return null;
        }
    }
    
    /// <inheritdoc />
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await ExecuteOpCommandAsync(["account", "list"], cancellationToken);
            return !string.IsNullOrWhiteSpace(result);
        }
        catch
        {
            try
            {
                var result = await ExecuteOpCommandAsync(["whoami"], cancellationToken);
                return !string.IsNullOrWhiteSpace(result);
            }
            catch
            {
                return false;
            }
        }
    }
    
    /// <inheritdoc />
    public async Task<IEnumerable<Secret>> SearchSecretsAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(searchTerm);
        
        try
        {
            var allSecrets = await ListSecretsAsync(cancellationToken);
            var term = searchTerm.ToLowerInvariant();
            
            return allSecrets.Where(s =>
                s.Key.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (s.Name?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (s.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (s.Tags?.Any(t => t.Contains(term, StringComparison.OrdinalIgnoreCase)) ?? false)
            );
        }
        catch
        {
            return [];
        }
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
            var overview = element.TryGetProperty("overview", out var overviewProp) ? overviewProp : default;
            var fields = element.TryGetProperty("fields", out var fieldsProp) ? fieldsProp : default;
            
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }
            
            var name = title ?? id;
            var description = overview.TryGetProperty("notesPlain", out var notesProp) ? notesProp.GetString() : null;
            
            var tags = new List<string>();
            if (overview.ValueKind == JsonValueKind.Object && overview.TryGetProperty("tags", out var tagsProp))
            {
                foreach (var tag in tagsProp.EnumerateArray())
                {
                    if (tag.ValueKind == JsonValueKind.String)
                    {
                        tags.Add(tag.GetString() ?? string.Empty);
                    }
                }
            }
            
            var category = overview.TryGetProperty("category", out var categoryProp) ? categoryProp.GetString() : null;
            
            var value = ExtractPasswordValue(fields);
            if (string.IsNullOrWhiteSpace(value))
            {
                value = ExtractFirstFieldValue(fields);
            }
            
            return new Secret
            {
                Key = id,
                Value = value ?? string.Empty,
                Name = name,
                Description = description,
                Tags = tags.Count > 0 ? tags.ToArray() : null,
                Category = category
            };
        }
        catch
        {
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

