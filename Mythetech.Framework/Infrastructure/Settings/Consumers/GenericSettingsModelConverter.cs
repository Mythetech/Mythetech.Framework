using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Settings.Events;

namespace Mythetech.Framework.Infrastructure.Settings.Consumers;

/// <summary>
/// Converts untyped SettingsModelChanged events to typed SettingsModelChanged&lt;T&gt; events.
/// This allows domain consumers to subscribe only to their specific settings type
/// without needing to filter or cast from a generic event.
/// </summary>
public class GenericSettingsModelConverter : IConsumer<SettingsModelChanged>
{
    private readonly IMessageBus _bus;
    private readonly ILogger<GenericSettingsModelConverter> _logger;

    private static readonly MethodInfo PublishTypedMethod = typeof(GenericSettingsModelConverter)
        .GetMethod(nameof(PublishTypedAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly ConcurrentDictionary<Type, Func<IMessageBus, SettingsModelChanged, Task>> TypedPublishers = new();

    /// <summary>
    /// Creates a new converter instance.
    /// </summary>
    /// <param name="bus">The message bus for publishing converted events.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public GenericSettingsModelConverter(IMessageBus bus, ILogger<GenericSettingsModelConverter> logger)
    {
        _bus = bus;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Consume(SettingsModelChanged message)
    {
        var settingsType = message.Settings.GetType();

        try
        {
            var publish = TypedPublishers.GetOrAdd(settingsType, static type => PublishTypedMethod
                .MakeGenericMethod(type)
                .CreateDelegate<Func<IMessageBus, SettingsModelChanged, Task>>());

            await publish(_bus, message);

            _logger.LogDebug("Converted and published typed settings event for {SettingsType}", settingsType.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert settings event for {SettingsType}", settingsType.Name);
        }
    }

    private static Task PublishTypedAsync<T>(IMessageBus bus, SettingsModelChanged message) where T : SettingsBase
        => bus.PublishAsync(new SettingsModelChanged<T>((T)message.Settings) { LoadedFromStorage = message.LoadedFromStorage });
}
