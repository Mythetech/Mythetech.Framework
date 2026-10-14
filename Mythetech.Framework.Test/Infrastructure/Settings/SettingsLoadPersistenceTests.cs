using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Settings;
using Mythetech.Framework.Infrastructure.Settings.Consumers;
using Mythetech.Framework.Infrastructure.Settings.Events;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Settings;

public class SettingsLoadPersistenceTests
{
    private readonly ISettingsStorage _storage = Substitute.For<ISettingsStorage>();
    private readonly TestSettings _settings = new();
    private readonly IMessageBus _bus;
    private readonly ISettingsProvider _provider;

    public SettingsLoadPersistenceTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMessageBus();
        services.AddSettingsFramework();
        services.AddSingleton(_storage);
        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.UseSettingsFramework();

        _bus = serviceProvider.GetRequiredService<IMessageBus>();
        _provider = serviceProvider.GetRequiredService<ISettingsProvider>();
        _provider.RegisterSettings(_settings);
    }

    private Task LoadAsync(string json)
        => _provider.ApplyPersistedSettingsAsync(new Dictionary<string, string> { [_settings.SettingsId] = json });

    [Fact(DisplayName = "Applying persisted settings does not write them back to storage")]
    public async Task Applying_persisted_settings_does_not_save()
    {
        await LoadAsync("""{"TestValue":"Loaded"}""");

        _settings.TestValue.ShouldBe("Loaded");
        await _storage.DidNotReceiveWithAnyArgs().SaveSettingsAsync(default!, default!);
    }

    [Fact(DisplayName = "Applying persisted settings still publishes the change, marked as loaded from storage")]
    public async Task Applying_persisted_settings_publishes_load_event()
    {
        SettingsModelChanged? received = null;
        _bus.Subscribe(new TestSettingsConsumer(e => received = e));

        await LoadAsync("""{"TestValue":"Loaded"}""");

        received.ShouldNotBeNull();
        received.Settings.ShouldBeSameAs(_settings);
        received.LoadedFromStorage.ShouldBeTrue();
    }

    [Fact(DisplayName = "A settings change is still saved to storage")]
    public async Task Settings_change_is_saved()
    {
        _settings.TestValue = "Changed";

        await _provider.NotifySettingsChangedAsync(_settings);

        await _storage.Received(1).SaveSettingsAsync(_settings.SettingsId, Arg.Is<string>(json => json.Contains("Changed")));
    }

    [Fact(DisplayName = "The typed settings event carries the load flag from the untyped event")]
    public async Task Typed_event_carries_load_flag()
    {
        var received = new List<SettingsModelChanged<TestSettings>>();
        _bus.Subscribe(new TypedTestSettingsConsumer(received.Add));

        await LoadAsync("""{"TestValue":"Loaded"}""");
        await _provider.NotifySettingsChangedAsync(_settings);

        received.Count.ShouldBe(2);
        received.ShouldAllBe(e => ReferenceEquals(e.Settings, _settings));
        received[0].LoadedFromStorage.ShouldBeTrue();
        received[1].LoadedFromStorage.ShouldBeFalse();
    }

    [Fact(DisplayName = "SettingsPersister skips an event loaded from storage and saves any other")]
    public async Task Persister_skips_only_loaded_events()
    {
        var persister = new SettingsPersister(NullLogger<SettingsPersister>.Instance, _storage);

        await persister.Consume(new SettingsModelChanged(_settings) { LoadedFromStorage = true });
        await _storage.DidNotReceiveWithAnyArgs().SaveSettingsAsync(default!, default!);

        await persister.Consume(new SettingsModelChanged(_settings));
        await _storage.Received(1).SaveSettingsAsync(_settings.SettingsId, Arg.Any<string>());
    }

    [Theory(DisplayName = "GenericSettingsModelConverter copies the load flag onto the typed event")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Converter_copies_load_flag(bool loadedFromStorage)
    {
        var bus = Substitute.For<IMessageBus>();
        var converter = new GenericSettingsModelConverter(bus, NullLogger<GenericSettingsModelConverter>.Instance);

        await converter.Consume(new SettingsModelChanged(_settings) { LoadedFromStorage = loadedFromStorage });

        await bus.Received(1).PublishAsync(Arg.Is<SettingsModelChanged<TestSettings>>(
            e => ReferenceEquals(e.Settings, _settings) && e.LoadedFromStorage == loadedFromStorage));
    }
}

public class TypedTestSettingsConsumer : IConsumer<SettingsModelChanged<TestSettings>>
{
    private readonly Action<SettingsModelChanged<TestSettings>> _onReceived;

    public TypedTestSettingsConsumer(Action<SettingsModelChanged<TestSettings>> onReceived)
    {
        _onReceived = onReceived;
    }

    public Task Consume(SettingsModelChanged<TestSettings> message)
    {
        _onReceived(message);
        return Task.CompletedTask;
    }
}
