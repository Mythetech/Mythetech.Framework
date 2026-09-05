using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Settings;

namespace Mythetech.Framework.Infrastructure.Keyboard;

/// <summary>
/// Merges declared defaults with the user's overrides.
///
/// This type deliberately does not implement <c>IConsumer</c>. Consumer scanning
/// registers matching types as transient, which would break this service's
/// singleton identity. Components observe <c>SettingsModelChanged</c> instead.
/// </summary>
public sealed class KeyBindingService : IKeyBindingService
{
    private readonly IKeyBindingRegistry _registry;
    private readonly KeyBindingSettings _settings;
    private readonly ISettingsProvider _settingsProvider;
    private readonly IMessageBus _bus;
    private readonly ILogger<KeyBindingService> _logger;

    /// <inheritdoc cref="KeyBindingService"/>
    public KeyBindingService(
        IKeyBindingRegistry registry,
        KeyBindingSettings settings,
        ISettingsProvider settingsProvider,
        IMessageBus bus,
        ILogger<KeyBindingService> logger)
    {
        _registry = registry;
        _settings = settings;
        _settingsProvider = settingsProvider;
        _bus = bus;
        _logger = logger;
    }

    /// <inheritdoc />
    public IReadOnlyList<ResolvedKeyBinding> GetAll()
        => _registry.GetAll().Select(Resolve).ToArray();

    /// <inheritdoc />
    public IEnumerable<IGrouping<string, ResolvedKeyBinding>> GetByCategory()
        => GetAll().GroupBy(binding => binding.Category);

    /// <inheritdoc />
    public KeyBinding? GetBinding(string id)
        => _registry.Get(id) is { } definition ? Resolve(definition).CurrentBinding : null;

    /// <inheritdoc />
    public async Task SetBindingAsync(string id, KeyBinding binding)
    {
        if (_registry.Get(id) is not { } definition)
        {
            _logger.LogWarning("No key binding registered with id {Id}; ignoring assignment", id);
            return;
        }

        ApplyBinding(definition, binding);
        await PersistAsync();
    }

    /// <inheritdoc />
    public async Task UnbindAsync(string id)
    {
        if (_registry.Get(id) is not { } definition)
        {
            _logger.LogWarning("No key binding registered with id {Id}; ignoring unbind", id);
            return;
        }

        ApplyUnbind(definition);
        await PersistAsync();
    }

    /// <inheritdoc />
    public async Task ResetAsync(string id)
    {
        if (!_settings.Overrides.Remove(id))
        {
            return;
        }

        await PersistAsync();
    }

    /// <inheritdoc />
    public async Task ResetAllAsync()
    {
        if (_settings.Overrides.Count == 0)
        {
            return;
        }

        _settings.Overrides.Clear();
        await PersistAsync();
    }

    private void ApplyBinding(KeyBindingDefinition definition, KeyBinding binding)
    {
        if (binding == definition.DefaultBinding)
        {
            _settings.Overrides.Remove(definition.Id);
            return;
        }

        _settings.Overrides[definition.Id] = SerializedKeyBinding.FromBinding(binding);
    }

    private void ApplyUnbind(KeyBindingDefinition definition)
    {
        if (definition.DefaultBinding is null)
        {
            _settings.Overrides.Remove(definition.Id);
            return;
        }

        _settings.Overrides[definition.Id] = null;
    }

    private async Task PersistAsync()
    {
        _settings.MarkDirty();
        await _settingsProvider.NotifySettingsChangedAsync(_settings);
    }

    /// <inheritdoc />
    public bool TryFindConflict(string id, KeyBinding proposed, out string? conflictingId)
    {
        foreach (var resolved in GetAll())
        {
            if (resolved.Id == id)
            {
                continue;
            }

            if (resolved.CurrentBinding == proposed)
            {
                conflictingId = resolved.Id;
                return true;
            }
        }

        conflictingId = null;
        return false;
    }

    /// <inheritdoc />
    public async Task ReassignAsync(string id, KeyBinding binding)
    {
        if (_registry.Get(id) is not { } definition)
        {
            _logger.LogWarning("No key binding registered with id {Id}; ignoring reassignment", id);
            return;
        }

        if (TryFindConflict(id, binding, out var conflictingId)
            && conflictingId is not null
            && _registry.Get(conflictingId) is { } previous)
        {
            ApplyUnbind(previous);
        }

        ApplyBinding(definition, binding);
        await PersistAsync();
    }

    /// <inheritdoc />
    public async Task InvokeAsync(string id, CancellationToken cancellationToken)
    {
        if (_registry.Get(id) is not { } definition)
        {
            _logger.LogWarning("No key binding registered with id {Id}; ignoring invocation", id);
            return;
        }

        try
        {
            await definition.Handler(_bus, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Key binding {Id} handler threw", id);
        }
    }

    private ResolvedKeyBinding Resolve(KeyBindingDefinition definition)
        => _settings.Overrides.TryGetValue(definition.Id, out var serialized)
            ? new ResolvedKeyBinding(definition, serialized?.ToBinding(), IsCustomized: true)
            : new ResolvedKeyBinding(definition, definition.DefaultBinding, IsCustomized: false);
}
