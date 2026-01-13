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

    private static readonly MethodInfo PublishAsyncMethod = typeof(IMessageBus)
        .GetMethods()
        .First(m => m.Name == nameof(IMessageBus.PublishAsync) &&
                    m.IsGenericMethod &&
                    m.GetParameters().Length == 1);

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
            // Create SettingsModelChanged<T> where T is the actual settings type
            var genericEventType = typeof(SettingsModelChanged<>).MakeGenericType(settingsType);
            var typedEvent = Activator.CreateInstance(genericEventType, message.Settings);

            if (typedEvent == null)
            {
                _logger.LogWarning("Failed to create typed event for {SettingsType}", settingsType.Name);
                return;
            }

            // Use reflection to call PublishAsync<T> with the correct generic type
            var genericPublish = PublishAsyncMethod.MakeGenericMethod(genericEventType);
            var task = genericPublish.Invoke(_bus, [typedEvent]) as Task;

            if (task != null)
            {
                await task;
            }

            _logger.LogDebug("Converted and published typed settings event for {SettingsType}", settingsType.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert settings event for {SettingsType}", settingsType.Name);
        }
    }
}
