using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

        var options = Options.Create(new SettingsRegistrationOptions());
        _provider = new SettingsProvider(
            _bus,
            Substitute.For<ILogger<SettingsProvider>>(),
            Services.BuildServiceProvider(),
            options);
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

    // --- Search Tests ---

    [Fact(DisplayName = "SearchSettings matches DisplayName")]
    public void SearchSettings_Matches_DisplayName()
    {
        var settings = new TestSettingsWithGroups();
        _provider.RegisterSettings(settings);

        var results = _provider.SearchSettings("Grouped").ToList();

        results.ShouldNotBeEmpty();
        results.ShouldAllBe(r => r.MatchField == "SectionName");
        results.ShouldAllBe(r => r.Settings == settings);
    }

    [Fact(DisplayName = "SearchSettings matches SettingsId")]
    public void SearchSettings_Matches_SettingsId()
    {
        var settings = new TestSettingsWithGroups();
        _provider.RegisterSettings(settings);

        var results = _provider.SearchSettings("grouped").ToList();

        results.ShouldNotBeEmpty();
        results.ShouldAllBe(r => r.MatchField == "SectionName");
    }

    [Fact(DisplayName = "SearchSettings matches property Label")]
    public void SearchSettings_Matches_PropertyLabel()
    {
        var settings = new TestSettingsWithGroups();
        _provider.RegisterSettings(settings);

        var results = _provider.SearchSettings("Font Size").ToList();

        results.Count.ShouldBe(1);
        results[0].PropertyName.ShouldBe("FontSize");
        results[0].MatchField.ShouldBe("Label");
    }

    [Fact(DisplayName = "SearchSettings matches property Description")]
    public void SearchSettings_Matches_PropertyDescription()
    {
        var settings = new TestSettingsWithGroups();
        _provider.RegisterSettings(settings);

        var results = _provider.SearchSettings("Color theme").ToList();

        results.Count.ShouldBe(1);
        results[0].PropertyName.ShouldBe("Theme");
        results[0].MatchField.ShouldBe("Description");
    }

    [Fact(DisplayName = "SearchSettings matches property Group")]
    public void SearchSettings_Matches_PropertyGroup()
    {
        var settings = new TestSettingsWithGroups();
        _provider.RegisterSettings(settings);

        var results = _provider.SearchSettings("Behavior").ToList();

        results.Count.ShouldBe(1);
        results[0].PropertyName.ShouldBe("AutoSave");
        results[0].MatchField.ShouldBe("Group");
    }

    [Fact(DisplayName = "SearchSettings is case insensitive")]
    public void SearchSettings_IsCaseInsensitive()
    {
        var settings = new TestSettingsWithGroups();
        _provider.RegisterSettings(settings);

        var results = _provider.SearchSettings("font size").ToList();

        results.Count.ShouldBe(1);
        results[0].PropertyName.ShouldBe("FontSize");
    }

    [Fact(DisplayName = "SearchSettings with empty term returns empty")]
    public void SearchSettings_EmptyTerm_ReturnsEmpty()
    {
        var settings = new TestSettingsWithGroups();
        _provider.RegisterSettings(settings);

        _provider.SearchSettings("").ShouldBeEmpty();
        _provider.SearchSettings("   ").ShouldBeEmpty();
    }

    // --- Persistence Edge Cases ---

    [Fact(DisplayName = "ApplyPersistedSettingsAsync with missing properties only applies existing")]
    public async Task ApplyPersistedSettingsAsync_WithMissingProperties_OnlyAppliesExisting()
    {
        var settings = new TestSettings { TestValue = "Original", TestNumber = 10 };
        _provider.RegisterSettings(settings);

        var persistedData = new Dictionary<string, string>
        {
            ["test"] = """{"TestValue":"Updated"}"""
        };

        await _provider.ApplyPersistedSettingsAsync(persistedData);

        settings.TestValue.ShouldBe("Updated");
        settings.TestNumber.ShouldBe(10); // unchanged
    }

    [Fact(DisplayName = "ApplyPersistedSettingsAsync with extra properties ignores unknown")]
    public async Task ApplyPersistedSettingsAsync_WithExtraProperties_IgnoresUnknown()
    {
        var settings = new TestSettings { TestValue = "Original" };
        _provider.RegisterSettings(settings);

        var persistedData = new Dictionary<string, string>
        {
            ["test"] = """{"TestValue":"Updated","UnknownProp":"should be ignored"}"""
        };

        await _provider.ApplyPersistedSettingsAsync(persistedData);

        settings.TestValue.ShouldBe("Updated");
    }

    [Fact(DisplayName = "ApplyPersistedSettingsAsync with invalid JSON does not throw")]
    public async Task ApplyPersistedSettingsAsync_WithInvalidJson_DoesNotThrow()
    {
        var settings = new TestSettings { TestValue = "Original" };
        _provider.RegisterSettings(settings);

        var persistedData = new Dictionary<string, string>
        {
            ["test"] = "not valid json {{"
        };

        await Should.NotThrowAsync(() => _provider.ApplyPersistedSettingsAsync(persistedData));
        settings.TestValue.ShouldBe("Original"); // unchanged
    }

    [Fact(DisplayName = "ApplyPersistedSettingsAsync with unregistered ID skips gracefully")]
    public async Task ApplyPersistedSettingsAsync_WithUnregisteredId_SkipsGracefully()
    {
        var settings = new TestSettings { TestValue = "Original" };
        _provider.RegisterSettings(settings);

        var persistedData = new Dictionary<string, string>
        {
            ["nonexistent"] = """{"TestValue":"Updated"}"""
        };

        await Should.NotThrowAsync(() => _provider.ApplyPersistedSettingsAsync(persistedData));
        settings.TestValue.ShouldBe("Original");
    }

    [Fact(DisplayName = "ApplyPersistedSettingsAsync does not trigger dirty flag")]
    public async Task ApplyPersistedSettingsAsync_DoesNotTriggerDirtyFlag()
    {
        var settings = new TestSettings();
        _provider.RegisterSettings(settings);
        settings.IsDirty.ShouldBeFalse();

        var persistedData = new Dictionary<string, string>
        {
            ["test"] = """{"TestValue":"Loaded","TestNumber":99}"""
        };

        await _provider.ApplyPersistedSettingsAsync(persistedData);

        settings.IsDirty.ShouldBeFalse();
    }

    // --- Dirty Tracking ---

    [Fact(DisplayName = "MarkDirty sets IsDirty to true")]
    public void MarkDirty_SetsIsDirtyTrue()
    {
        var settings = new TestSettings();
        settings.IsDirty.ShouldBeFalse();

        settings.MarkDirty();

        settings.IsDirty.ShouldBeTrue();
    }

    [Fact(DisplayName = "ClearDirty sets IsDirty to false")]
    public void ClearDirty_SetsIsDirtyFalse()
    {
        var settings = new TestSettings();
        settings.MarkDirty();
        settings.IsDirty.ShouldBeTrue();

        settings.ClearDirty();

        settings.IsDirty.ShouldBeFalse();
    }

    [Fact(DisplayName = "Modifying property does not automatically set dirty")]
    public void ModifyingProperty_DoesNotAutoSetDirty()
    {
        var settings = new TestSettings();
        settings.IsDirty.ShouldBeFalse();

        settings.TestValue = "Changed";
        settings.TestNumber = 42;

        settings.IsDirty.ShouldBeFalse();
    }

    // --- Snapshot Edge Cases ---

    [Fact(DisplayName = "CreateSnapshot only includes Setting-attributed properties")]
    public void CreateSnapshot_OnlyIncludesSettingAttributeProperties()
    {
        var settings = new TestSettingsWithGroups();
        settings.NonSettingProperty = "should not appear";

        var snapshot = settings.CreateSnapshot();

        snapshot.ShouldContainKey("FontSize");
        snapshot.ShouldContainKey("Theme");
        snapshot.ShouldContainKey("AutoSave");
        snapshot.ShouldNotContainKey("NonSettingProperty");
    }

    // --- Retrieval Edge Cases ---

    [Fact(DisplayName = "GetSettings for unregistered type returns null")]
    public void GetSettings_UnregisteredType_ReturnsNull()
    {
        _provider.GetSettings<TestSettings>().ShouldBeNull();
    }

    [Fact(DisplayName = "GetSettingsById for unregistered ID returns null")]
    public void GetSettingsById_UnregisteredId_ReturnsNull()
    {
        _provider.GetSettingsById("nonexistent").ShouldBeNull();
    }

    [Fact(DisplayName = "GetAllSettings with no registrations returns empty list")]
    public void GetAllSettings_Empty_ReturnsEmptyList()
    {
        var all = _provider.GetAllSettings();
        all.ShouldBeEmpty();
    }

    [Fact(DisplayName = "GetAllSettings ties in Order sort by DisplayName")]
    public void GetAllSettings_TiesInOrder_SortByDisplayName()
    {
        var beta = new TestSettingsBeta();
        var alpha = new TestSettingsAlpha();
        _provider.RegisterSettings(beta);
        _provider.RegisterSettings(alpha);

        var all = _provider.GetAllSettings();

        all.Count.ShouldBe(2);
        all[0].ShouldBeSameAs(alpha); // "Alpha" before "Beta"
        all[1].ShouldBeSameAs(beta);
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

public class TestSettingsWithGroups : SettingsBase
{
    public override string SettingsId => "grouped";
    public override string DisplayName => "Grouped Settings";
    public override string Icon => "icon";

    [Setting(Label = "Font Size", Description = "Controls the editor font size", Group = "Appearance")]
    public int FontSize { get; set; } = 14;

    [Setting(Label = "Theme", Description = "Color theme for the UI", Group = "Appearance")]
    public string Theme { get; set; } = "Dark";

    [Setting(Label = "Auto Save", Description = "Save files automatically", Group = "Behavior")]
    public bool AutoSave { get; set; } = true;

    public string NonSettingProperty { get; set; } = "ignored";
}

public class TestSettingsAlpha : SettingsBase
{
    public override string SettingsId => "alpha";
    public override string DisplayName => "Alpha Settings";
    public override string Icon => "icon";
    public override int Order => 50;
}

public class TestSettingsBeta : SettingsBase
{
    public override string SettingsId => "beta";
    public override string DisplayName => "Beta Settings";
    public override string Icon => "icon";
    public override int Order => 50;
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
