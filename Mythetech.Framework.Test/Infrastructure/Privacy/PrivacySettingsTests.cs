using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Privacy;
using Mythetech.Framework.Infrastructure.Settings;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Privacy;

public class PrivacySettingsTests
{
    [Fact(DisplayName = "CrashReportingEnabled defaults to false")]
    public void CrashReportingEnabled_Defaults_To_False()
    {
        var settings = new PrivacySettings();
        settings.CrashReportingEnabled.ShouldBeFalse();
    }

    [Fact(DisplayName = "ErrorReportingEnabled defaults to false")]
    public void ErrorReportingEnabled_Defaults_To_False()
    {
        var settings = new PrivacySettings();
        settings.ErrorReportingEnabled.ShouldBeFalse();
    }

    [Fact(DisplayName = "HasSeenPrivacyDialog defaults to false")]
    public void HasSeenPrivacyDialog_Defaults_To_False()
    {
        var settings = new PrivacySettings();
        settings.HasSeenPrivacyDialog.ShouldBeFalse();
    }

    [Fact(DisplayName = "SettingsId is privacy")]
    public void SettingsId_Is_Privacy()
    {
        var settings = new PrivacySettings();
        settings.SettingsId.ShouldBe("privacy");
    }

    [Fact(DisplayName = "Order is int.MaxValue")]
    public void Order_Is_MaxValue()
    {
        var settings = new PrivacySettings();
        settings.Order.ShouldBe(int.MaxValue);
    }
}

public class PrivacySettingsExtensionsTests
{
    private readonly ISettingsProvider _provider;

    public PrivacySettingsExtensionsTests()
    {
        _provider = Substitute.For<ISettingsProvider>();
    }

    [Fact(DisplayName = "HasSeenPrivacyDialog returns false when not seen")]
    public void HasSeenPrivacyDialog_Returns_False_When_Not_Seen()
    {
        var settings = new PrivacySettings();
        _provider.GetSettings<PrivacySettings>().Returns(settings);

        _provider.HasSeenPrivacyDialog().ShouldBeFalse();
    }

    [Fact(DisplayName = "HasSeenPrivacyDialog returns true when seen")]
    public void HasSeenPrivacyDialog_Returns_True_When_Seen()
    {
        var settings = new PrivacySettings { HasSeenPrivacyDialog = true };
        _provider.GetSettings<PrivacySettings>().Returns(settings);

        _provider.HasSeenPrivacyDialog().ShouldBeTrue();
    }

    [Fact(DisplayName = "HasSeenPrivacyDialog returns false when settings not registered")]
    public void HasSeenPrivacyDialog_Returns_False_When_Settings_Null()
    {
        _provider.GetSettings<PrivacySettings>().Returns((PrivacySettings?)null);

        _provider.HasSeenPrivacyDialog().ShouldBeFalse();
    }
}
