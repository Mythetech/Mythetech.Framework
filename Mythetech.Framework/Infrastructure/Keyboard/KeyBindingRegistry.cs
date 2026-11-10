using Microsoft.Extensions.Options;

namespace Mythetech.Framework.Infrastructure.Keyboard;

/// <inheritdoc cref="IKeyBindingRegistry"/>
public sealed class KeyBindingRegistry : IKeyBindingRegistry
{
    private readonly IReadOnlyList<KeyBindingDefinition> _definitions;
    private readonly Dictionary<string, KeyBindingDefinition> _byId;
    private readonly Dictionary<KeyBindingScope, KeyBinding[]> _reservedByScope;

    /// <inheritdoc cref="KeyBindingRegistry"/>
    public KeyBindingRegistry(IOptions<KeyBindingRegistrationOptions> options)
    {
        _definitions = options.Value.Definitions.ToArray();
        _reservedByScope = options.Value.Reserved
            .GroupBy(reserved => reserved.Scope)
            .ToDictionary(group => group.Key, group => group.Select(reserved => reserved.Binding).Distinct().ToArray());
        _byId = new Dictionary<string, KeyBindingDefinition>(StringComparer.Ordinal);

        foreach (var definition in _definitions)
        {
            if (!_byId.TryAdd(definition.Id, definition))
            {
                throw new InvalidOperationException(
                    $"Key binding '{definition.Id}' is registered more than once. Ids are persistence keys and must be unique.");
            }
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<KeyBindingDefinition> GetAll() => _definitions;

    /// <inheritdoc />
    public KeyBindingDefinition? Get(string id) => _byId.GetValueOrDefault(id);

    /// <inheritdoc />
    public IReadOnlyList<KeyBinding> GetReserved(KeyBindingScope scope) => _reservedByScope.GetValueOrDefault(scope) ?? [];
}
