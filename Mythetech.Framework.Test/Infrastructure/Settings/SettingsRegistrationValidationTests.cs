using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Mythetech.Framework.Desktop;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Settings;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Settings;

public class SettingsRegistrationValidationTests
{
    private static readonly ServiceProviderOptions Validation = new()
    {
        ValidateOnBuild = true,
        ValidateScopes = true
    };

    [Fact(DisplayName = "AddSettingsFramework without the plugin framework passes service validation")]
    public void AddSettingsFramework_WithoutPluginFramework_PassesValidation()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSettingsFramework();
        services.AddMessageBus();

        Should.NotThrow(() => services.BuildServiceProvider(Validation));
    }

    [Fact(DisplayName = "AddDesktopServices without the plugin framework passes service validation")]
    public void AddDesktopServices_WithoutPluginFramework_PassesValidation()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IJSRuntime>());
        services.AddSettingsFramework();
        services.AddMessageBus();
        services.AddDesktopServices();

        Should.NotThrow(() => services.BuildServiceProvider(Validation));
    }
}
