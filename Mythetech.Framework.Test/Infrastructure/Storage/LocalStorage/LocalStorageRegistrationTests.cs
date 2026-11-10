using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Framework.Infrastructure.Settings;
using Mythetech.Framework.WebAssembly;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Storage.LocalStorage;

// The Framework resolves both services from the root provider, which Development's scope validation rejects for scoped ones.
public class LocalStorageRegistrationTests
{
    [Fact(DisplayName = "AddWebAssemblySettingsStorage resolves from the root provider under scope validation")]
    public void AddWebAssemblySettingsStorage_ResolvesFromTheRootProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IJSRuntime>());
        services.AddLogging();
        services.AddWebAssemblySettingsStorage();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        Should.NotThrow(() => provider.GetRequiredService<ISettingsStorage>());
    }

    [Fact(DisplayName = "AddPluginStateProvider resolves from the root provider under scope validation")]
    public void AddPluginStateProvider_ResolvesFromTheRootProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IJSRuntime>());
        services.AddLogging();
        services.AddPluginStateProvider();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        Should.NotThrow(() => provider.GetRequiredService<IPluginStateProvider>());
    }
}
