namespace Mythetech.Framework.Infrastructure.Settings;

/// <summary>
/// Options for configuring custom settings editors.
/// Used to collect custom editor registrations before the service provider is built.
/// </summary>
public class SettingsEditorOptions
{
    /// <summary>
    /// Custom editor mappings that override or extend the default editors.
    /// Key is the data type, value is the editor component type.
    /// </summary>
    public Dictionary<Type, Type> CustomEditors { get; } = new();
}
