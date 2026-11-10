using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Mythetech.Framework.Desktop;
using Mythetech.Framework.Desktop.Plugins;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Framework.Infrastructure.Plugins.Installation;
using Mythetech.Framework.Infrastructure.Plugins.Local;
using Mythetech.Framework.Infrastructure.Settings;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Plugins;

public class PluginRegistrationValidationTests
{
    private static readonly ServiceProviderOptions Validation = new()
    {
        ValidateOnBuild = true,
        ValidateScopes = true
    };

    [Fact(DisplayName = "AddPluginFramework with a scoped HttpClient passes service validation and resolves the installer")]
    public void AddPluginFramework_WithScopedHttpClient_ResolvesInstaller()
    {
        // Mirrors a WebAssembly host, which registers HttpClient as scoped.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IJSRuntime>());
        services.AddScoped(_ => new HttpClient());
        services.AddMessageBus();
        services.AddPluginFramework();
        services.AddHttpClient();

        var provider = Should.NotThrow(() => services.BuildServiceProvider(Validation));

        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IPluginInstaller>().ShouldBeOfType<PluginInstaller>();
    }

    [Fact(DisplayName = "UsePluginFramework gives PluginState a logger, so a throwing StateChanged handler is logged")]
    public void UsePluginFramework_ThrowingStateChangedHandler_IsLogged()
    {
        var logs = new CapturingLoggerProvider();
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddProvider(logs));
        services.AddSingleton(Substitute.For<IJSRuntime>());
        services.AddMessageBus();
        services.AddPluginFramework();
        services.AddHttpClient();
        using var provider = services.BuildServiceProvider(Validation);
        provider.UsePluginFramework();
        var state = provider.GetRequiredService<PluginState>();
        state.StateChanged += (_, _) => throw new InvalidOperationException("handler off the UI thread");

        state.NotifyStateChanged();

        logs.Entries.ShouldContain(e => e.Category == typeof(PluginState).FullName && e.Level == LogLevel.Error);
    }

    [Fact(DisplayName = "AddPluginFramework alone registers the unavailable local plugin service")]
    public void AddPluginFramework_RegistersUnavailableLocalPluginService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IJSRuntime>());
        services.AddMessageBus();
        services.AddPluginFramework();
        services.AddHttpClient();

        using var provider = services.BuildServiceProvider(Validation);

        provider.GetRequiredService<ILocalPluginService>().ShouldBeOfType<UnavailableLocalPluginService>();
    }

    [Theory(DisplayName = "AddDesktopServices with the plugin framework, in either order, resolves an available local plugin service")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AddDesktopServices_WithPluginFramework_ResolvesAvailableLocalPluginService(bool desktopFirst)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IJSRuntime>());
        services.AddSettingsFramework();
        services.AddMessageBus();
        if (desktopFirst)
        {
            services.AddDesktopServices();
            services.AddPluginFramework();
        }
        else
        {
            services.AddPluginFramework();
            services.AddDesktopServices();
        }
        services.AddHttpClient();

        await using var provider = Should.NotThrow(() => services.BuildServiceProvider(Validation));

        var local = provider.GetRequiredService<ILocalPluginService>().ShouldBeOfType<LocalPluginService>();
        local.ShouldBeSameAs(provider.GetRequiredService<LocalPluginService>());
        local.IsAvailable.ShouldBeTrue();
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public List<(string Category, LogLevel Level)> Entries { get; } = [];

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, Entries);

        public void Dispose()
        {
        }

        private sealed class CapturingLogger(string category, List<(string Category, LogLevel Level)> entries) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                lock (entries)
                {
                    entries.Add((category, logLevel));
                }
            }
        }
    }
}
