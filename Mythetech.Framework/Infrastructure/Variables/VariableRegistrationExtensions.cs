using Microsoft.Extensions.DependencyInjection;

namespace Mythetech.Framework.Infrastructure.Variables;

/// <summary>
/// Extension methods for registering variable resolution services.
/// </summary>
public static class VariableRegistrationExtensions
{
    /// <summary>
    /// Adds the variable resolution infrastructure with default resolvers.
    /// Includes: EnvironmentVariableResolver, DynamicVariableResolver.
    /// </summary>
    public static IServiceCollection AddVariableResolution(this IServiceCollection services)
    {
        services.AddSingleton<IVariableResolver, EnvironmentVariableResolver>();
        services.AddSingleton<IVariableResolver, DynamicVariableResolver>();
        services.AddSingleton<CompositeVariableResolver>();

        return services;
    }

    /// <summary>
    /// Adds only the environment variable resolver ($env:VARNAME).
    /// </summary>
    public static IServiceCollection AddEnvironmentVariableResolver(this IServiceCollection services)
    {
        services.AddSingleton<IVariableResolver, EnvironmentVariableResolver>();
        return services;
    }

    /// <summary>
    /// Adds only the dynamic variable resolver ($uuid, $timestamp, etc.).
    /// </summary>
    public static IServiceCollection AddDynamicVariableResolver(this IServiceCollection services)
    {
        services.AddSingleton<IVariableResolver, DynamicVariableResolver>();
        return services;
    }

    /// <summary>
    /// Adds a custom variable resolver.
    /// </summary>
    /// <typeparam name="TResolver">The resolver type</typeparam>
    public static IServiceCollection AddVariableResolver<TResolver>(this IServiceCollection services)
        where TResolver : class, IVariableResolver
    {
        services.AddSingleton<IVariableResolver, TResolver>();
        return services;
    }

    /// <summary>
    /// Adds a custom variable resolver instance.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="resolver">The resolver instance</param>
    public static IServiceCollection AddVariableResolver(this IServiceCollection services, IVariableResolver resolver)
    {
        services.AddSingleton(resolver);
        return services;
    }
}
