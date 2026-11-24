using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Settings;
using Mythetech.Framework.Infrastructure.Settings.Consumers;
using Mythetech.Framework.Infrastructure.Settings.Events;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.FeatureFlags;

public class FeatureFlagSettingsTests
{
    private readonly ISettingsStorage _storage = Substitute.For<ISettingsStorage>();
    private readonly TestFeatureFlags _flags = new();

    [Fact(DisplayName = "A flag is saved with its settings")]
    public async Task A_flag_is_saved_with_its_settings()
    {
        var persister = new SettingsPersister(NullLogger<SettingsPersister>.Instance, _storage);
        _flags.TestFeature = true;

        await persister.Consume(new SettingsModelChanged(_flags));

        await _storage.Received(1).SaveSettingsAsync("TestFeatureFlags",
            Arg.Is<string>(json => json.Contains("\"TestFeature\":true")));
    }

    [Fact(DisplayName = "A saved flag is loaded")]
    public async Task A_saved_flag_is_loaded()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMessageBus();
        services.AddSettingsFramework();
        var provider = services.BuildServiceProvider().GetRequiredService<ISettingsProvider>();
        provider.RegisterSettings(_flags);

        await provider.ApplyPersistedSettingsAsync(new Dictionary<string, string>
        {
            ["TestFeatureFlags"] = """{"TestFeature":true,"Beta":false}"""
        });

        (_flags.TestFeature, _flags.Beta).ShouldBe((true, false));
    }

    [Fact(DisplayName = "A snapshot restores a flag")]
    public void A_snapshot_restores_a_flag()
    {
        var snapshot = _flags.CreateSnapshot();
        _flags.SetFlagValue("TestFeature", true);

        _flags.RestoreFromSnapshot(snapshot);

        _flags.TestFeature.ShouldBeFalse();
    }
}
