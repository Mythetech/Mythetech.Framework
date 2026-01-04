using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Mythetech.Framework.Infrastructure.Environment;

namespace Mythetech.Framework.WebAssembly.Environment;

/// <summary>
/// WebAssembly implementation of <see cref="IRuntimeEnvironment"/>.
/// Uses <see cref="IWebAssemblyHostEnvironment"/> from Blazor WebAssembly hosting
/// to determine the environment name automatically.
/// </summary>
public class WebAssemblyRuntimeEnvironment : IRuntimeEnvironment
{
    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public Version Version { get; }

    /// <inheritdoc/>
    public string BaseAddress { get; }

    /// <summary>
    /// Creates a new WebAssembly runtime environment.
    /// </summary>
    /// <param name="hostEnvironment">The Blazor WebAssembly host environment</param>
    /// <param name="navigationManager">The navigation manager for base URL</param>
    /// <param name="version">Optional version override. If not specified, uses the entry assembly version.</param>
    public WebAssemblyRuntimeEnvironment(
        IWebAssemblyHostEnvironment hostEnvironment,
        NavigationManager navigationManager,
        Version? version = null)
    {
        Name = hostEnvironment.Environment;
        BaseAddress = navigationManager.BaseUri;
        Version = version ?? Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(1, 0, 0);
    }
}
