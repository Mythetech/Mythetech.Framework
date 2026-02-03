using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mythetech.Framework.Observability.Context;
using Mythetech.Framework.Observability.Exceptions;
using Mythetech.Framework.Observability.Health;
using Mythetech.Framework.Observability.Metrics;
using Mythetech.Framework.Observability.Performance;
using Mythetech.Framework.Observability.Reporting;

namespace Mythetech.Framework.Observability;

/// <summary>
/// Extension methods for registering observability services.
/// </summary>
public static class ObservabilityRegistrationExtensions
{
    /// <summary>
    /// Adds observability services with default options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddObservability(this IServiceCollection services)
        => services.AddObservability(_ => { });

    /// <summary>
    /// Adds observability services with custom options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddObservability(this IServiceCollection services, Action<ObservabilityOptions> configure)
    {
        var options = new ObservabilityOptions();
        configure(options);

        services.AddSingleton(options);

        // Metrics
        services.TryAddSingleton<IMeterFactory>(sp =>
            new DiagnosticsMeterFactory(options.MeterName, options.MeterVersion));

        // Health checks
        services.TryAddSingleton<IHealthCheckService, InMemoryHealthCheckService>();

        // Operation context
        if (options.EnableOperationContext)
        {
            services.TryAddSingleton<IOperationContext, AsyncLocalOperationContext>();
        }

        // Performance monitoring
        if (options.EnablePerformanceMonitoring)
        {
            services.TryAddSingleton<IPerformanceMonitor, DefaultPerformanceMonitor>();
        }

        // Exception handler
        if (options.EnableExceptionHandler)
        {
            services.TryAddSingleton<IExceptionHandler, DefaultExceptionHandler>();
        }

        // Diagnostic context collector
        services.TryAddSingleton<IDiagnosticContextCollector, DefaultDiagnosticContextCollector>();

        return services;
    }

    /// <summary>
    /// Adds a health check to the service collection.
    /// </summary>
    /// <typeparam name="T">The health check type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHealthCheck<T>(this IServiceCollection services)
        where T : class, IHealthCheck
    {
        services.AddSingleton<IHealthCheck, T>();
        return services;
    }

    /// <summary>
    /// Adds a health check instance to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="healthCheck">The health check instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHealthCheck(this IServiceCollection services, IHealthCheck healthCheck)
    {
        services.AddSingleton(healthCheck);
        return services;
    }

    /// <summary>
    /// Adds an exception observer to the service collection.
    /// </summary>
    /// <typeparam name="T">The exception observer type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddExceptionObserver<T>(this IServiceCollection services)
        where T : class, IExceptionObserver
    {
        services.AddSingleton<IExceptionObserver, T>();
        return services;
    }

    /// <summary>
    /// Adds an exception observer instance to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="observer">The exception observer instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddExceptionObserver(this IServiceCollection services, IExceptionObserver observer)
    {
        services.AddSingleton(observer);
        return services;
    }

    /// <summary>
    /// Adds a diagnostic context provider to the service collection.
    /// </summary>
    /// <typeparam name="T">The diagnostic context provider type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDiagnosticContextProvider<T>(this IServiceCollection services)
        where T : class, IDiagnosticContextProvider
    {
        services.AddSingleton<IDiagnosticContextProvider, T>();
        return services;
    }

    /// <summary>
    /// Adds a diagnostic context provider instance to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="provider">The diagnostic context provider instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDiagnosticContextProvider(this IServiceCollection services, IDiagnosticContextProvider provider)
    {
        services.AddSingleton(provider);
        return services;
    }
}
