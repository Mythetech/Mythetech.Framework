using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Framework.Infrastructure.Secrets;
using Mythetech.Framework.Infrastructure.Secrets.Consumers;
using Mythetech.Framework.Infrastructure.Settings;
using Mythetech.Framework.Infrastructure.Settings.Events;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Secrets;

public class SecretManagerSettingsConsumerTests
{
    /// <summary>
    /// AddSettingsFramework registers every settings consumer in the Framework, the secret manager's
    /// among them, and a host in Development validates each registration when it builds.
    /// </summary>
    [Fact(DisplayName = "A host that uses settings without the secret manager passes service validation")]
    public void HostWithoutSecretManager_PassesServiceValidation()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMessageBus(Array.Empty<Assembly>());
        services.AddSingleton(new PluginState());
        services.AddSettingsFramework();

        Should.NotThrow(() =>
        {
            using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
        });
    }

    [Fact(DisplayName = "Without the secret manager a saved choice of manager is ignored")]
    public async Task WithoutSecretManager_SavedChoiceIsIgnored()
    {
        var consumer = new SecretManagerSettingsConsumer();
        var saved = new SecretManagerSettings { ActiveManagerName = "1Password CLI" };

        await Should.NotThrowAsync(() => consumer.Consume(new SettingsModelChanged<SecretManagerSettings>(saved)));
    }
}
