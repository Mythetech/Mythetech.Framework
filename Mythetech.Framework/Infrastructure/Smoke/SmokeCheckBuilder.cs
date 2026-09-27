using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Mythetech.Framework.Infrastructure.Smoke;

/// <summary>
/// Registers an app's own smoke checks alongside the general Framework checks.
/// Returned by <see cref="SmokeCheckRegistrationExtensions.AddSmokeChecks"/>.
/// </summary>
public sealed class SmokeCheckBuilder
{
    private readonly IServiceCollection _services;

    internal SmokeCheckBuilder(IServiceCollection services)
    {
        _services = services;
    }

    /// <summary>
    /// Registers a smoke check. Registration is cheap and always allowed; the check is only created and
    /// run in a smoke run.
    /// </summary>
    /// <typeparam name="T">The check type.</typeparam>
    public SmokeCheckBuilder WithSmokeCheck<T>() where T : class, ISmokeCheck
    {
        _services.TryAddEnumerable(ServiceDescriptor.Transient<ISmokeCheck, T>());
        return this;
    }
}
