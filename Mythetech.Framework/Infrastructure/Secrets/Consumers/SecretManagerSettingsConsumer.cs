using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Settings.Events;

namespace Mythetech.Framework.Infrastructure.Secrets.Consumers;

/// <summary>
/// Restores the saved active secret manager when persisted settings are loaded.
/// </summary>
public class SecretManagerSettingsConsumer : IConsumer<SettingsModelChanged<SecretManagerSettings>>
{
    private readonly SecretManagerState _state;

    /// <summary>
    /// Creates a new secret manager settings consumer.
    /// </summary>
    public SecretManagerSettingsConsumer(SecretManagerState state)
    {
        _state = state;
    }

    /// <inheritdoc />
    public Task Consume(SettingsModelChanged<SecretManagerSettings> message)
    {
        _state.RestoreActiveManager(message.Settings.ActiveManagerName);
        return Task.CompletedTask;
    }
}
