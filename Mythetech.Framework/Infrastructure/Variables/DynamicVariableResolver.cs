namespace Mythetech.Framework.Infrastructure.Variables;

/// <summary>
/// Metadata for a dynamic variable including its generator, description, and example output.
/// </summary>
/// <param name="Name">The variable name (e.g., "$uuid")</param>
/// <param name="Description">Human-readable description</param>
/// <param name="Generator">Function that generates the value</param>
/// <param name="ExampleOutput">Example output for documentation</param>
public record DynamicVariableInfo(
    string Name,
    string Description,
    Func<string> Generator,
    string ExampleOutput
);

/// <summary>
/// Resolves dynamic variables that generate values at runtime.
/// Supports: $uuid, $timestamp, $isoDate, $randomInt, $randomString
/// </summary>
public class DynamicVariableResolver : IVariableResolver
{
    private static readonly Dictionary<string, DynamicVariableInfo> BuiltInVariables = new(StringComparer.OrdinalIgnoreCase)
    {
        ["$uuid"] = new DynamicVariableInfo(
            "$uuid",
            "Random UUID v4",
            () => Guid.NewGuid().ToString(),
            "550e8400-e29b-41d4-a716-446655440000"
        ),
        ["$timestamp"] = new DynamicVariableInfo(
            "$timestamp",
            "Unix timestamp (seconds)",
            () => DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
            "1704067200"
        ),
        ["$timestampMs"] = new DynamicVariableInfo(
            "$timestampMs",
            "Unix timestamp (milliseconds)",
            () => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(),
            "1704067200000"
        ),
        ["$isoDate"] = new DynamicVariableInfo(
            "$isoDate",
            "ISO 8601 date/time (UTC)",
            () => DateTime.UtcNow.ToString("O"),
            "2024-01-01T00:00:00.0000000Z"
        ),
        ["$date"] = new DynamicVariableInfo(
            "$date",
            "Date only (yyyy-MM-dd)",
            () => DateTime.UtcNow.ToString("yyyy-MM-dd"),
            "2024-01-01"
        ),
        ["$randomInt"] = new DynamicVariableInfo(
            "$randomInt",
            "Random integer (0 to max int)",
            () => Random.Shared.Next(0, int.MaxValue).ToString(),
            "1847293654"
        ),
        ["$randomString"] = new DynamicVariableInfo(
            "$randomString",
            "Random 16-character alphanumeric string",
            () => Guid.NewGuid().ToString("N")[..16],
            "a1b2c3d4e5f6g7h8"
        )
    };

    private readonly Dictionary<string, DynamicVariableInfo> _variables;

    /// <summary>
    /// Creates a resolver with only the built-in variables.
    /// </summary>
    public DynamicVariableResolver()
    {
        _variables = new Dictionary<string, DynamicVariableInfo>(BuiltInVariables, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Creates a resolver with built-in variables plus custom variables.
    /// </summary>
    /// <param name="customVariables">Additional custom variables to register</param>
    public DynamicVariableResolver(IEnumerable<DynamicVariableInfo> customVariables)
        : this()
    {
        foreach (var variable in customVariables)
        {
            _variables[variable.Name] = variable;
        }
    }

    /// <summary>
    /// Gets the list of supported dynamic variable names.
    /// </summary>
    public IReadOnlyCollection<string> SupportedVariables => _variables.Keys;

    /// <summary>
    /// Gets metadata for all supported dynamic variables.
    /// </summary>
    public IReadOnlyCollection<DynamicVariableInfo> GetVariableInfos() => _variables.Values;

    /// <summary>
    /// Registers a custom dynamic variable.
    /// </summary>
    /// <param name="info">The variable metadata and generator</param>
    public void RegisterVariable(DynamicVariableInfo info)
    {
        _variables[info.Name] = info;
    }

    /// <inheritdoc />
    public bool CanResolve(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        return value.StartsWith('$') && _variables.ContainsKey(value);
    }

    /// <inheritdoc />
    public Task<VariableResolutionResult> ResolveAsync(string value, CancellationToken cancellationToken = default)
    {
        if (_variables.TryGetValue(value, out var info))
        {
            try
            {
                var resolvedValue = info.Generator();
                return Task.FromResult(VariableResolutionResult.Ok(resolvedValue));
            }
            catch (Exception ex)
            {
                return Task.FromResult(VariableResolutionResult.Fail(
                    $"[DYNAMIC_ERROR:{value}]",
                    $"Failed to generate dynamic value for '{value}': {ex.Message}"
                ));
            }
        }

        return Task.FromResult(VariableResolutionResult.Fail(
            value,
            $"Unknown dynamic variable: {value}. Supported: {string.Join(", ", _variables.Keys)}"
        ));
    }
}
