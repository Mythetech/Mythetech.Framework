namespace Mythetech.Framework.Infrastructure.Settings;

/// <summary>
/// Abstract base class for all domain settings models.
/// Each settings domain (Editor, Console, Preferences) should inherit from this.
///
/// Settings with [Setting] attribute are edited via the settings dialog and
/// should use simple auto-properties. The dialog handles change notification
/// when the user clicks Done.
///
/// For settings that need immediate persistence (like favorites/recents),
/// call MarkDirty() after modification, then call the persistence method directly.
/// </summary>
public abstract class SettingsBase
{
    /// <summary>
    /// Unique identifier for this settings domain (e.g., "Editor", "Console").
    /// Used as the key for persistence.
    /// </summary>
    public abstract string SettingsId { get; }

    /// <summary>
    /// Display name shown in the settings UI navigation.
    /// </summary>
    public abstract string DisplayName { get; }

    /// <summary>
    /// Icon identifier to show in the navigation.
    /// Can be a Material icon name or custom icon class.
    /// </summary>
    public abstract string Icon { get; }

    /// <summary>
    /// Sort order in the settings UI (lower values appear first).
    /// </summary>
    public virtual int Order => 50;

    /// <summary>
    /// Tracks whether this settings model has unsaved changes.
    /// Set to true when properties are modified, reset to false after persistence.
    /// </summary>
    public bool IsDirty { get; private set; }

    /// <summary>
    /// Marks this settings model as having unsaved changes.
    /// Call this after modifying properties that need immediate persistence.
    /// </summary>
    public void MarkDirty()
    {
        IsDirty = true;
    }

    /// <summary>
    /// Clears the dirty flag after settings have been persisted.
    /// </summary>
    public void ClearDirty()
    {
        IsDirty = false;
    }

    /// <summary>
    /// Creates a snapshot of current property values for revert capability.
    /// Only captures properties with [Setting] attribute.
    /// </summary>
    public Dictionary<string, object?> CreateSnapshot()
    {
        var snapshot = new Dictionary<string, object?>();

        var properties = SettingsPropertyCache.GetSettingProperties(GetType());

        foreach (var propInfo in properties)
        {
            if (!propInfo.Property.CanRead)
                continue;

            snapshot[propInfo.Property.Name] = propInfo.Property.GetValue(this);
        }

        return snapshot;
    }

    /// <summary>
    /// Restores property values from a previously created snapshot.
    /// </summary>
    public void RestoreFromSnapshot(Dictionary<string, object?> snapshot)
    {
        var properties = SettingsPropertyCache.GetSettingProperties(GetType());

        foreach (var propInfo in properties)
        {
            if (!propInfo.Property.CanWrite)
                continue;

            if (!snapshot.TryGetValue(propInfo.Property.Name, out var value))
                continue;

            try
            {
                propInfo.Property.SetValue(this, value);
            }
            catch
            {
                // Ignore type mismatch errors during restore
            }
        }
    }

    /// <summary>
    /// Optional Blazor component type rendered at the beginning of this settings section, before any settings.
    /// Use for introductory text, warnings, or custom controls that should appear first.
    /// The component will be instantiated via DynamicComponent and can inject services.
    /// </summary>
    public virtual Type? BeginningContent => null;

    /// <summary>
    /// Optional Blazor component type rendered at the end of this settings section, after all settings.
    /// Use for action buttons (delete, reset), additional info, or custom controls.
    /// The component will be instantiated via DynamicComponent and can inject services.
    /// </summary>
    public virtual Type? EndingContent => null;
}
