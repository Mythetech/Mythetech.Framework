namespace Mythetech.Framework.Infrastructure.Settings.Events;

/// <summary>
/// Event published when any settings model changes.
/// Consumers can subscribe to this for generic settings handling (e.g., persistence).
/// </summary>
/// <param name="Settings">The settings instance that changed.</param>
public record SettingsModelChanged(SettingsBase Settings)
{
    /// <summary>
    /// True when the values were just read from settings storage at startup rather than changed.
    /// Consumers apply them as usual; persistence skips them, since storage already holds them.
    /// </summary>
    public bool LoadedFromStorage { get; init; }
}

/// <summary>
/// Typed event for domain-specific settings changes.
/// Allows consumers to subscribe only to their specific settings type.
/// </summary>
/// <typeparam name="T">The specific settings type.</typeparam>
/// <param name="Settings">The settings instance that changed.</param>
public record SettingsModelChanged<T>(T Settings) where T : SettingsBase
{
    /// <inheritdoc cref="SettingsModelChanged.LoadedFromStorage"/>
    public bool LoadedFromStorage { get; init; }
}
