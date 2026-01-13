using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Settings;
using Mythetech.Framework.Infrastructure.Settings.Events;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Settings;

public class SettingsProviderTests : TestContext
{
    private readonly IMessageBus _bus;
    private readonly SettingsProvider _provider;

    public SettingsProviderTests()
    {
        _bus = new InMemoryMessageBus(
            Services,
            Substitute.For<ILogger<InMemoryMessageBus>>(),
            Array.Empty<IMessagePipe>(),
            Array.Empty<IConsumerFilter>());
        Services.AddSingleton(_bus);

        _provider = new SettingsProvider(_bus, Substitute.For<ILogger<SettingsProvider>>());
    }

    [Fact(DisplayName = "Can register and retrieve settings by type")]
    public void Can_Register_And_Retrieve_Settings_By_Type()
    {
        // Arrange
        var settings = new TestSettings();

        // Act
        _provider.RegisterSettings(settings);
        var retrieved = _provider.GetSettings<TestSettings>();

        // Assert
        retrieved.ShouldNotBeNull();
        retrieved.ShouldBeSameAs(settings);
    }

    [Fact(DisplayName = "Can register and retrieve settings by ID")]
    public void Can_Register_And_Retrieve_Settings_By_Id()
    {
        // Arrange
        var settings = new TestSettings();

        // Act
        _provider.RegisterSettings(settings);
        var retrieved = _provider.GetSettingsById("test");

        // Assert
        retrieved.ShouldNotBeNull();
        retrieved.ShouldBeSameAs(settings);
    }

    [Fact(DisplayName = "GetAllSettings returns settings ordered by Order property")]
    public void GetAllSettings_Returns_Ordered_Settings()
    {
        // Arrange
        var highOrder = new TestSettingsHighOrder();
        var lowOrder = new TestSettingsLowOrder();
        _provider.RegisterSettings(highOrder);
        _provider.RegisterSettings(lowOrder);

        // Act
        var allSettings = _provider.GetAllSettings();

        // Assert
        allSettings.Count.ShouldBe(2);
        allSettings[0].ShouldBeSameAs(lowOrder);
        allSettings[1].ShouldBeSameAs(highOrder);
    }

    [Fact(DisplayName = "Duplicate registration is ignored with warning")]
    public void Duplicate_Registration_Is_Ignored()
    {
        // Arrange
        var settings1 = new TestSettings { TestValue = "First" };
        var settings2 = new TestSettings { TestValue = "Second" };

        // Act
        _provider.RegisterSettings(settings1);
        _provider.RegisterSettings(settings2);
        var retrieved = _provider.GetSettings<TestSettings>();

        // Assert
        retrieved.ShouldNotBeNull();
        retrieved.TestValue.ShouldBe("First");
    }

    [Fact(DisplayName = "NotifySettingsChangedAsync publishes SettingsModelChanged event")]
    public async Task NotifySettingsChangedAsync_Publishes_Event()
    {
        // Arrange
        var settings = new TestSettings { TestValue = "Hello" };
        _provider.RegisterSettings(settings);

        SettingsModelChanged? receivedEvent = null;
        var consumer = new TestSettingsConsumer(e => receivedEvent = e);
        _bus.Subscribe(consumer);

        // Act
        await _provider.NotifySettingsChangedAsync(settings);

        // Assert
        receivedEvent.ShouldNotBeNull();
        receivedEvent.Settings.ShouldBeSameAs(settings);
    }

    [Fact(DisplayName = "NotifySettingsChangedAsync clears dirty flag")]
    public async Task NotifySettingsChangedAsync_Clears_Dirty_Flag()
    {
        // Arrange
        var settings = new TestSettings();
        settings.MarkDirty();
        settings.IsDirty.ShouldBeTrue();

        // Act
        await _provider.NotifySettingsChangedAsync(settings);

        // Assert
        settings.IsDirty.ShouldBeFalse();
    }

    [Fact(DisplayName = "ApplyPersistedSettingsAsync applies JSON data to settings")]
    public async Task ApplyPersistedSettingsAsync_Applies_Json_Data()
    {
        // Arrange
        var settings = new TestSettings { TestValue = "Original", TestNumber = 10 };
        _provider.RegisterSettings(settings);

        var persistedData = new Dictionary<string, string>
        {
            ["test"] = """{"TestValue":"Updated","TestNumber":42}"""
        };

        // Act
        await _provider.ApplyPersistedSettingsAsync(persistedData);

        // Assert
        settings.TestValue.ShouldBe("Updated");
        settings.TestNumber.ShouldBe(42);
    }

    [Fact(DisplayName = "CreateSnapshot captures property values")]
    public void CreateSnapshot_Captures_Property_Values()
    {
        // Arrange
        var settings = new TestSettings { TestValue = "Hello", TestNumber = 42 };

        // Act
        var snapshot = settings.CreateSnapshot();

        // Assert
        snapshot.ShouldContainKey("TestValue");
        snapshot.ShouldContainKey("TestNumber");
        snapshot["TestValue"].ShouldBe("Hello");
        snapshot["TestNumber"].ShouldBe(42);
    }

    [Fact(DisplayName = "RestoreFromSnapshot restores property values")]
    public void RestoreFromSnapshot_Restores_Property_Values()
    {
        // Arrange
        var settings = new TestSettings { TestValue = "Original", TestNumber = 10 };
        var snapshot = settings.CreateSnapshot();

        settings.TestValue = "Modified";
        settings.TestNumber = 99;

        // Act
        settings.RestoreFromSnapshot(snapshot);

        // Assert
        settings.TestValue.ShouldBe("Original");
        settings.TestNumber.ShouldBe(10);
    }
}

public class TestSettings : SettingsBase
{
    public override string SettingsId => "test";
    public override string DisplayName => "Test Settings";
    public override string Icon => "test_icon";

    [Setting(Label = "Test Value")]
    public string TestValue { get; set; } = "Default";

    [Setting(Label = "Test Number", Min = 0, Max = 100)]
    public int TestNumber { get; set; } = 0;
}

public class TestSettingsLowOrder : SettingsBase
{
    public override string SettingsId => "low";
    public override string DisplayName => "Low Order";
    public override string Icon => "icon";
    public override int Order => 10;
}

public class TestSettingsHighOrder : SettingsBase
{
    public override string SettingsId => "high";
    public override string DisplayName => "High Order";
    public override string Icon => "icon";
    public override int Order => 100;
}

public class TestSettingsConsumer : IConsumer<SettingsModelChanged>
{
    private readonly Action<SettingsModelChanged> _onReceived;

    public TestSettingsConsumer(Action<SettingsModelChanged> onReceived)
    {
        _onReceived = onReceived;
    }

    public Task Consume(SettingsModelChanged message)
    {
        _onReceived(message);
        return Task.CompletedTask;
    }
}
