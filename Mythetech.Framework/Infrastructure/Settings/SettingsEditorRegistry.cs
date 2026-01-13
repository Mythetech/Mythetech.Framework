using Microsoft.AspNetCore.Components;

namespace Mythetech.Framework.Infrastructure.Settings;

/// <summary>
/// Default implementation of <see cref="ISettingsEditorRegistry"/>.
/// Maintains a dictionary mapping data types to their editor components.
/// </summary>
public class SettingsEditorRegistry : ISettingsEditorRegistry
{
    private readonly Dictionary<Type, Type> _editors = new();

    /// <inheritdoc />
    public void RegisterEditor(Type dataType, Type editorComponentType)
    {
        ArgumentNullException.ThrowIfNull(dataType);
        ArgumentNullException.ThrowIfNull(editorComponentType);

        if (!typeof(ComponentBase).IsAssignableFrom(editorComponentType))
        {
            throw new ArgumentException(
                $"Editor type {editorComponentType.Name} must inherit from ComponentBase.",
                nameof(editorComponentType));
        }

        _editors[dataType] = editorComponentType;
    }

    /// <inheritdoc />
    public Type? GetEditorForType(Type dataType)
    {
        ArgumentNullException.ThrowIfNull(dataType);
        return _editors.GetValueOrDefault(dataType);
    }

    /// <inheritdoc />
    public bool HasEditorForType(Type dataType)
    {
        ArgumentNullException.ThrowIfNull(dataType);
        return _editors.ContainsKey(dataType);
    }
}
