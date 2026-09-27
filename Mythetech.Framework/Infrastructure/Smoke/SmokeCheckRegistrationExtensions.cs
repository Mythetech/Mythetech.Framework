using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Mythetech.Framework.Infrastructure.Smoke;

/// <summary>
/// Registers smoke checks.
/// </summary>
public static class SmokeCheckRegistrationExtensions
{
    /// <summary>
    /// Opts the app into the general Framework smoke checks (<c>framework/initialization</c>,
    /// <c>framework/storage</c> and <c>framework/plugins</c>, each only when the app uses that part of the
    /// Framework) and returns a builder for the app's own checks. Nothing is created or run outside a smoke
    /// run, and the order relative to other registrations does not matter.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public static SmokeCheckBuilder AddSmokeChecks(this IServiceCollection services)
    {
        // A desktop host registers its own context; this keeps ISmokeTestContext injectable everywhere else.
        services.TryAddSingleton<ISmokeTestContext>(DisabledSmokeTestContext.Instance);
        services.TryAddSingleton<FrameworkSmokeChecks>();
        return new SmokeCheckBuilder(services);
    }
}
