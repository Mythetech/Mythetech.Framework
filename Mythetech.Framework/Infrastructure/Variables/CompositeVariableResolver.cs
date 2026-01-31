using Microsoft.Extensions.Logging;

namespace Mythetech.Framework.Infrastructure.Variables;

/// <summary>
/// A variable resolver that chains multiple resolvers together.
/// Uses the first resolver that can handle each value.
/// </summary>
public class CompositeVariableResolver : IVariableResolver
{
    private readonly IEnumerable<IVariableResolver> _resolvers;
    private readonly ILogger<CompositeVariableResolver>? _logger;

    /// <summary>
    /// Creates a composite resolver from the given resolvers.
    /// </summary>
    /// <param name="resolvers">Resolvers to chain, checked in order</param>
    /// <param name="logger">Optional logger for diagnostics</param>
    public CompositeVariableResolver(
        IEnumerable<IVariableResolver> resolvers,
        ILogger<CompositeVariableResolver>? logger = null)
    {
        _resolvers = resolvers;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool CanResolve(string value)
    {
        return _resolvers.Any(r => r.CanResolve(value));
    }

    /// <inheritdoc />
    public async Task<VariableResolutionResult> ResolveAsync(string value, CancellationToken cancellationToken = default)
    {
        foreach (var resolver in _resolvers)
        {
            if (resolver.CanResolve(value))
            {
                try
                {
                    _logger?.LogDebug("Using {ResolverType} for value pattern", resolver.GetType().Name);
                    return await resolver.ResolveAsync(value, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Resolver {ResolverType} failed for value",
                        resolver.GetType().Name);
                    return VariableResolutionResult.Fail(
                        $"[ERROR:{value}]",
                        $"Resolver failed: {ex.Message}"
                    );
                }
            }
        }

        _logger?.LogDebug("No resolver found for value, returning as-is");
        return VariableResolutionResult.Ok(value);
    }
}
