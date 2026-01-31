namespace Mythetech.Framework.Infrastructure.Variables;

/// <summary>
/// Resolves environment variable references to their values.
/// Pattern: $env:VARIABLE_NAME
/// </summary>
public class EnvironmentVariableResolver : IVariableResolver
{
    private const string EnvPrefix = "$env:";

    /// <inheritdoc />
    public bool CanResolve(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        return value.StartsWith(EnvPrefix, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Extracts the environment variable name from a reference value.
    /// </summary>
    /// <param name="value">The full value (e.g., "$env:PATH")</param>
    /// <returns>The variable name (e.g., "PATH")</returns>
    public static string ExtractVariableName(string value)
    {
        if (string.IsNullOrEmpty(value) || !value.StartsWith(EnvPrefix, StringComparison.OrdinalIgnoreCase))
            return value;

        return value[EnvPrefix.Length..];
    }

    /// <summary>
    /// Creates an environment variable reference from a variable name.
    /// </summary>
    /// <param name="variableName">The environment variable name</param>
    /// <returns>The reference value (e.g., "$env:PATH")</returns>
    public static string CreateReference(string variableName)
    {
        return $"{EnvPrefix}{variableName}";
    }

    /// <inheritdoc />
    public Task<VariableResolutionResult> ResolveAsync(string value, CancellationToken cancellationToken = default)
    {
        var variableName = ExtractVariableName(value);

        if (string.IsNullOrWhiteSpace(variableName))
        {
            return Task.FromResult(VariableResolutionResult.Fail(
                "[ENV_ERROR:empty_name]",
                "Environment variable reference has an empty name"
            ));
        }

        try
        {
            var envValue = System.Environment.GetEnvironmentVariable(variableName);

            if (envValue == null)
            {
                return Task.FromResult(VariableResolutionResult.Fail(
                    $"[ENV_NOT_FOUND:{variableName}]",
                    $"Environment variable '{variableName}' is not set"
                ));
            }

            return Task.FromResult(VariableResolutionResult.Ok(envValue));
        }
        catch (Exception ex)
        {
            return Task.FromResult(VariableResolutionResult.Fail(
                $"[ENV_ERROR:{variableName}]",
                $"Failed to read environment variable '{variableName}': {ex.Message}"
            ));
        }
    }
}
