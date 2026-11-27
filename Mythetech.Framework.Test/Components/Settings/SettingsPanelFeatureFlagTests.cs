using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Mythetech.Framework.Components.FeatureFlags;
using Mythetech.Framework.Components.Settings;
using Mythetech.Framework.Infrastructure.Settings;
using Mythetech.Framework.Test.Infrastructure.FeatureFlags;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Components.Settings;

public class SettingsPanelFeatureFlagTests : BunitContext
{
    private readonly ISettingsProvider _provider = Substitute.For<ISettingsProvider>();
    private readonly TestFeatureFlags _flags = new();

    public SettingsPanelFeatureFlagTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        _provider.GetAllSettings().Returns([_flags]);
        Services.AddSingleton(_provider);
    }

    [Fact(DisplayName = "A class of flags alone shows its section")]
    public void A_class_of_flags_alone_shows_its_section()
    {
        var cut = Render<SettingsPanel>();

        cut.FindComponents<FeatureFlagSettingsSection>().Count.ShouldBe(1);
    }

    [Fact(DisplayName = "Done publishes a toggled flag")]
    public async Task Done_publishes_a_toggled_flag()
    {
        var cut = Render<SettingsPanel>();
        _flags.SetFlagValue("TestFeature", true);
        _flags.MarkDirty();

        await cut.InvokeAsync(() => cut.Instance.CommitChangesAsync());

        await _provider.Received(1).NotifySettingsChangedAsync(_flags);
    }
}
