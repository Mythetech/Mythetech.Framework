using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Framework.Infrastructure.Plugins.Installation;
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
}
