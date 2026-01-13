namespace Mythetech.Framework.Infrastructure.Settings;

/// <summary>
/// Registry for mapping data types to their editor components.
/// Used by the settings framework to determine which component renders each setting type.
/// </summary>
public interface ISettingsEditorRegistry
{
    /// <summary>
    /// Registers an editor component type for a specific data type.
    /// If an editor is already registered for the type, it will be replaced.
    /// </summary>
    /// <param name="dataType">The data type to register an editor for.</param>
    /// <param name="editorComponentType">The Blazor component type that renders this data type.</param>
    void RegisterEditor(Type dataType, Type editorComponentType);

    /// <summary>
    /// Gets the editor component type registered for a data type.
    /// </summary>
    /// <param name="dataType">The data type to look up.</param>
    /// <returns>The editor component type, or null if none is registered.</returns>
    Type? GetEditorForType(Type dataType);

    /// <summary>
    /// Checks whether an editor is registered for a data type.
    /// </summary>
    /// <param name="dataType">The data type to check.</param>
    /// <returns>True if an editor is registered, false otherwise.</returns>
    bool HasEditorForType(Type dataType);
}
